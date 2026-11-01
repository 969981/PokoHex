using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using PokoHex.Core;
using Xunit;
using static System.Buffers.Binary.BinaryPrimitives;

namespace PokoHex.Tests;

public static class SaveFolderTests
{
    [Fact]
    public static void PreserveImportedFiles()
    {
        var source = GetFiles();
        var save = SaveFolder.ReadFiles(source);
        var output = save.ExportFiles();
        foreach (var (path, data) in source)
            Assert.Equal(data, output[path.Replace('/', Path.DirectorySeparatorChar)]);
        source["S00/opaque.dat"][0] = 99;
        output[Path.Combine("S00", "opaque.dat")][0] = 88;
        Assert.Equal(new byte[] { 1, 2, 3 }, save.ExportFiles()[Path.Combine("S00", "opaque.dat")]);
        Assert.Contains(save.SaveParts, x => x.RelativePath == "G00/SCSHDAT.BIN"
            && x.Kind == "SCSHDAT (unverified)" && !x.SupportsEditing
            && x.OriginalSha256.Length == 64);
        Assert.Contains(save.SaveParts, x => x.RelativePath == "G00/STGDAT01.BIN"
            && x.SupportsEditing);
        save.Common.PlayerName = "Web";
        var reopened = SaveFolder.ReadFiles(save.ExportFiles());
        Assert.Equal("Web", reopened.Common.PlayerName);
        Assert.Equal(new byte[] { 1, 2, 3 }, reopened.ExportFiles()[Path.Combine("S00", "opaque.dat")]);
        Assert.Equal(source["G00/SCSHDAT.BIN"], reopened.ExportFiles()[Path.Combine("G00", "SCSHDAT.BIN")]);
    }

    [Theory]
    [InlineData("../file")]
    [InlineData("/file")]
    [InlineData("G00/../file")]
    [InlineData("C:/file")]
    [InlineData("G00//file")]
    [InlineData("G00/./file")]
    public static void RejectInvalidImportedPath(string path)
    {
        var files = GetFiles();
        files.Add(path, []);
        Assert.Throws<InvalidDataException>(() => SaveFolder.ReadFiles(files));
    }

    [Fact]
    public static void RejectDuplicateImportedPaths()
    {
        var duplicate = GetFiles();
        duplicate.Add("g00\\cmndat.bin", duplicate["G00/CMNDAT.BIN"]);
        Assert.Throws<InvalidDataException>(() => SaveFolder.ReadFiles(duplicate));
    }

    [Fact]
    public static void RejectMissingCommonFile()
    {
        var missing = GetFiles();
        missing.Remove("G00/CMNDAT.BIN");
        Assert.Throws<InvalidDataException>(() => SaveFolder.ReadFiles(missing));
    }

    [Fact]
    public static void ImportG00OnlyFlatArchiveAndKeepUnknownFiles()
    {
        var files = GetFiles();
        var flat = new Dictionary<string, byte[]>
        {
            ["CMNDAT.BIN"] = files["G00/CMNDAT.BIN"],
            ["SCSHDAT.BIN"] = files["G00/SCSHDAT.BIN"],
            ["STGDAT01.BIN"] = files["G00/STGDAT01.BIN"],
        };
        var save = SaveFolder.ReadFiles(flat);
        Assert.Single(save.StageIds);
        Assert.Equal(1, save.StageIds[0]);
        Assert.Equal(3, save.SaveParts.Length);
        var output = save.ExportFiles();
        Assert.Equal(flat["SCSHDAT.BIN"], output[Path.Combine("G00", "SCSHDAT.BIN")]);
        Assert.Equal(flat["STGDAT01.BIN"], output[Path.Combine("G00", "STGDAT01.BIN")]);
    }

    [Fact]
    public static void ImportG00OnlyOnDiskAndExportToNewRoot()
    {
        var folder = Path.Combine(Path.GetTempPath(), "PokoHex-G00-" + Guid.NewGuid().ToString("N"));
        var destination = Path.Combine(Path.GetTempPath(), "PokoHex-copy-" + Guid.NewGuid().ToString("N"));
        try
        {
            Directory.CreateDirectory(folder);
            var files = GetFiles();
            foreach (var file in new[] { "CMNDAT.BIN", "SCSHDAT.BIN", "STGDAT01.BIN" })
                File.WriteAllBytes(Path.Combine(folder, file), files["G00/" + file]);
            var save = SaveFolder.Read(folder);
            save.Export(destination);
            Assert.Equal(files["G00/SCSHDAT.BIN"], File.ReadAllBytes(Path.Combine(destination, "G00", "SCSHDAT.BIN")));
            Assert.False(Directory.Exists(Path.Combine(destination, "S00")));
        }
        finally
        {
            if (Directory.Exists(folder)) Directory.Delete(folder, true);
            if (Directory.Exists(destination)) Directory.Delete(destination, true);
        }
    }

    private static Dictionary<string, byte[]> GetFiles()
    {
        var layout = SaveLayout.Version70;
        using var output = new MemoryStream();
        output.Write(new byte[layout.HeaderSize]);
        using (var zlib = new ZLibStream(output, CompressionLevel.Fastest, true))
            zlib.Write(new byte[layout.DataSize]);
        var common = output.ToArray();
        WriteUInt32LittleEndian(common, 0x31227687);
        WriteUInt32LittleEndian(common.AsSpan(4), layout.Version);
        WriteUInt32LittleEndian(common.AsSpan(8), 0x40001);
        WriteInt32LittleEndian(common.AsSpan(0x10), common.Length);
        return new Dictionary<string, byte[]>
        {
            ["G00/CMNDAT.BIN"] = common,
            ["G00/SCSHDAT.BIN"] = [4, 3, 2, 1],
            ["G00/STGDAT01.BIN"] = [0x53, 0x54, 0x47],
            ["S00/opaque.dat"] = [1, 2, 3],
        };
    }
}
