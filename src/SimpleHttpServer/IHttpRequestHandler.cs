using System.Net;
using System.Text;

namespace SimpleHttpServer;

public interface IHttpRequestHandler
{
    HttpResponse Handle(HttpRequest request);

    void OnError(Exception exception);
}

public class HttpResponse
{
    public required HttpStatusCode StatusCode { get; init; }

    public required IReadOnlyDictionary<string, string> Headers { get; init; }

    public required byte[] Body { get; init; }

    public static HttpResponse Create(HttpStatusCode statusCode, string body) =>
        new()
        {
            StatusCode = statusCode,
            Headers = new Dictionary<string, string>
            {
                { "Content-Type", "text/plain" },
                { "Content-Encoding", Encoding.UTF8.WebName }
            },
            Body = Encoding.UTF8.GetBytes(body)
        };
}