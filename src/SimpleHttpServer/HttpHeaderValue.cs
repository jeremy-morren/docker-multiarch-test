// ReSharper disable UnusedMember.Global
namespace SimpleHttpServer;

/// <summary>
/// Value of an HTTP header.
/// </summary>
/// <remarks>
/// Allows multiple values and implements case-insensitive comparison.
/// </remarks>
public class HttpHeaderValue : IEquatable<HttpHeaderValue>, IEquatable<string>
{
    private readonly string[] _values;

    private HttpHeaderValue(string[] values)
    {
        _values = values;
    }

    /// <summary>
    /// Creates a new <see cref="HttpHeaderValue"/> with the specified value appended to the existing values.
    /// </summary>
    public HttpHeaderValue Add(string value)
    {
        var array = new string[_values.Length + 1];
        _values.CopyTo(array, 0);
        array[^1] = value;
        return new HttpHeaderValue(array);
    }

    /// <summary>
    /// ets the concatenated string representation of all values in the header, separated by commas.
    /// </summary>
    public string Value => string.Join(',', _values);

    /// <summary>
    /// Checks if the header value is equal to the specified string, ignoring case.
    /// </summary>
    /// <param name="other"></param>
    /// <returns></returns>
    public bool Equals(string? other) => string.Equals(Value, other, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Checks if the header value is equal to the specified string, ignoring case.
    /// </summary>
    /// <param name="other"></param>
    /// <returns></returns>
    public bool StartsWith(string other) => Value.StartsWith(other, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Checks if the header value contains the specified string, ignoring case.
    /// </summary>
    /// <param name="other"></param>
    /// <returns></returns>
    public bool Contains(string other) => Value.Contains(other, StringComparison.OrdinalIgnoreCase);

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

    public static implicit operator HttpHeaderValue(string value) => new([value]);
    public static implicit operator string(HttpHeaderValue value) => value.Value;

    /// <inheritdoc cref="Value"/>
    public override string ToString() => Value;
}