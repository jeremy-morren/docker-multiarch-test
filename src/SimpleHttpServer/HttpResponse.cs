using System.Net;
using System.Text;

namespace SimpleHttpServer;

/// <summary>
/// A class representing an HTTP response.
/// </summary>
public class HttpResponse
{
    public required HttpStatusCode StatusCode { get; init; }

    public required IReadOnlyDictionary<string, string> Headers { get; init; }

    public byte[] Body { get; init; } = [];

    public static HttpResponse Create(HttpStatusCode statusCode) => new ()
    {
        StatusCode = statusCode,
        Headers = new Dictionary<string, string>()
    };

    public static HttpResponse Create(HttpStatusCode statusCode, string body) =>
        new()
        {
            StatusCode = statusCode,
            Headers = new Dictionary<string, string>
            {
                { "Content-Type", "text/plain; charset=utf-8" }
            },
            Body = Encoding.UTF8.GetBytes(body)
        };
}