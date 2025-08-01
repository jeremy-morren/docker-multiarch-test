using System.Diagnostics;

namespace SimpleHttpServer;

public class BufferReader
{
    private readonly Stream _stream;

    private readonly byte[] _buffer;

    public BufferReader(Stream stream, int bufferSize = 512)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(bufferSize);

        _stream = stream;
        _buffer = new byte[bufferSize];
    }

    private int _bufferPosition = -1;
    private int _bufferLength = int.MaxValue;

    /// <summary>
    /// Reads as many bytes as possible from the stream, up to <paramref name="count"/>
    /// </summary>
    public ReadOnlyMemory<byte> Read(int count)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        if (count == 0)
            return ReadOnlyMemory<byte>.Empty;

        if (_bufferLength == -1)
            throw new EndOfStreamException();

        if (_bufferPosition != -1 && _bufferPosition + count <= _bufferLength)
        {
            // We have enough data in the buffer to satisfy the request
            var memory = _buffer.AsMemory(_bufferPosition, count);
            _bufferPosition += count;
            return memory;
        }

        // We don't have enough data in the buffer, so we need to read from the stream

        var result = new byte[count];
        var resultPos = 0;

        while (true)
        {
            Debug.Assert(resultPos < result.Length, "resultPos < result.Length");

            if (_bufferPosition != -1 && _bufferPosition < _bufferLength)
            {
                // We have some data in the buffer
                var bufferRem = _bufferLength - _bufferPosition;
                var resultRem = result.Length - resultPos;

                var rem = Math.Min(bufferRem, resultRem);
                _buffer.AsMemory(_bufferPosition, rem).CopyTo(result.AsMemory(resultPos, rem));
                _bufferPosition += rem;
                resultPos += rem;

                if (resultPos == result.Length)
                    // We have read enough data
                    return result;
            }

            // Read more data from the stream
            _bufferLength = _stream.Read(_buffer, 0, _buffer.Length);
            _bufferPosition = 0;
            if (_bufferLength != 0) continue;

            // No more data to read from the stream
            _bufferLength = -1;
            return result.AsMemory(0, resultPos + 1);
        }
    }

    public byte ReadByte()
    {
        var result = Read(1);
        if (result.Length == 0)
            throw new EndOfStreamException();
        Debug.Assert(result.Length == 1);
        return result.Span[0];
    }
}