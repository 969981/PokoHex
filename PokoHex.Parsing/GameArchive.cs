using System;
using System.IO;
using System.IO.Compression;
using static System.Buffers.Binary.BinaryPrimitives;

namespace PokoHex.Parsing;

public sealed class GameArchive : IDisposable
{
    private const int MaxEntrySize = 64 * 1024 * 1024;
    private readonly byte[] Index;
    private readonly FileStream Data;
    public int EntryCount => Index.Length / 32;

    public GameArchive(string folder)
    {
        Index = File.ReadAllBytes(Path.Combine(folder, "LI_C0_R.IDX"));
        if (Index.Length == 0 || Index.Length % 32 != 0)
            throw new InvalidDataException("The game archive index is invalid.");
        Data = File.OpenRead(Path.Combine(folder, "LD_C0_R.BIN"));
    }

    public byte[] Read(int index)
    {
        if ((uint)index >= Index.Length / 32)
            throw new InvalidDataException("The game archive version is not supported.");
        var entry = Index.AsSpan(index * 32, 32);
        var offset = ReadInt64LittleEndian(entry);
        var rawSize = ReadInt64LittleEndian(entry[8..]);
        var packedSize = ReadInt64LittleEndian(entry[16..]);
        var flag = ReadInt64LittleEndian(entry[24..]);
        var size = flag == 0 ? rawSize : packedSize;
        if (flag is not (0 or 1) || rawSize is < 0 or > MaxEntrySize || packedSize is < 0 or > MaxEntrySize ||
            (flag == 0 && packedSize != rawSize) || (size == 0 && rawSize != 0) ||
            offset < 0 || offset > Data.Length - size)
            throw new InvalidDataException("The game archive entry is invalid.");
        if (size == 0)
            return [];
        var data = new byte[(int)size];
        Data.Position = offset;
        Data.ReadExactly(data);
        if (flag == 0)
            return data;
        return Decompress(data, (int)rawSize);
    }

    private static byte[] Decompress(byte[] data, int rawSize)
    {
        if (data.Length < 12 || ReadInt32LittleEndian(data.AsSpan(8)) != rawSize)
            throw new InvalidDataException("The compressed archive header is invalid.");
        var blockSize = ReadInt32LittleEndian(data);
        if (blockSize is not (0x10000 or 0x20000 or 0x80000))
            throw new InvalidDataException("The archive block size is not supported.");
        var count = ReadInt32LittleEndian(data.AsSpan(4));
        if (count < 0 || count != (rawSize + blockSize - 1) / blockSize)
            throw new InvalidDataException("The archive block count is invalid.");
        var headerSize = 12 + (count * 4);
        var position = (headerSize + 127) & ~127;
        if (position > data.Length || data.AsSpan(headerSize, position - headerSize).ContainsAnyExcept((byte)0))
            throw new InvalidDataException("The archive block table is invalid.");
        var result = new byte[rawSize];
        var written = 0;
        for (int i = 0; i < count; i++)
        {
            var slotSize = ReadInt32LittleEndian(data.AsSpan(12 + (i * 4)));
            if (slotSize <= 0 || slotSize > data.Length - position)
                throw new InvalidDataException("The archive block length is invalid.");
            var expected = Math.Min(blockSize, result.Length - written);
            if (slotSize == expected)
                data.AsSpan(position, slotSize).CopyTo(result.AsSpan(written, expected));
            else
            {
                // Raw slots have no prefix; compressed slots include a four-byte stream length.
                if (slotSize <= 4)
                    throw new InvalidDataException("The archive block length is invalid.");
                var length = ReadInt32LittleEndian(data.AsSpan(position));
                if (length < 6 || length != slotSize - 4)
                    throw new InvalidDataException("The archive block length is invalid.");
                using var input = new MemoryStream(data, position + 4, length, false);
                using var zlib = new ZLibStream(input, CompressionMode.Decompress);
                zlib.ReadExactly(result.AsSpan(written, expected));
                if (zlib.ReadByte() != -1)
                    throw new InvalidDataException("The archive block size is invalid.");
                // ZLibStream can return all output even when the checksum trailer is incomplete.
                if (GetAdler32(result.AsSpan(written, expected)) != ReadUInt32BigEndian(data.AsSpan(position + slotSize - 4)))
                    throw new InvalidDataException("The archive block checksum is invalid.");
            }
            written += expected;
            var end = position + slotSize;
            position = (end + 127) & ~127;
            if (position > data.Length || data.AsSpan(end, position - end).ContainsAnyExcept((byte)0))
                throw new InvalidDataException("The archive block padding is invalid.");
        }
        if (position != data.Length)
            throw new InvalidDataException("The compressed archive length is invalid.");
        return result;
    }

    private static uint GetAdler32(ReadOnlySpan<byte> data)
    {
        uint a = 1;
        uint b = 0;
        foreach (var value in data)
        {
            a = (a + value) % 65521;
            b = (b + a) % 65521;
        }
        return (b << 16) | a;
    }

    public void Dispose() => Data.Dispose();
}
