namespace SimpleHttpServer;

public class HttpRequest
{
    public required string Protocol { get; init; }

    public required HttpMethod Method { get; init; }

    public required string Path { get; init; }

    public required IReadOnlyDictionary<string, string> Headers { get; init; }

    public required Uri Url { get; init; }

    public required byte[] Body { get; init; }
}