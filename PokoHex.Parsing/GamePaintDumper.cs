using System;
using System.IO;
using PokoHex.Core;
using static System.Buffers.Binary.BinaryPrimitives;

namespace PokoHex.Parsing;

public static class GamePaintDumper
{
    public static byte[] GetPaintList(string path)
    {
        using var archive = new GameArchive(GameItemDumper.GetArchiveFolder(path));
        if (archive.EntryCount != 12298)
            throw new InvalidDataException("The paint parameter version is not supported.");
        var items = archive.Read(983);
        var blocks = archive.Read(34);
        var parts = archive.Read(40);
        var variations = archive.Read(41);
        CheckTable(items, 4600, 3, 68);
        CheckTable(blocks, 2048, 3, 16);
        CheckTable(parts, 4096, 0, 80);
        CheckTable(variations, 4096, 3, 16);
        var result = new byte[ItemPaintInfo.ItemCount];
        for (int i = 1; i < result.Length; i++)
        {
            var item = items.AsSpan(64 + i * 68, 68);
            var value = ReadUInt32LittleEndian(item[12..]) & 0xFFFF;
            ushort part;
            if (item[0x3A] == 1 && value is > 0 and < 2048)
                part = ReadUInt16LittleEndian(blocks.AsSpan(64 + (int)value * 16 + 8));
            else if (item[0x3A] == 2 && value is > 0 and < 4096)
                part = ReadUInt16LittleEndian(variations.AsSpan(64 + (int)value * 16 + 4));
            else
                continue;
            if (part >= 4096)
                throw new InvalidDataException("A paint definition is outside its table.");
            if (part == 0)
                continue;
            var row = parts.AsSpan(64 + part * 80, 80);
            // The four-area house path uses a different packed layout.
            if (row[0x41] is >= 2 and <= 4)
                result[i] = (byte)(row[0x41] - 1 | ((row[0x4D] & 8) != 0 ? 4 : 0));
        }
        return result;
    }

    private static void CheckTable(byte[] table, int count, int version, int stride)
    {
        if (table.Length != 64 + count * stride || ReadInt32LittleEndian(table) != count || ReadInt32LittleEndian(table.AsSpan(4)) != version)
            throw new InvalidDataException("The paint parameter table is not supported.");
    }
}
