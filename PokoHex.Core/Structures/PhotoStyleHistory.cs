using System;

namespace PokoHex.Core;

public sealed class PhotoStyleHistory(Memory<byte> raw)
{
    public const int FrameCount = 64;
    public const int FilterCount = 16;
    public const int SIZE = FrameCount + FilterCount;

    public bool IsUnlocked(int index, bool filters = false)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(index);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(index, filters ? FilterCount : FrameCount);
        return index == 0 || (raw.Span[(filters ? FrameCount : 0) + index] & 1) != 0;
    }
}
