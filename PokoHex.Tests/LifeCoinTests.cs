using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using PokoHex.Core;
using Xunit;
using static System.Buffers.Binary.BinaryPrimitives;

namespace PokoHex.Tests;

public static class LifeCoinTests
{
    [Fact]
    public static void EditAndRoundTripLifeCoins()
    {
        var save = SaveFolder.ReadFiles(GetFiles(SaveLayout.Version381));
        Assert.True(save.Common.LifeCoinsEditable);
        Assert.Equal(82930, save.Common.LifeCoins);
        save.Common.LifeCoins = 999_999;
        var reopened = SaveFolder.ReadFiles(save.ExportFiles());
        Assert.True(reopened.Common.LifeCoinsEditable);
        Assert.Equal(999_999, reopened.Common.LifeCoins);
        Assert.Throws<ArgumentOutOfRangeException>(() => save.Common.LifeCoins = -1);
    }

    [Fact]
    public static void LifeCoinsUnavailableOnOlderLayout()
    {
        var save = SaveFolder.ReadFiles(GetFiles(SaveLayout.Version70));
        Assert.False(save.Common.LifeCoinsEditable);
        Assert.Equal(0, save.Common.LifeCoins);
        Assert.Throws<InvalidOperationException>(() => save.Common.LifeCoins = 1);
    }

    private static Dictionary<string, byte[]> GetFiles(SaveLayout layout)
    {
        using var output = new MemoryStream();
        output.Write(new byte[layout.HeaderSize]);
        var raw = new byte[layout.DataSize];
        if (layout.LifeCoinsOffset != 0)
            WriteInt32LittleEndian(raw.AsSpan(layout.LifeCoinsOffset), 82930);
        using (var zlib = new ZLibStream(output, CompressionLevel.Fastest, true))
            zlib.Write(raw);
        var common = output.ToArray();
        WriteUInt32LittleEndian(common, 0x31227687);
        WriteUInt32LittleEndian(common.AsSpan(4), layout.Version);
        WriteUInt32LittleEndian(common.AsSpan(8), 0x40001);
        WriteInt32LittleEndian(common.AsSpan(0x10), common.Length);
        return new Dictionary<string, byte[]>
        {
            ["G00/CMNDAT.BIN"] = common,
            ["S00/opaque.dat"] = [1, 2, 3],
        };
    }
}
