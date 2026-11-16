using System;
using System.Collections.Generic;
using System.Linq;

namespace PokoHex.Core;

public sealed class PhotoOps(PhotoHistory history, ScreenshotSave screenshots)
{
    private readonly Stack<Edit> Undo = [];
    private readonly Stack<Edit> Redo = [];
    private sealed record EntryChange(int Index, byte[] Before);
    private sealed record Edit(int Id, bool Completed, EntryChange[] Entries);
    private static readonly byte[] EmptyEntry = CreateEmpty();
    public ScreenshotSave Screenshots { get; } = screenshots;
    public bool CanUndo => Undo.Count != 0;
    public bool CanRedo => Redo.Count != 0;
    public bool IsCompleted(int id) => history.IsCompleted(id);
    public PhotoEntry[] GetPhotos(int id) => Screenshots.Entries.Where(z => !z.IsEmpty && z.OpId == id).ToArray();

    public ReadOnlyMemory<byte> GetPreview(int id)
    {
        foreach (var entry in GetPhotos(id).OrderBy(z => z.Kind == 4 ? 0 : 1))
        {
            var data = Screenshots.GetImage(entry);
            if (!data.IsEmpty)
                return data;
        }
        return ReadOnlyMemory<byte>.Empty;
    }

    public string? GetResetError(int id)
    {
        if (!PhotoOpInfo.IsPublic(id))
            return "Select a Photo Op.";
        return GetPhotos(id).Any(z => !z.CanReset) ? "This Photo Op has unsupported photo data." : null;
    }

    public bool CanReset(int id) => GetResetError(id) == null && (IsCompleted(id) || GetPhotos(id).Length != 0);

    public void Reset(int id)
    {
        var error = GetResetError(id);
        if (error != null)
            throw new ArgumentException(error);
        var entries = GetPhotos(id);
        var completed = IsCompleted(id);
        if (!completed && entries.Length == 0)
            return;
        // Gather every record before changing either save file.
        var edit = new Edit(id, completed, entries.Select(z => new EntryChange(z.Index, z.Write())).ToArray());
        Apply(edit, false);
        Undo.Push(edit);
        Redo.Clear();
    }

    private void Apply(Edit edit, bool restore)
    {
        foreach (var entry in edit.Entries)
            Screenshots.SetEntry(entry.Index, restore ? entry.Before : EmptyEntry);
        history.SetCompleted(edit.Id, restore && edit.Completed);
    }

    public void UndoEdit()
    {
        if (!Undo.TryPop(out var edit))
            return;
        Apply(edit, true);
        Redo.Push(edit);
    }

    public void RedoEdit()
    {
        if (!Redo.TryPop(out var edit))
            return;
        Apply(edit, false);
        Undo.Push(edit);
    }

    private static byte[] CreateEmpty()
    {
        var data = new byte[PhotoEntry.SIZE];
        data.AsSpan(0, 8).Fill(0xFF);
        return data;
    }
}
