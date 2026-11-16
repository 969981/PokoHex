using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using static System.Buffers.Binary.BinaryPrimitives;

namespace PokoHex.Core;

public sealed class ScreenshotSave
{
    public const uint Version = 0x04000006;
    public const int HeaderSize = 0x40;
    public const int DataSize = 0x2DE7603;
    public const int SlotCount = 187;
    public const int ImageSize = 256000;
    private const int Metadata = SlotCount * 4;
    private const int PoolSize = Metadata + SlotCount * PhotoEntry.SIZE;
    private const int Pool = PoolSize + 8;
    private const int PoolLength = SlotCount * ImageSize;
    private readonly byte[] Original;
    private readonly byte[] Raw;
    private readonly Dictionary<int, byte[]> Changed = [];
    public PhotoEntry[] Entries { get; }
    public ReadOnlyMemory<byte> Data => Raw;
    public bool IsEdited => Changed.Count != 0;

    private ScreenshotSave(byte[] original, byte[] raw)
    {
        Original = original;
        Raw = raw;
        if (ReadUInt64LittleEndian(raw.AsSpan(PoolSize)) != PoolLength ||
            ReadUInt64LittleEndian(raw.AsSpan(Pool + PoolLength)) > ImageSize ||
            ReadUInt64LittleEndian(raw.AsSpan(Pool + PoolLength + 8)) != ImageSize)
            throw new InvalidDataException("The photo allocation is not supported.");
        Entries = Enumerable.Range(0, SlotCount).Select(i =>
            new PhotoEntry(i, raw.AsMemory(Metadata + i * PhotoEntry.SIZE, PhotoEntry.SIZE))).ToArray();
        if (Entries.Any(z => !z.IsEmpty && z.Slot >= SlotCount))
            throw new InvalidDataException("A photo reference is outside the image buffer.");
    }

    public static ScreenshotSave Read(byte[] data)
    {
        if (data.Length < HeaderSize || ReadUInt32LittleEndian(data) != 0x31227687 ||
            ReadUInt32LittleEndian(data.AsSpan(4)) != Version || ReadUInt32LittleEndian(data.AsSpan(8)) != 0x40001)
            throw new InvalidDataException("This photo save version is not supported.");
        var length = ReadInt32LittleEndian(data.AsSpan(0x10));
        if (length <= HeaderSize || length > data.Length)
            throw new InvalidDataException("The compressed photo length is invalid.");
        using var input = new MemoryStream(data, HeaderSize, length - HeaderSize, false);
        using var zlib = new ZLibStream(input, CompressionMode.Decompress);
        var raw = new byte[DataSize];
        zlib.ReadExactly(raw);
        if (zlib.ReadByte() != -1)
            throw new InvalidDataException("The photo save layout is not supported.");
        return new ScreenshotSave((byte[])data.Clone(), raw);
    }

    public ReadOnlyMemory<byte> GetImage(PhotoEntry entry)
    {
        if (entry.IsEmpty || !Entries.Contains(entry))
            return ReadOnlyMemory<byte>.Empty;
        var word = ReadUInt32LittleEndian(Raw.AsSpan((int)entry.Slot * 4));
        var size = (int)(word & 0x0FFFFFFF);
        if ((word >> 28) != 0 || size is < 4 or > ImageSize)
            return ReadOnlyMemory<byte>.Empty;
        var data = Raw.AsMemory(Pool + (int)entry.Slot * ImageSize, size);
        return data.Span[0] == 0xFF && data.Span[1] == 0xD8 && data.Span[^2] == 0xFF && data.Span[^1] == 0xD9
            ? data : ReadOnlyMemory<byte>.Empty;
    }

    internal void SetEntry(int index, ReadOnlySpan<byte> data)
    {
        var entry = Entries[index];
        if (!Changed.TryGetValue(index, out var original))
        {
            original = entry.Write();
            Changed.Add(index, original);
        }
        entry.Read(data);
        if (data.SequenceEqual(original))
            Changed.Remove(index);
    }

    public byte[] Write()
    {
        if (!IsEdited)
            return (byte[])Original.Clone();
        using var compressed = new MemoryStream();
        using (var zlib = new ZLibStream(compressed, CompressionLevel.SmallestSize, true))
            zlib.Write(Raw);
        var length = checked(HeaderSize + (int)compressed.Length);
        var result = new byte[Math.Max(length, Original.Length)];
        Original.AsSpan(0, HeaderSize).CopyTo(result);
        compressed.GetBuffer().AsSpan(0, (int)compressed.Length).CopyTo(result.AsSpan(HeaderSize));
        WriteInt32LittleEndian(result.AsSpan(0x10), length);
        return result;
    }
}
