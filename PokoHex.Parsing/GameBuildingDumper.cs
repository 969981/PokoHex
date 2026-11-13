using System;
using System.IO;
using PokoHex.Core;
using static System.Buffers.Binary.BinaryPrimitives;

namespace PokoHex.Parsing;

public static class GameBuildingDumper
{
    public static byte[] GetBuildingList(string path)
    {
        using var archive = new GameArchive(GameItemDumper.GetArchiveFolder(path));
        if (archive.EntryCount != 12298)
            throw new InvalidDataException("The building parameter version is not supported.");
        var buildings = archive.Read(51);
        var parts = archive.Read(40);
        var variations = archive.Read(41);
        CheckTable(buildings, 128, 0, 92);
        CheckTable(parts, 4096, 0, 80);
        CheckTable(variations, 4096, 3, 16);
        var result = new byte[MapBuildingInfo.Count * MapBuildingInfo.SIZE];
        for (int i = 0; i < MapBuildingInfo.Count; i++)
        {
            var building = buildings.AsSpan(64 + i * 92, 92);
            var variation = ReadUInt16LittleEndian(building[0x32..]);
            if (variation >= 4096)
                throw new InvalidDataException("A building definition is outside its table.");
            var part = ReadUInt16LittleEndian(variations.AsSpan(64 + variation * 16 + 4));
            if (part >= 4096)
                throw new InvalidDataException("A building definition is outside its table.");
            var item = ReadUInt16LittleEndian(parts.AsSpan(64 + part * 80 + 6));
            if (item >= 4600)
                throw new InvalidDataException("A building item is outside its table.");
            var row = result.AsSpan(i * MapBuildingInfo.SIZE, MapBuildingInfo.SIZE);
            WriteUInt16LittleEndian(row, item);
            WriteUInt16LittleEndian(row[2..], ReadUInt16LittleEndian(building[0x14..]));
            WriteUInt16LittleEndian(row[4..], ReadUInt16LittleEndian(building[0x12..]));
        }
        return result;
    }

    private static void CheckTable(byte[] table, int count, int version, int stride)
    {
        if (table.Length != 64 + count * stride || ReadInt32LittleEndian(table) != count || ReadInt32LittleEndian(table.AsSpan(4)) != version)
            throw new InvalidDataException("The building parameter table is not supported.");
    }
}
