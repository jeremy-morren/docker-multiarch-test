namespace SimpleHttpServer;

/// <summary>
/// An HTTP header value that can be compared with other values.
/// This class implements case-insensitive comparison for HTTP header values,
/// </summary>
public class HttpHeaderValue : IEquatable<HttpHeaderValue>, IEquatable<string>
{
    public string Value { get; }

    public HttpHeaderValue(string value)
    {
        Value = value;
    }

    public bool StartsWith(string other) => Value.StartsWith(other, StringComparison.OrdinalIgnoreCase);

    public bool Equals(string? other) => string.Equals(Value, other, StringComparison.OrdinalIgnoreCase);

    public bool Equals(HttpHeaderValue? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return Equals(other.Value);
    }

    public override bool Equals(object? obj)
    {
        if (obj is null) return false;
        if (ReferenceEquals(this, obj)) return true;
        return obj switch
        {
            HttpHeaderValue other => Equals(other.Value),
            string other => Equals(other),
            _ => false
        };
    }

    public override int GetHashCode() => StringComparer.OrdinalIgnoreCase.GetHashCode(Value);

    public static bool operator ==(HttpHeaderValue? left, HttpHeaderValue? right) => Equals(left, right);

    public static bool operator !=(HttpHeaderValue? left, HttpHeaderValue? right) => !Equals(left, right);

    public static implicit operator HttpHeaderValue(string value) => new(value);
    public static implicit operator string(HttpHeaderValue value) => value.Value;

    public override string ToString() => Value;
}