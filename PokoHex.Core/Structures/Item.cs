using System;
using static System.Buffers.Binary.BinaryPrimitives;

namespace PokoHex.Core;

public sealed class Item(Memory<byte> raw, bool paintEditable = true)
{
    public const int SIZE = 5;
    public const uint ItemIdMask = 0x1FFF;
    public const int MaxCount = byte.MaxValue;

    private Span<byte> Data => raw.Span;

    public uint RawValue
    {
        get => ReadUInt32LittleEndian(Data);
        set => WriteUInt32LittleEndian(Data, value);
    }

    public ushort ItemId
    {
        get => (ushort)(RawValue & ItemIdMask);
        set
        {
            ArgumentOutOfRangeException.ThrowIfGreaterThan(value, ItemIdMask);
            RawValue = (RawValue & ~ItemIdMask) | value;
        }
    }

    public byte Count { get => Data[4]; set => Data[4] = value; }
    public bool IsEmpty => ItemId == 0;
    public bool PaintEditable { get; } = paintEditable;
    public int PaintCount => PaintEditable ? ItemPaintInfo.GetColorCount(ItemId) : 0;
    public bool AllowsPattern => PaintEditable && ItemPaintInfo.AllowsPattern(ItemId);

    public ItemPaint Paint
    {
        get
        {
            // Patterns share the first color byte; values 32..131 select patterns 1..100.
            var first = (ushort)((RawValue >> 13) & 255);
            return new ItemPaint(first < 32 ? first : (ushort)0, (ushort)((RawValue >> 21) & 31),
                (ushort)((RawValue >> 26) & 31), first < 32 ? (ushort)0 : (ushort)(first - 31));
        }
        set
        {
            if (Paint == value)
                return;
            var count = PaintCount;
            if (IsEmpty || count == 0)
                throw new ArgumentException("This item cannot be painted.");
            if (value.Pattern > ItemPaintInfo.PatternCount || value.Pattern != 0 && !AllowsPattern)
                throw new ArgumentException("Select a supported pattern.");
            if (value.Pattern == 0 && value.Color1 > ItemPaintInfo.ColorCount ||
                count > 1 && value.Color2 > ItemPaintInfo.ColorCount ||
                count > 2 && value.Color3 > ItemPaintInfo.ColorCount)
                throw new ArgumentException("Select a supported color.");

            uint first = value.Pattern == 0 ? value.Color1 : (uint)value.Pattern + 31;
            var result = (RawValue & ~0x001FE000u) | (first << 13);
            if (count > 1)
                result = (result & ~0x03E00000u) | (uint)value.Color2 << 21;
            if (count > 2)
                result = (result & ~0x7C000000u) | (uint)value.Color3 << 26;
            RawValue = result;
        }
    }

    public void Set(ushort itemId, int count)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThan(itemId, ItemIdMask);
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(count, MaxCount);
        if (itemId == 0 || count == 0)
        {
            Data.Clear();
            return;
        }

        // Variant and placement bits belong to the old item.
        if (ItemId != itemId)
            RawValue = 0;
        ItemId = itemId;
        Count = (byte)count;
    }
}
