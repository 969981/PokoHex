using System;
using static System.Buffers.Binary.BinaryPrimitives;

namespace PokoHex.Core;

public sealed record MapBuildingInfo(ushort ItemId, ushort FloorVariation, ushort RoofVariation)
{
    public const int Count = 128;
    public const int SIZE = 6;
    private static readonly MapBuildingInfo[] Buildings = ReadBuildings();
    public static MapBuildingInfo Get(byte type) => type < Buildings.Length ? Buildings[type] : Buildings[0];

    private static MapBuildingInfo[] ReadBuildings()
    {
        var data = ResourceUtil.GetBinaryResource("map_buildings.bin");
        if (data.Length != Count * SIZE)
            throw new InvalidOperationException("The building catalog is invalid.");
        var result = new MapBuildingInfo[Count];
        for (int i = 0; i < result.Length; i++)
        {
            var row = data.AsSpan(i * SIZE, SIZE);
            result[i] = new(ReadUInt16LittleEndian(row), ReadUInt16LittleEndian(row[2..]), ReadUInt16LittleEndian(row[4..]));
        }
        return result;
    }
}
