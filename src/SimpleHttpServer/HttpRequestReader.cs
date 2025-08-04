using System.Text;

namespace SimpleHttpServer;

public static class HttpRequestReader
{
    public static HttpRequest? Read(Stream stream)
    {
        var reader = new BufferReader(stream);
        var requestLine = TryReadRequestLine(reader);
        if (requestLine == null)
            return null; // End of stream reached, no request to read
        ParseRequestLine(requestLine, out var method, out var path, out var protocol);

        // Read the headers (until we hit an empty line)
        var headerLines = new List<string>();
        while (true)
        {
            var line = reader.ReadAsciiLine();
            if (line == string.Empty)
                break;
            headerLines.Add(line);
        }
        var headers = new HttpHeadersDictionary();
        foreach (var header in headerLines)
        {
            ParseHeader(header, out var key, out var value);
            headers.Add(key, value);
        }
        byte[] body;

        if (headers.TryGetValue("Content-Length", out var contentLength))
        {
            // Read the body based on the Content-Length header
            if (!int.TryParse(contentLength, out var length) || length < 0)
                throw new InvalidHttpRequestException("Invalid Content-Length header.");
            body = reader.Read(length).ToArray();
            if (body.Length != length)
                throw new InvalidHttpRequestException("Content-Length does not match the actual body length.");
        }
        else if (headers.GetValueOrDefault("Transfer-Encoding") == "chunked")
        {
            // Read the body as chunked encoding
            using var bodyStream = new MemoryStream();
            while (true)
            {
                var chunkSizeLine = reader.ReadAsciiLine();
                if (string.IsNullOrEmpty(chunkSizeLine))
                    throw new InvalidHttpRequestException("Invalid chunked transfer encoding.");
                if (!int.TryParse(chunkSizeLine, System.Globalization.NumberStyles.HexNumber, null, out var chunkSize) || chunkSize < 0)
                    throw new InvalidHttpRequestException("Invalid chunk size in chunked transfer encoding.");
                var chunkData = reader.Read(chunkSize);
                if (chunkData.Length != chunkSize)
                    throw new InvalidHttpRequestException("Chunk size does not match the actual chunk data length.");
                bodyStream.Write(chunkData.Span);
                // Read the trailing CRLF after the chunk data
                if (reader.ReadByte() != '\r' || reader.ReadByte() != '\n')
                    throw new InvalidHttpRequestException("Expected CRLF after chunk data.");
                if (chunkSize == 0)
                    break; // Last chunk, end of body
            }
            body = bodyStream.ToArray();
        }
        else
        {
            // No request body
            body = [];
        }

        return new HttpRequest()
        {
            Method = method,
            Protocol = protocol,
            Path = path,
            Headers = headers,
            Url = GetUrl(path, headers),
            Body = body,
        };
    }

    private static void ParseRequestLine(string line, out HttpMethod method, out string path, out string protocol)
    {
        var parts = line.Split(' ');
        if (parts.Length != 3)
            throw new InvalidHttpRequestException("Invalid HTTP request line format.");

        method = HttpMethod.Parse(parts[0]);
        path = parts[1];
        protocol = parts[2];
    }

    private static void ParseHeader(string line, out string key, out string value)
    {
        var colonIndex = line.IndexOf(':');
        if (colonIndex == -1)
            throw new InvalidHttpRequestException("Invalid HTTP header format.");
        key = line[..colonIndex].Trim();
        value = line[(colonIndex + 1)..].Trim();
    }

    private static Uri GetUrl(string path, HttpHeadersDictionary headers)
    {
        if (headers.TryGetValue("Host", out var host))
        {
            // Use the Host header to construct the full URL
            var fullUrl = $"http://{host}{path}";
            if (!Uri.TryCreate(fullUrl, UriKind.Absolute, out var uri))
                throw new InvalidHttpRequestException($"Invalid Host header: {host}");
            return uri;
        }

        // If no Host header, just use the path as the URL
        if (!Uri.TryCreate(path, UriKind.Relative, out var uriPath))
            throw new InvalidHttpRequestException($"Invalid path: {path}");
        return uriPath;
    }

    private static string? TryReadRequestLine(BufferReader reader)
    {
        var result = reader.Read(1);
        if (result.Length == 0 || result.Span[0] == 0)
            return null; // End of stream reached, no request line to read

        // There is more data to read, so we can read the rest of the request line
        var sb = new StringBuilder(capacity: 64);
        sb.Append((char)result.Span[0]);
        sb.Append(reader.ReadAsciiLine());
        return sb.ToString();
    }

    private static string ReadAsciiLine(this BufferReader reader)
    {
        var result = new StringBuilder(capacity: 64);
        while (true)
        {
            var b = reader.ReadByte();
            if (b == '\r')
            {
                // Next byte should be '\n'
                if (reader.ReadByte() != '\n')
                    throw new InvalidHttpRequestException(@"Expected '\n' after '\r' in HTTP request line.");
                return result.ToString();
            }
            // Not a line end, so append the byte to the result
            result.Append((char)b);
        }
    }
}