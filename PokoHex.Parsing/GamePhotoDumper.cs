using System;
using System.IO;
using System.Text;
using PokoHex.Core;
using static System.Buffers.Binary.BinaryPrimitives;

namespace PokoHex.Parsing;

public static class GamePhotoDumper
{
    public static string[] GetPhotoOpList(string path, string language = GameLanguage.DefaultLanguage) => ReadNames(path, language, 5059, 101, PhotoHistory.SIZE);
    public static string[] GetFrameList(string path, string language = GameLanguage.DefaultLanguage) => ReadNames(path, language, 4870, PhotoStyleHistory.FrameCount, PhotoStyleHistory.FrameCount);
    public static string[] GetFilterList(string path, string language = GameLanguage.DefaultLanguage) => ReadNames(path, language, 4141, PhotoStyleHistory.FilterCount, PhotoStyleHistory.FilterCount);

    private static string[] ReadNames(string path, string language, int first, int count, int capacity)
    {
        var shift = language switch
        {
            "jp" => 0, "en" => 1, "fr" => 2, "it" => 3, "de" => 4,
            "es" => 5, "zht" => 6, "ko" => 7, "zhs" => 8,
            _ => throw new ArgumentException("This language is not supported.", nameof(language)),
        };
        using var archive = new GameArchive(GameItemDumper.GetArchiveFolder(path));
        if (archive.EntryCount != 12298)
            throw new InvalidDataException("The photo text version is not supported.");
        var data = archive.Read(first + shift);
        if (data.Length < 64 + count * 8 || ReadInt32LittleEndian(data) != count || ReadInt32LittleEndian(data.AsSpan(4)) != 2)
            throw new InvalidDataException("The photo text table is not supported.");
        var result = new string[capacity];
        Array.Fill(result, string.Empty);
        for (int i = 0; i < count; i++)
        {
            var field = 64 + i * 8;
            var offset = (long)field + ReadUInt32LittleEndian(data.AsSpan(field));
            if (offset < 64 + count * 8 || offset >= data.Length)
                throw new InvalidDataException("The photo name offset is invalid.");
            var text = data.AsSpan((int)offset);
            var end = text.IndexOf((byte)0);
            if (end < 0)
                throw new InvalidDataException("The photo name is not terminated.");
            result[i] = new UTF8Encoding(false, true).GetString(text[..end]);
        }
        return result;
    }

    public static void UpdateDumps(string path, string destination)
    {
        for (int i = 0; i < GameLanguage.LanguageCount; i++)
        {
            var language = GameLanguage.Language2Char(i);
            var folder = Path.Combine(destination, "text", language);
            Directory.CreateDirectory(folder);
            File.WriteAllLines(Path.Combine(folder, $"text_photoop_{language}.txt"), GetPhotoOpList(path, language), new UTF8Encoding(false));
            File.WriteAllLines(Path.Combine(folder, $"text_photoframe_{language}.txt"), GetFrameList(path, language), new UTF8Encoding(false));
            File.WriteAllLines(Path.Combine(folder, $"text_photofilter_{language}.txt"), GetFilterList(path, language), new UTF8Encoding(false));
        }
    }
}
