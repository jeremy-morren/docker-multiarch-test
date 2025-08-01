using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Serilog;

namespace SimpleHttpServer;

/// <summary>
/// A simple HTTP server that listens for incoming TCP connections and handles HTTP requests.
/// </summary>
public sealed class HttpServer : IDisposable
{
    private readonly IHttpRequestHandler _handler;
    private readonly ILogger _logger;

    private readonly TcpListener _server;

    public HttpServer(
        IPAddress address,
        int port,
        IHttpRequestHandler handler,
        ILogger logger)
    {
        _handler = handler;
        _logger = logger.ForContext<HttpServer>();

        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(port);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(port, short.MaxValue);

        _server = new TcpListener(address, port);
    }

    /// <summary>
    /// IP address on which the server is listening.
    /// </summary>
    public IPAddress IpAddress => ((IPEndPoint)_server.LocalEndpoint).Address;

    /// <summary>
    /// Port on which the server is listening.
    /// </summary>
    public int Port => ((IPEndPoint)_server.LocalEndpoint).Port;

    public void Start()
    {
        _logger.Debug("Starting HTTP server...");

        _server.Start();
        _server.BeginAcceptTcpClient(OnAcceptTcpClient, this);

        _logger.Information("HTTP server started. Endpoint: {IPAddress}:{Port}", IpAddress, Port);
    }

    public void Stop()
    {
        _logger.Debug("Stopping HTTP server...");

        try
        {
            _server.Stop();
            _logger.Information("HTTP server stopped");
        }
        catch (Exception ex)
        {
            _logger.Fatal(ex, "Error stopping HTTP server");
            throw;
        }
    }

    public void Dispose() => _server.Dispose();

    private static void OnAcceptTcpClient(IAsyncResult result)
    {
        Debug.Assert(result.AsyncState is HttpServer);
        var server = (HttpServer)result.AsyncState;
        try
        {
            var client = TryAcceptTcpClient(server._server, result);

            if (client == null)
            {
                // The server was stopped, no client to handle
                return;
            }

            // Accept the next client
            server._server.BeginAcceptTcpClient(OnAcceptTcpClient, server);

            // Handle the accepted client
            Task.Run(() => server.HandleClient(client));
        }
        catch (OperationCanceledException ex)
        {
            server._logger.Debug(ex, "Server was stopped while accepting client");
        }
        catch (Exception ex)
        {
            server._logger.Fatal(ex, "Unknown error accepting client");
        }
    }

    private async Task HandleClient(TcpClient client)
    {
        try
        {
            _logger.Debug("Accepted client {Handle} {RemoteAddress}", client.Client.Handle, GetRemoteEndpoint(client));

            await using var stream = client.GetStream();

            // Read requests from the client until the connection is closed
            while (true)
            {
                if (!client.Connected)
                {
                    _logger.Debug("Client {Handle} {RemoteAddress} disconnected",
                        GetHandle(client), GetRemoteEndpoint(client));
                    break; // Client is no longer connected, exit the loop
                }

                var request = HttpRequestReader.Read(stream);
                if (request == null)
                {
                    // No request was read, likely the client closed the connection
                    _logger.Debug("Client {Handle} {RemoteAddress} closed the connection",
                        GetHandle(client), GetRemoteEndpoint(client));
                    break;
                }
                _logger.Debug("Handling request {Method} {Url} from {Handle} {RemoteAddress}",
                    request.Method, request.Url, GetHandle(client), GetRemoteEndpoint(client));

                var ts = Stopwatch.GetTimestamp();

                // Handle the request
                HttpResponse response;
                try
                {
                    response = await _handler.Handle(request);
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, "An error occurred while handling the request");
                    response = HttpResponse.Create(HttpStatusCode.InternalServerError);
                }

                WriteResponse(stream, response);

                var elapsed = Stopwatch.GetElapsedTime(ts);
                _logger.Information(
                    "{RemoteAddress} {Method} {Url} {StatusCode} {BytesSent} {Elapsed:#,0.00} ms {UserAgent}",
                    GetRemoteEndpoint(client),
                    request.Method,
                    request.Url,
                    (int)response.StatusCode,
                    response.Body.Length,
                    elapsed.TotalMilliseconds,
                    request.Headers.GetValueOrDefault("User-Agent"));
            }

            client.Close();
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error handling client {Handle}: {RemoteAddress}",
                GetHandle(client), GetRemoteEndpoint(client));
        }
        finally
        {
            client.Dispose();
        }
    }

    private static void WriteResponse(Stream stream, HttpResponse response)
    {
        var header = new List<string>()
        {
            $"HTTP/1.1 {(int)response.StatusCode} {response.StatusCode}",
            $"Content-Length: {response.Body.Length}"
        };
        foreach (var (key, value) in response.Headers)
            header.Add($"{key}: {value}");

        var headerStr = string.Join("\r\n", header) + "\r\n\r\n";
        var headerBytes = Encoding.ASCII.GetBytes(headerStr);

        stream.Write(headerBytes);
        stream.Write(response.Body);
    }

    private static TcpClient? TryAcceptTcpClient(TcpListener server, IAsyncResult result)
    {
        try
        {
            return server.EndAcceptTcpClient(result);
        }
        catch (ObjectDisposedException ex) when (ex.ObjectName == typeof(Socket).FullName)
        {
            // The server was stopped, so we can't accept a client
            // See https://github.com/dotnet/runtime/blob/eef526e6d1d767e0797cc4471f05012c08d7733e/src/libraries/System.Net.Sockets/src/System/Net/Sockets/TCPListener.cs#L303
            return null;
        }
    }


    private static string? GetRemoteEndpoint(TcpClient client)
    {
        try
        {
            // ReSharper disable once ConditionalAccessQualifierIsNonNullableAccordingToAPIContract
            return client.Client?.RemoteEndPoint switch
            {
                IPEndPoint ip => ip.Address.ToString(),
                { } ep => ep.ToString(),
                _ => null
            };
        }
        catch (Exception)
        {
            // If the client was disconnected or the endpoint is not available, return null
            return null;
        }
    }

    private static IntPtr? GetHandle(TcpClient client)
    {
        try
        {
            // ReSharper disable once ConditionalAccessQualifierIsNonNullableAccordingToAPIContract
            return client.Client?.Handle;
        }
        catch (Exception)
        {
            // If the client was disconnected or the endpoint is not available, return null
            return null;
        }
    }
}