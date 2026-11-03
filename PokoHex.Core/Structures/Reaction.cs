using System;

namespace PokoHex.Core;

public sealed class Reaction
{
    private static readonly string[] Names = ResourceUtil.GetStringList("reaction", "en");
    private readonly Memory<byte> Raw;
    public int Index { get; }
    public bool Learned => (Raw.Span[0] & 1) != 0;

    internal Reaction(int index, Memory<byte> raw)
    {
        Index = index;
        Raw = raw;
    }

    internal void Learn() => Raw.Span[0] |= 1;

    public static bool IsPublic(int index) => index is >= 1 and <= 100 && index < Names.Length && Names[index].Length != 0;
}
