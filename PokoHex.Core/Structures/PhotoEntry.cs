using System;
using static System.Buffers.Binary.BinaryPrimitives;

namespace PokoHex.Core;

public sealed class PhotoEntry(int index, Memory<byte> raw)
{
    public const int SIZE = 26;
    public int Index { get; } = index;
    public ulong Slot => ReadUInt64LittleEndian(raw.Span);
    public bool IsEmpty => Slot == ulong.MaxValue;
    public uint OpId => ReadUInt32LittleEndian(raw.Span[20..]);
    public byte Kind => raw.Span[25];
    internal bool CanReset => Kind is 1 or 4 && ReadUInt32LittleEndian(raw.Span[16..]) == 0 && raw.Span[24] == 0;
    internal byte[] Write() => raw.ToArray();
    internal void Read(ReadOnlySpan<byte> data) => data.CopyTo(raw.Span);
}
