

using System.Net;
using SimpleHttpServer;

const int port = 11523;

Console.WriteLine($"Starting HTTP server on port {port}");

using var server = new HttpServer(port, new BasicHandler());
server.Start();
Console.WriteLine($"HTTP server started on port {port}. Press any key to stop...");
Console.ReadKey();
server.Stop();

class BasicHandler : IHttpRequestHandler
{
    public HttpResponse Handle(HttpRequest request)
    {
        Console.WriteLine($"Received request: {request.Method} {request.Url}");
        return HttpResponse.Create(HttpStatusCode.OK, "Hello, World!");
    }

    public void OnError(Exception exception)
    {
        Console.WriteLine($"Error handling request: {exception}");
    }
}