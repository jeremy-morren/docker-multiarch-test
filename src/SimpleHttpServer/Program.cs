

using System.Net;
using System.Text;
using System.Web;
using Serilog;
using SimpleHttpServer;

const int defaultPort = 11523;

var port = args.Length > 0 && int.TryParse(args[0], out var parsedPort) && parsedPort > 0 && parsedPort < short.MaxValue
    ? parsedPort
    : defaultPort;

Console.WriteLine($"Starting HTTP server on port {port}");

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .MinimumLevel.Debug()
    .CreateLogger();

var exitEvent = new ManualResetEvent(false);
Console.CancelKeyPress += (_, e) =>
{
    Console.WriteLine("Exiting...");
    e.Cancel = true; // Prevent the process from terminating immediately
    exitEvent.Set(); // Signal the exit event
};
try
{
    using var server = new HttpServer(IPAddress.Any, port, new BasicHandler(), Log.Logger);
    server.Start();
    Console.WriteLine("Press Ctrl+C to exit");
    exitEvent.WaitOne(); // Wait for the exit event to be signaled
    server.Stop();
    return 0;
}
catch (Exception ex)
{
    Log.Fatal(ex, "Unhandled exception");
    return 1;
}
finally
{
    Log.CloseAndFlush();
}

class BasicHandler : IHttpRequestHandler
{
    public Task<HttpResponse> Handle(HttpRequest request)
    {
        // Return the raw response (for demonstration purposes)
        var sb = new StringBuilder();
        sb.AppendLine($"{request.Protocol} {request.Method} {request.Path}");
        foreach (var header in request.Headers)
            sb.AppendLine($"{header.Key}: {header.Value}");
        sb.AppendLine();
        if (request.Body.Length > 0)
            sb.AppendLine(Encoding.UTF8.GetString(request.Body));
        var response = HttpResponse.Create(HttpStatusCode.OK, sb.ToString());
        return Task.FromResult(response);
    }
}