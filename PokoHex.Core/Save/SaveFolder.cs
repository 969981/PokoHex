using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace PokoHex.Core;

public sealed class SaveFolder
{
    public string Folder { get; }
    public SaveFile Common { get; }
    private readonly Dictionary<string, byte[]> Files;
    private readonly Dictionary<int, StageSave> Stages = [];
    private PhotoOps? Photos;
    public int[] StageIds { get; }
    public bool IsEdited => Common.IsEdited || Stages.Values.Any(z => z.IsEdited) || Photos?.Screenshots.IsEdited == true;

    private SaveFolder(string folder, Dictionary<string, byte[]> files)
    {
        Folder = folder;
        Files = files;
        Common = SaveFile.Read(files[Path.Combine("G00", "CMNDAT.BIN")]);
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

    public PhotoOps GetPhotoOps()
    {
        if (Photos != null)
            return Photos;
        if (Common.Photos == null)
            throw new InvalidDataException("Photo Ops aren’t available for this save version.");
        if (!Files.TryGetValue(Path.Combine("G00", "SCSHDAT.BIN"), out var data))
            throw new InvalidDataException("The photo file is missing.");
        Photos = new PhotoOps(Common.Photos, ScreenshotSave.Read(data));
        return Photos;
    }

    public static SaveFolder Read(string path)
    {
        path = Path.GetFullPath(path);
        if (File.Exists(path))
            path = Directory.GetParent(Path.GetDirectoryName(path)!)!.FullName;
        if (Path.GetFileName(path).Equals("G00", StringComparison.OrdinalIgnoreCase))
            path = Directory.GetParent(path)!.FullName;
        if (!File.Exists(Path.Combine(path, "G00", "CMNDAT.BIN")) || !Directory.Exists(Path.Combine(path, "S00")))
            throw new InvalidDataException("Select the save folder containing G00 and S00.");

        var files = new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase);
        foreach (var file in Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories))
        {
            if ((File.GetAttributes(file) & FileAttributes.ReparsePoint) != 0)
                throw new IOException("Linked save files are not supported.");
            files.Add(Path.GetRelativePath(path, file), File.ReadAllBytes(file));
        }
        return new SaveFolder(path, files);
    }

    public static SaveFolder ReadFiles(IEnumerable<KeyValuePair<string, byte[]>> source)
    {
        var files = new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase);
        foreach (var (name, data) in source)
        {
            var parts = name.Replace('\\', '/').Split('/');
            if (parts.Any(z => z.Length == 0 || z is "." or ".." || z.Contains(':') || z.Any(char.IsControl)))
                throw new InvalidDataException("The save contains an invalid file path.");
            if (!files.TryAdd(Path.Combine(parts), (byte[])data.Clone()))
                throw new InvalidDataException("The save contains duplicate file paths.");
        }
        if (!files.ContainsKey(Path.Combine("G00", "CMNDAT.BIN")) ||
            !files.Keys.Any(z => z.StartsWith("S00" + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)))
            throw new InvalidDataException("Select the save folder containing G00 and S00.");
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
        if (Photos?.Screenshots.IsEdited == true)
        {
            var data = Photos.Screenshots.Write();
            var verified = ScreenshotSave.Read(data);
            if (!verified.Data.Span.SequenceEqual(Photos.Screenshots.Data.Span))
                throw new InvalidDataException("The edited photos could not be verified.");
            edited.Add(Path.Combine("G00", "SCSHDAT.BIN"), data);
        }
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
