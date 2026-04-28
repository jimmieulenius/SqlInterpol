using System;
using System.Buffers;
using System.Runtime.CompilerServices;

namespace SqlInterpol.Parsing;

/// <summary>
/// A high-performance, stack-friendly string builder that uses a Span as its buffer.
/// </summary>
internal ref struct ValueStringBuilder
{
    private char[]? _arrayToReturn;
    private Span<char> _chars;
    private int _pos;

    public ValueStringBuilder(Span<char> initialBuffer)
    {
        _arrayToReturn = null;
        _chars = initialBuffer;
        _pos = 0;
    }

    public int Length => _pos;

    public void Append(char c)
    {
        int pos = _pos;
        if ((uint)pos < (uint)_chars.Length)
        {
            _chars[pos] = c;
            _pos = pos + 1;
        }
        else
        {
            GrowAndAppend(c);
        }
    }

    public void Append(string? s)
    {
        if (string.IsNullOrEmpty(s)) return;

        int pos = _pos;
        if (s.Length == 1 && (uint)pos < (uint)_chars.Length)
        {
            _chars[pos] = s[0];
            _pos = pos + 1;
        }
        else
        {
            AppendSlow(s);
        }
    }

    private void AppendSlow(string s)
    {
        int pos = _pos;
        if (pos > _chars.Length - s.Length)
        {
            Grow(s.Length);
        }

        s.AsSpan().CopyTo(_chars.Slice(pos));
        _pos = pos + s.Length;
    }

    public void Append(ReadOnlySpan<char> value)
    {
        int pos = _pos;
        if (pos > _chars.Length - value.Length)
        {
            Grow(value.Length);
        }

        value.CopyTo(_chars.Slice(_pos));
        _pos += value.Length;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void GrowAndAppend(char c)
    {
        Grow(1);
        Append(c);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void Grow(int requiredMinCapacity)
    {
        // Standard growth strategy: double the buffer or take what is required
        int newCapacity = Math.Max(_pos + requiredMinCapacity, _chars.Length * 2);
        
        char[] poolArray = ArrayPool<char>.Shared.Rent(newCapacity);
        _chars.Slice(0, _pos).CopyTo(poolArray);

        if (_arrayToReturn != null)
        {
            ArrayPool<char>.Shared.Return(_arrayToReturn);
        }

        _chars = _arrayToReturn = poolArray;
    }

    public override string ToString()
    {
        string s = _chars.Slice(0, _pos).ToString();
        Dispose();
        return s;
    }

    public void Dispose()
    {
        char[]? toReturn = _arrayToReturn;
        this = default; // Clear the struct
        if (toReturn != null)
        {
            ArrayPool<char>.Shared.Return(toReturn);
        }
    }
}