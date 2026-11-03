using System;
using static System.Buffers.Binary.BinaryPrimitives;

namespace PokoHex.Core;

public sealed class Skill
{
    public const int SIZE = 8;
    private readonly Memory<byte> Raw;
    public int Index { get; }
    public uint Uses => ReadUInt32LittleEndian(Raw.Span);
    public bool Learned => (Raw.Span[4] & 1) != 0;

    internal Skill(int index, Memory<byte> raw)
    {
        Index = index;
        Raw = raw;
    }

    internal void Learn() => Raw.Span[4] |= 1;

    public static bool IsPublic(int index) => index is >= 1 and <= 6 or >= 10 and <= 13;
    public static bool CanUseWheel(int index) => index is >= 1 and <= 6 or 12 or 13;
}
