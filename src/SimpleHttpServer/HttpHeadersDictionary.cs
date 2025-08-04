using System.Collections;
using System.Diagnostics.CodeAnalysis;

// ReSharper disable UnusedMember.Global

namespace SimpleHttpServer;

/// <summary>
/// Dictionary to hold HTTP headers.
/// </summary>
/// <remarks>
/// Uses case-insensitive string comparison for header names.
/// </remarks>
public class HttpHeadersDictionary : IReadOnlyDictionary<string, HttpHeaderValue>
{
    private readonly Dictionary<string, HttpHeaderValue> _headers = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Add a header to the dictionary. If the header already exists, the value is added to the existing list.
    /// </summary>
    public void Add(string key, string value)
    {
        ArgumentException.ThrowIfNullOrEmpty(key);
        ArgumentNullException.ThrowIfNull(value);
        if (_headers.TryGetValue(key, out var current))
            _headers[key] = current.Add(value);
        else
            _headers.Add(key, value);
    }

    /// <summary>
    /// Check if a header with the specified name exists.
    /// </summary>
    public bool Contains(string name) => _headers.ContainsKey(name);

    /// <summary>
    /// Total number of headers in the dictionary.
    /// </summary>
    public int Count => _headers.Count;

    /// <summary>
    /// Gets the first value of the specified header key.
    /// </summary>
    public HttpHeaderValue this[string name] => _headers.TryGetValue(name, out var value)
        ? value
        : throw new KeyNotFoundException($"Header not found: {name}");

    /// <summary>
    /// Gets the first value of the specified header key, or null if not found.
    /// </summary>
    /// <param name="name">Header name</param>
    /// <param name="value">The first value of the header, if found. If not found, <c>null</c></param>
    /// <returns>
    /// <c>true</c> if the header exists, otherwise <c>false</c>.
    /// </returns>
    public bool TryGetValue(string name, [MaybeNullWhen(false)] out HttpHeaderValue value) =>
        _headers.TryGetValue(name, out value);

    /// <summary>
    /// Gets the first value of the specified header key, or returns a default value if not found.
    /// </summary>
    /// <param name="name">Header name</param>
    /// <param name="defaultValue">Fallback value, returned if the header is not found.</param>
    /// <returns>
    /// The first value of the header if found, otherwise the specified default value.
    /// </returns>
    public HttpHeaderValue? GetValueOrDefault(string name, HttpHeaderValue? defaultValue = null) => TryGetValue(name, out var value) ? value : defaultValue;

    #region Implementation of IReadOnlyDictionary<string, HttpHeaderValue>

    public IEnumerator<KeyValuePair<string, HttpHeaderValue>> GetEnumerator() => _headers.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)_headers).GetEnumerator();

    bool IReadOnlyDictionary<string, HttpHeaderValue>.ContainsKey(string key) =>
        _headers.ContainsKey(key);

    public IEnumerable<string> Keys => _headers.Keys;

    public IEnumerable<HttpHeaderValue> Values => _headers.Values;

    #endregion
}