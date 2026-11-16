using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using PokoHex.Core;

namespace PokoHex;

public partial class PhotoOpEditor : UserControl
{
    private SaveFolder? SAV;
    private PhotoOps? Photos;
    private bool Loading;
    private bool Opened;
    private int SelectedOp = -1;
    private int SelectedPhoto = -1;
    private PhotoEntry[] Captures = [];
    private string LoadError = string.Empty;

    public PhotoOpEditor() => InitializeComponent();

    public void LoadSave(SaveFolder save)
    {
        SAV = save;
        Photos = null;
        Opened = false;
        SelectedOp = -1;
        SelectedPhoto = -1;
        LoadError = string.Empty;
        TB_Search.Text = string.Empty;
        LoadEntries();
        if (CB_View.SelectedIndex > 0)
            Editor_Styles.LoadSave(save.Common, CB_View.SelectedIndex == 2);
        if (Visible)
            RefreshValues();
    }

    public void RefreshValues()
    {
        if (!Opened && SAV != null)
        {
            Opened = true;
            try { Photos = SAV.GetPhotoOps(); }
            catch (Exception ex) when (ex is IOException or InvalidDataException or ArgumentException)
            {
                LoadError = ex.Message;
            }
        }
        LoadEntries();
    }

    public void RefreshLanguage()
    {
        LoadEntries();
        Editor_Styles.RefreshLanguage();
    }

    private void LoadEntries()
    {
        Loading = true;
        DGV_Photos.Rows.Clear();
        TB_Search.Enabled = Photos != null;
        if (Photos != null)
        {
            var search = TB_Search.Text.Trim();
            foreach (var item in GameInfo.Strings.PhotoOpDataSource)
            {
                if (!item.Text.Contains(search, StringComparison.CurrentCultureIgnoreCase) && !item.Value.ToString().Contains(search))
                    continue;
                var row = DGV_Photos.Rows[DGV_Photos.Rows.Add(item.Text, Photos.IsCompleted(item.Value) ? "Yes" : "No", Photos.GetPhotos(item.Value).Length)];
                row.Tag = (int)item.Value;
                if (item.Value == SelectedOp)
                    DGV_Photos.CurrentCell = row.Cells[0];
            }
        }
        SelectedOp = DGV_Photos.CurrentRow?.Tag is int index ? index : -1;
        L_Status.Text = LoadError;
        B_Undo.Enabled = Photos?.CanUndo == true;
        B_Redo.Enabled = Photos?.CanRedo == true;
        Loading = false;
        LoadPhoto();
    }

    private void LoadPhoto()
    {
        Loading = true;
        Captures = Photos == null || SelectedOp < 1 ? [] : Photos.GetPhotos(SelectedOp).OrderBy(z => z.Kind == 4 ? 0 : 1).ThenBy(z => z.Index).ToArray();
        if (!Captures.Any(z => z.Index == SelectedPhoto))
            SelectedPhoto = Captures.FirstOrDefault(z => !Photos!.Screenshots.GetImage(z).IsEmpty)?.Index ?? Captures.FirstOrDefault()?.Index ?? -1;
        CB_Photo.DataSource = Captures.Select((z, i) => new ItemInfo((ushort)z.Index, $"Photo {i + 1}")).ToArray();
        CB_Photo.SelectedValue = (ushort)Math.Max(0, SelectedPhoto);
        CB_Photo.Enabled = Captures.Length > 1;
        Loading = false;
        LoadPreview();
    }

    private ReadOnlyMemory<byte> SelectedImage => Photos == null || Captures.FirstOrDefault(z => z.Index == SelectedPhoto) is not { } entry
        ? ReadOnlyMemory<byte>.Empty : Photos.Screenshots.GetImage(entry);

    private void LoadPreview()
    {
        ClearPreview();
        GB_Photo.Text = SelectedOp > 0 ? PhotoOpInfo.GetName(SelectedOp) : "Photo Op";
        B_Reset.Enabled = Photos != null && SelectedOp > 0 && Photos.CanReset(SelectedOp);
        B_ExportPhoto.Enabled = !SelectedImage.IsEmpty;
        L_Preview.Text = string.Empty;
        if (Photos == null || SelectedOp < 1)
            return;
        L_Status.Text = Photos.GetResetError(SelectedOp) ?? string.Empty;
        var data = SelectedImage;
        if (!data.IsEmpty)
        {
            try
            {
                using var stream = new MemoryStream(data.ToArray(), false);
                using var image = Image.FromStream(stream);
                PB_Photo.Image = new Bitmap(image);
                return;
            }
            catch (Exception ex) when (ex is ArgumentException or ExternalException or OutOfMemoryException) { }
        }
        PB_Photo.Image = new Bitmap(PhotoSprite.GetPlaceholder(Captures.Length != 0)!);
        L_Preview.Text = Photos.GetPhotos(SelectedOp).Length == 0 ? "No photo" : "Preview unavailable";
    }

    private void ClearPreview()
    {
        var image = PB_Photo.Image;
        PB_Photo.Image = null;
        image?.Dispose();
    }

    private void DGV_Photos_CurrentCellChanged(object sender, EventArgs e)
    {
        if (Loading)
            return;
        SelectedOp = DGV_Photos.CurrentRow?.Tag is int index ? index : -1;
        LoadPhoto();
    }

    private void TB_Search_TextChanged(object sender, EventArgs e)
    {
        if (!Loading)
            LoadEntries();
    }

    private void B_Reset_Click(object sender, EventArgs e)
    {
        if (Photos == null || SelectedOp < 1)
            return;
        try { Photos.Reset(SelectedOp); }
        catch (ArgumentException ex) { L_Status.Text = ex.Message; return; }
        LoadEntries();
    }

    private void B_Undo_Click(object sender, EventArgs e)
    {
        Photos?.UndoEdit();
        LoadEntries();
    }

    private void B_Redo_Click(object sender, EventArgs e)
    {
        Photos?.RedoEdit();
        LoadEntries();
    }

    private void CB_Photo_SelectedIndexChanged(object sender, EventArgs e)
    {
        if (Loading)
            return;
        SelectedPhoto = (CB_Photo.SelectedItem as ItemInfo)?.Value ?? -1;
        LoadPreview();
    }

    private void CB_View_SelectedIndexChanged(object sender, EventArgs e)
    {
        var gallery = CB_View.SelectedIndex > 0;
        P_Photos.Visible = !gallery;
        Editor_Styles.Visible = gallery;
        L_Status.Visible = !gallery;
        if (SAV == null)
            return;
        if (gallery)
        {
            ClearPreview();
            Editor_Styles.LoadSave(SAV.Common, CB_View.SelectedIndex == 2);
        }
        else
            RefreshValues();
    }

    private void B_ExportPhoto_Click(object sender, EventArgs e)
    {
        if (SelectedImage.IsEmpty)
            return;
        using var dialog = new SaveFileDialog
        {
            Filter = "JPEG image (*.jpg)|*.jpg", DefaultExt = "jpg", AddExtension = true,
            FileName = $"PhotoOp-{SelectedOp}-{Array.FindIndex(Captures, z => z.Index == SelectedPhoto) + 1}.jpg",
        };
        if (dialog.ShowDialog(this) != DialogResult.OK)
            return;
        try { ExportPhoto(dialog.FileName); }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException)
        {
            MessageBox.Show(this, ex.Message, "PokoHex", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void ExportPhoto(string path)
    {
        if (SelectedImage.IsEmpty)
            throw new ArgumentException("Select a photo.");
        path = Path.GetFullPath(path);
        if (!string.IsNullOrEmpty(SAV?.Folder) && path.StartsWith(SAV.Folder.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            throw new IOException("Choose a location outside the opened save folder.");
        File.WriteAllBytes(path, SelectedImage.ToArray());
    }
}
