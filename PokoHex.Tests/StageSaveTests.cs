using System;
using System.IO;
using System.IO.Compression;
using PokoHex.Core;
using Xunit;
using static System.Buffers.Binary.BinaryPrimitives;

namespace PokoHex.Tests;

public static class StageSaveTests
{
    [Fact]
    public static void PreserveStaleLinksAndValidatePoints()
    {
        var raw = new byte[StageSave.DataSize];
        raw.AsSpan(0x58571B, 0x2000).Fill(0xFF);
        WriteUInt16LittleEndian(raw.AsSpan(0x58571B), 0);
        WriteInt32LittleEndian(raw.AsSpan(0x58771B), 1);
        WriteInt32LittleEndian(raw.AsSpan(0x58771F), 2);
        // Forward link and a stale link beyond the active pool, as in issue #8.
        WriteUInt32LittleEndian(raw.AsSpan(0x587723), 1);
        WriteUInt32LittleEndian(raw.AsSpan(0x587727), 1498);
        WriteUInt32LittleEndian(raw.AsSpan(0x587733), 5);
        WriteUInt32LittleEndian(raw.AsSpan(0x587737), 1498);
        WriteUInt32LittleEndian(raw.AsSpan(0x58773B), 1 << 4);
        WriteUInt32LittleEndian(raw.AsSpan(0x1207727), 1 << 12);
        var original = Pack(raw);
        var stage = StageSave.Read(original);
        Assert.Equal(2, stage.Parts.Length);
        Assert.Equal(1u, stage.Parts[0].Link);
        Assert.Equal(5u, stage.Parts[1].Link);
        Assert.False(stage.IsEdited);
        Assert.Equal(original, stage.Write());

        WriteUInt32LittleEndian(raw.AsSpan(0x1207727), 0);
        Assert.Throws<InvalidDataException>(() => StageSave.Read(Pack(raw)));
        WriteUInt32LittleEndian(raw.AsSpan(0x1207727), (1 << 12) | 1);
        Assert.Throws<InvalidDataException>(() => StageSave.Read(Pack(raw)));
    }

    private static byte[] Pack(byte[] raw)
    {
        using var output = new MemoryStream();
        output.Write(new byte[StageSave.HeaderSize]);
        using (var zlib = new ZLibStream(output, CompressionLevel.Fastest, true))
            zlib.Write(raw);
        var data = output.ToArray();
        WriteUInt32LittleEndian(data, 0x31227687);
        WriteUInt32LittleEndian(data.AsSpan(4), StageSave.Version);
        WriteInt32LittleEndian(data.AsSpan(0x10), data.Length);
        return data;
    }
}
