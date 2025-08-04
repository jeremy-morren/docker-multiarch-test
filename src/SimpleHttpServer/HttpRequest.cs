namespace SimpleHttpServer;

public class HttpRequest
{
    /// <summary>
    /// The protocol parsed from the request line. Always <c>HTTP/1.1</c>.
    /// </summary>
    public required string Protocol { get; init; }

    /// <summary>
    /// The method parsed from the request line.
    /// </summary>
    public required HttpMethod Method { get; init; }

    /// <summary>
    /// Raw request path, as received from the client.
    /// </summary>
    public required string Path { get; init; }

    /// <summary>
    /// Headers parsed from the request.
    /// </summary>
    public required HttpHeadersDictionary Headers { get; init; }

    /// <summary>
    /// Parsed URL from the request
    /// </summary>
    /// <remarks>
    /// Constructed from the <c>Host</c> header and the request path.
    /// If the <c>Host</c> header is not present, the URL type will be <c>Relative</c>.
    /// </remarks>
    public required Uri Url { get; init; }

    /// <summary>
    /// Body of the request, if present.
    /// </summary>
    public required byte[] Body { get; init; }
}