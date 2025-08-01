

using System.Net;
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
    private static readonly HttpClient Client = new();

    public async Task<HttpResponse> Handle(HttpRequest request)
    {
        if (!request.Headers.TryGetValue("Authorization", out var auth) || !auth.StartsWith("Bearer "))
            return HttpResponse.Create(HttpStatusCode.Unauthorized);

        var query = HttpUtility.ParseQueryString(request.Url.Query);
        var qty = int.TryParse(query["qty"], out var q) ? q : 10;
        var response = await Client.GetByteArrayAsync($"https://fakerapi.it/api/v2/companies?_quantity={qty}");
        return new HttpResponse()
        {
            Body = request.Method == HttpMethod.Head ? [] : response,
            StatusCode = HttpStatusCode.OK,
            Headers = new Dictionary<string, string>()
            {
                { "Content-Type", "application/json; charset=utf-8" }
            }
        };
        // var response = HttpResponse.Create(HttpStatusCode.OK, "Hello, World!");
        // return Task.FromResult(response);
    }
}