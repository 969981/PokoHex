using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using PokoHex.Core;

namespace PokoHex.Web;

public static class SaveArchive
{
    public const int MaximumFiles = 512;
    public const long MaximumFileSize = 64 * 1024 * 1024;
    public const long MaximumTotalSize = 256 * 1024 * 1024;

    public static SaveFolder ReadZip(Stream stream)
    {
        using var archive = new ZipArchive(stream, ZipArchiveMode.Read, true);
        var entries = archive.Entries.Where(z => !z.FullName.EndsWith('/') && !z.FullName.EndsWith('\\')).ToArray();
        ValidateSize(entries.Length, entries.Select(z => z.Length));
        var files = new List<KeyValuePair<string, byte[]>>();
        foreach (var entry in entries)
        {
            using var input = entry.Open();
            var data = new byte[checked((int)entry.Length)];
            input.ReadExactly(data);
            if (input.ReadByte() != -1)
                throw new InvalidDataException("The ZIP entry length is invalid.");
            files.Add(new(entry.FullName, data));
        }
        return ReadFiles(files);
    }

    public static SaveFolder ReadFiles(IEnumerable<KeyValuePair<string, byte[]>> source)
    {
        var files = source.Select(z => new KeyValuePair<string, byte[]>(z.Key.Replace('\\', '/'), z.Value)).ToArray();
        ValidateSize(files.Length, files.Select(z => (long)z.Value.Length));
        // Prefer a complete save root; otherwise accept archives of just the
        // loose G00 files (CMNDAT.BIN, SCSHDAT.BIN, STGDATxx.BIN).
        var nested = files.Where(z => z.Key.Equals("G00/CMNDAT.BIN", StringComparison.OrdinalIgnoreCase) ||
            z.Key.EndsWith("/G00/CMNDAT.BIN", StringComparison.OrdinalIgnoreCase)).ToArray();
        var flat = files.Where(z => z.Key.Equals("CMNDAT.BIN", StringComparison.OrdinalIgnoreCase) ||
            z.Key.EndsWith("/CMNDAT.BIN", StringComparison.OrdinalIgnoreCase)).ToArray();
        var choices = nested.Length > 0 ? nested : flat;
        if (choices.Length != 1)
            throw new InvalidDataException("Select one save containing G00/CMNDAT.BIN or a G00-only set of BIN files.");
        var suffix = nested.Length > 0 ? "G00/CMNDAT.BIN" : "CMNDAT.BIN";
        var root = choices[0].Key[..^suffix.Length];
        if (files.Any(z => !z.Key.StartsWith(root, StringComparison.OrdinalIgnoreCase)))
            throw new InvalidDataException("The selection contains files outside the save folder.");
        // Validate the wrapper too; it must never hide a traversal or absolute path.
        var segments = root.TrimEnd('/').Split('/');
        if (root.Length != 0 && segments.Any(z => z.Length == 0 || z is "." or ".." || z.Contains(':') || z.Any(char.IsControl)))
            throw new InvalidDataException("The save folder path is invalid.");
        return SaveFolder.ReadFiles(files.Select(z => new KeyValuePair<string, byte[]>(z.Key[root.Length..], z.Value)));
    }

    public static void ValidateSize(int count, IEnumerable<long> lengths)
    {
        if (count is < 1 or > MaximumFiles)
            throw new InvalidDataException($"Select between 1 and {MaximumFiles} save files.");
        long total = 0;
        foreach (var length in lengths)
        {
            if (length < 0 || length > MaximumFileSize || (total += length) > MaximumTotalSize)
                throw new InvalidDataException("The selected save exceeds the browser import limit.");
        }
    }

    public static MemoryStream WriteZip(SaveFolder save) => WriteZip(save.ExportFiles());

    public static MemoryStream WriteZip(IEnumerable<KeyValuePair<string, byte[]>> files)
    {
        var output = new MemoryStream();
        using (var archive = new ZipArchive(output, ZipArchiveMode.Create, true))
        {
            foreach (var (name, data) in files)
            {
                var entry = archive.CreateEntry(name.Replace('\\', '/'), CompressionLevel.NoCompression);
                using var stream = entry.Open();
                stream.Write(data);
            }
        }
        output.Position = 0;
        return output;
    }
}
