using System;
using PokoHex.Core;
using Xunit;

namespace PokoHex.Tests;

public static class ItemPaintTests
{
    [Fact]
    public static void PreserveFlagsAndValidatePaint()
    {
        var item = new Item(new byte[Item.SIZE]) { RawValue = 0x80000000u | 1510, Count = 1 };
        item.Paint = new(1, 2, 18, 100);
        Assert.Equal(new ItemPaint(0, 2, 18, 100), item.Paint);
        Assert.Equal(0x80000000u, item.RawValue & 0x80000000u);
        var before = item.RawValue;
        Assert.Throws<ArgumentException>(() => item.Paint = new(3, 4, 19, 0));
        Assert.Equal(before, item.RawValue);
        item.Paint = new(1, 2, 18, 0);
        Assert.Equal(new ItemPaint(1, 2, 18, 0), item.Paint);
        item.Set(2340, 60);
        Assert.Equal(2340u, item.RawValue);
        Assert.Throws<ArgumentException>(() => item.Paint = new(1, 0, 0, 0));
        Assert.Equal(2340u, item.RawValue);
    }
}
