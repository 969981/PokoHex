using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;

namespace PokoHex.Core;

public sealed class SaveFolder
{
    public string Folder { get; }
    public SaveFile Common { get; }
    private readonly Dictionary<string, byte[]> Files;
    private readonly Dictionary<int, StageSave> Stages = [];
    public int[] StageIds { get; }
    public SavePartInfo[] SaveParts { get; }
    public bool IsEdited => Common.IsEdited || Stages.Values.Any(z => z.IsEdited);

    private SaveFolder(string folder, Dictionary<string, byte[]> files)
    {
        Folder = folder;
        Files = files;
        Common = SaveFile.Read(files[Path.Combine("G00", "CMNDAT.BIN")]);
        // Inventory all components, including opaque/photo candidates, without modifying their bytes.
        SaveParts = files.Select(entry =>
        {
            var relative = entry.Key.Replace('\\', '/');
            var inCommonFolder = relative.StartsWith("G00/", StringComparison.OrdinalIgnoreCase);
            var name = Path.GetFileName(relative);
            var isCommon = inCommonFolder && name.Equals("CMNDAT.BIN", StringComparison.OrdinalIgnoreCase);
            var isPhoto = inCommonFolder && name.Equals("SCSHDAT.BIN", StringComparison.OrdinalIgnoreCase);
            var isStage = inCommonFolder && name.Length == 12 && name.StartsWith("STGDAT", StringComparison.OrdinalIgnoreCase)
                && name.EndsWith(".BIN", StringComparison.OrdinalIgnoreCase) && int.TryParse(name.AsSpan(6, 2), out _);
            var kind = isCommon ? "Common" : isPhoto ? "SCSHDAT (unverified)" : isStage ? "Stage" : "Other";
            var support = isCommon ? "Player / inventory / villagers"
                : isStage ? "Limited map editing"
                : isPhoto ? "Preserved unchanged; fields not verified"
                : "Preserved unchanged";
            return new SavePartInfo(relative, kind, support, entry.Value.LongLength,
                Convert.ToHexString(SHA256.HashData(entry.Value)), isCommon || isStage);
        }).OrderBy(x => x.RelativePath, StringComparer.OrdinalIgnoreCase).ToArray();
        StageIds = files.Keys.Where(z => Path.GetDirectoryName(z)!.Equals("G00", StringComparison.OrdinalIgnoreCase))
            .Select(Path.GetFileName).Where(z => z!.Length == 12 && z.StartsWith("STGDAT", StringComparison.OrdinalIgnoreCase) &&
                z.EndsWith(".BIN", StringComparison.OrdinalIgnoreCase) && int.TryParse(z.AsSpan(6, 2), out _))
            .Select(z => int.Parse(z!.AsSpan(6, 2))).Order().ToArray();
    }

    public StageSave GetStage(int area)
    {
        if (Stages.TryGetValue(area, out var stage))
            return stage;
        if (!StageIds.Contains(area))
            throw new ArgumentException("The selected area is not present in this save.");
        stage = StageSave.Read(Files[Path.Combine("G00", $"STGDAT{area:D2}.BIN")]);
        Stages.Add(area, stage);
        return stage;
    }

    public static SaveFolder Read(string path)
    {
        path = Path.GetFullPath(path);
        if (File.Exists(path))
            path = Path.GetDirectoryName(path)!;
        if (Path.GetFileName(path).Equals("G00", StringComparison.OrdinalIgnoreCase))
            path = Directory.GetParent(path)!.FullName;

        // Accept either the full save root (G00, optionally S00) or a loose
        // folder containing the common/stage/photo BIN files directly.
        var nested = File.Exists(Path.Combine(path, "G00", "CMNDAT.BIN"));
        var flat = !nested && File.Exists(Path.Combine(path, "CMNDAT.BIN"));
        if (!nested && !flat)
            throw new InvalidDataException("Select a save folder with G00/CMNDAT.BIN or a folder containing CMNDAT.BIN.");

        var files = new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase);
        foreach (var file in Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories))
        {
            if ((File.GetAttributes(file) & FileAttributes.ReparsePoint) != 0)
                throw new IOException("Linked save files are not supported.");
            var relative = Path.GetRelativePath(path, file);
            if (flat)
                relative = Path.Combine("G00", relative);
            files.Add(relative, File.ReadAllBytes(file));
        }
        return new SaveFolder(path, files);
    }

    public static SaveFolder ReadFiles(IEnumerable<KeyValuePair<string, byte[]>> source)
    {
        var entries = source.ToArray();
        var flatG00 = entries.Any(z => z.Key.Replace('\\', '/').Equals("CMNDAT.BIN", StringComparison.OrdinalIgnoreCase));
        var nestedG00 = entries.Any(z => z.Key.Replace('\\', '/').Equals("G00/CMNDAT.BIN", StringComparison.OrdinalIgnoreCase));
        if (flatG00 && nestedG00)
            throw new InvalidDataException("The save has two Common files (flat and G00).");
        var files = new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase);
        foreach (var (name, data) in entries)
        {
            var parts = name.Replace('\\', '/').Split('/');
            if (parts.Any(z => z.Length == 0 || z is "." or ".." || z.Contains(':') || z.Any(char.IsControl)))
                throw new InvalidDataException("The save contains an invalid file path.");
            // A direct G00-only export may have CMNDAT.BIN at the archive root.
            var relative = flatG00 && parts.Length == 1 ? Path.Combine("G00", parts[0]) : Path.Combine(parts);
            if (!files.TryAdd(relative, (byte[])data.Clone()))
                throw new InvalidDataException("The save contains duplicate file paths.");
        }
        if (!files.ContainsKey(Path.Combine("G00", "CMNDAT.BIN")))
            throw new InvalidDataException("Select a save containing CMNDAT.BIN (under G00 or directly in a G00-only folder).");
        return new SaveFolder(string.Empty, files);
    }

    public void Export(string destination)
    {
        destination = Path.GetFullPath(destination).TrimEnd(Path.DirectorySeparatorChar);
        var source = Folder.TrimEnd(Path.DirectorySeparatorChar);
        if (destination.Equals(source, StringComparison.OrdinalIgnoreCase) ||
            destination.StartsWith(source + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            throw new IOException("Choose a new folder outside the opened save.");
        if (File.Exists(destination) || (Directory.Exists(destination) && Directory.EnumerateFileSystemEntries(destination).Any()))
            throw new IOException("The export folder must be empty.");

        var files = ExportFiles();
        Directory.CreateDirectory(destination);
        foreach (var (relative, data) in files)
        {
            var target = Path.Combine(destination, relative);
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            File.WriteAllBytes(target, data);
        }
    }

    public Dictionary<string, byte[]> ExportFiles()
    {
        var common = Common.Write();
        var check = SaveFile.Read(common);
        if (!check.Data.AsSpan().SequenceEqual(Common.Data))
            throw new InvalidDataException("The edited save could not be verified.");

        var edited = new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase)
        {
            [Path.Combine("G00", "CMNDAT.BIN")] = common,
        };
        foreach (var (area, stage) in Stages.Where(z => z.Value.IsEdited))
        {
            var data = stage.Write();
            var verified = StageSave.Read(data);
            if (!verified.Data.Span.SequenceEqual(stage.Data.Span))
                throw new InvalidDataException("The edited map could not be verified.");
            edited.Add(Path.Combine("G00", $"STGDAT{area:D2}.BIN"), data);
        }

        var result = new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase);
        foreach (var (relative, original) in Files)
            result.Add(relative, edited.TryGetValue(relative, out var data) ? data : (byte[])original.Clone());
        return result;
    }
}
