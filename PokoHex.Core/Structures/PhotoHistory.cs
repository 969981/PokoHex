using System;

namespace PokoHex.Core;

public sealed class PhotoHistory(Memory<byte> raw)
{
    public const int SIZE = 128;

    public bool IsCompleted(int index)
    {
        ValidateIndex(index);
        return (raw.Span[index] & 1) != 0;
    }

    internal void SetCompleted(int index, bool value)
    {
        ValidateIndex(index);
        ref var data = ref raw.Span[index];
        data = value ? (byte)(data | 1) : (byte)(data & ~1);
    }

    private static void ValidateIndex(int index)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(index, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(index, SIZE);
    }
}
