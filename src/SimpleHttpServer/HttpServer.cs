using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace SimpleHttpServer;

public sealed class HttpServer : IDisposable
{
    private readonly TcpListener _server;

    private readonly IHttpRequestHandler _handler;

    public HttpServer(int port, IHttpRequestHandler handler)
    {
        _handler = handler;
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(port);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(port, short.MaxValue);

        _server = new TcpListener(IPAddress.Any, port);
    }

    public void Start()
    {
        _server.Start();
        _server.BeginAcceptTcpClient(OnAcceptTcpClient, this);
    }

    public void Stop() => _server.Stop();

    public void Dispose() => _server.Dispose();

    private static void OnAcceptTcpClient(IAsyncResult result)
    {
        Debug.Assert(result.AsyncState is HttpServer);
        var server = (HttpServer)result.AsyncState;

        try
        {

            using var client = TryAcceptTcpClient(server._server, result);
            if (client == null)
            {
                // The server was stopped, no client to handle
                return;
            }

            // Accept the next client
            server._server.BeginAcceptTcpClient(OnAcceptTcpClient, server);

            // Handle the request
            using var stream = client.GetStream();

            while (true)
            {
                var ts = Stopwatch.GetTimestamp();

                var request = HttpRequestReader.Read(stream);
                if (request == null)
                {
                    // No request was read, likely the client closed the connection
                    break;
                }
                try
                {
                    var response = server._handler.Handle(request);
                    WriteResponse(stream, response);
                }
                catch (Exception ex)
                {
                    server._handler.OnError(ex);

                    var response = HttpResponse.Create(HttpStatusCode.InternalServerError, ex.ToString());
                    WriteResponse(stream, response);
                }

                var elapsed = Stopwatch.GetElapsedTime(ts);
                Console.WriteLine($"Handled request in {elapsed.TotalMilliseconds:#,0.00} ms");
            }
            client.Close();
        }
        catch (Exception ex)
        {
            server._handler.OnError(ex);
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
}