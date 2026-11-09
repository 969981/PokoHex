using System;

namespace PokoHex.Core;

public sealed class FunctionFlags
{
    public const int Count = 250;
    public const int SIZE = 32;
    private readonly Memory<byte> Raw;

    internal FunctionFlags(Memory<byte> raw) => Raw = raw;

    public bool GetFlag(int index)
    {
        ValidateIndex(index);
        return (Raw.Span[index >> 3] & (1 << (index & 7))) != 0;
    }

    public void SetFlag(int index, bool value)
    {
        ValidateIndex(index);
        var mask = 1 << (index & 7);
        var offset = index >> 3;
        Raw.Span[offset] = value ? (byte)(Raw.Span[offset] | mask) : (byte)(Raw.Span[offset] & ~mask);
    }

    private static void ValidateIndex(int index)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(index, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(index, Count);
    }
}
