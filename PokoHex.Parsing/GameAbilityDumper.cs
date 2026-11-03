using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using PokoHex.Core;
using static System.Buffers.Binary.BinaryPrimitives;

namespace PokoHex.Parsing;

public static class GameAbilityDumper
{
    private static readonly IReadOnlyDictionary<string, int> Languages = new Dictionary<string, int>
    {
        {"jp", 0}, {"en", 1}, {"fr", 2}, {"it", 3}, {"de", 4},
        {"es", 5}, {"zht", 6}, {"ko", 7}, {"zhs", 8},
    };

    public static string[] GetSkillList(string path, string language = GameLanguage.DefaultLanguage) => ReadNames(path, language, 4942, 61, PlayerAbilities.SkillCount);
    public static string[] GetReactionList(string path, string language = GameLanguage.DefaultLanguage) => ReadNames(path, language, 2332, 128, PlayerAbilities.ReactionCount);

    private static string[] ReadNames(string path, string language, int first, int count, int capacity)
    {
        if (!Languages.TryGetValue(language, out var shift))
            throw new ArgumentException("This language is not supported.", nameof(language));
        using var archive = new GameArchive(GameItemDumper.GetArchiveFolder(path));
        if (archive.EntryCount != 12298)
            throw new InvalidDataException("The player ability tables are not supported for this game version.");
        var data = archive.Read(first + shift);
        if (data.Length < 64 + count * 8 || ReadInt32LittleEndian(data) != count || ReadInt32LittleEndian(data.AsSpan(4)) != 2)
            throw new InvalidDataException("The player ability text table is not supported.");
        var result = new string[capacity];
        Array.Fill(result, string.Empty);
        for (int i = 0; i < count; i++)
        {
            var field = 64 + i * 8;
            var offset = (long)field + ReadUInt32LittleEndian(data.AsSpan(field));
            if (offset < 64 + count * 8 || offset >= data.Length)
                throw new InvalidDataException("The ability name offset is invalid.");
            var text = data.AsSpan((int)offset);
            var end = text.IndexOf((byte)0);
            if (end < 0)
                throw new InvalidDataException("The ability name is not terminated.");
            result[i] = new UTF8Encoding(false, true).GetString(text[..end]);
        }
        return result;
    }

    public static void UpdateDumps(string path, string destination)
    {
        foreach (var language in Languages.Keys)
        {
            var folder = Path.Combine(destination, "text", language);
            Directory.CreateDirectory(folder);
            File.WriteAllLines(Path.Combine(folder, $"text_skill_{language}.txt"), GetSkillList(path, language), new UTF8Encoding(false));
            File.WriteAllLines(Path.Combine(folder, $"text_reaction_{language}.txt"), GetReactionList(path, language), new UTF8Encoding(false));
        }
    }
}
