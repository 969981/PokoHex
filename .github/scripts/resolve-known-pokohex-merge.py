#!/usr/bin/env python3
"""Resolve one verified upstream PokoHex WinForms conflict without losing Files tab.

This resolver is intentionally pinned to the known Git index stage-2/stage-3 blob
SHAs from the November 2026 divergence. It refuses all unknown future conflicts.
Git's real two-parent merge is preserved, so subsequent upstream polls stay clean.
"""
from pathlib import Path
import subprocess
import sys

FILES = {
    # Exact reviewed 2026-11-16 conflicts: retain the fork's read-only Files
    # inventory and flat G00 import while adopting upstream Photo Ops/Catalogue.
    "PokoHex.Core/Save/SaveFolder.cs": (
        "b545962bccbe89f625f5f0dbb8124465d19bbfef",
        "c4df6524bab27b1aeaef1d1552610ed2f23e13c0",
    ),
    "PokoHex/Main.Designer.cs": (
        "c2f90973b845ba0fdd0b2d35761dadcb56a10f5c",
        "f1415c9a0ef51209684adbbca3b6b9b48eb91611",
    ),
    "PokoHex.Web/Main.razor.cs": (
        "732d0759953c2c434c67238bb4304bfc018be589",
        "21b2b98cebe63188805ea91ece97f8992cbcc41d",
    ),
}


def git(*args: str) -> str:
    return subprocess.check_output(["git", *args], text=True).strip()


def splice(s: str, old: str, new: str) -> str:
    if s.count(old) != 1:
        raise RuntimeError(f"Expected exactly one anchor: {old!r}")
    return s.replace(old, new, 1)


def load_stage(path: str, stage: int) -> str:
    raw = subprocess.check_output(["git", "show", f":{stage}:{path}"])
    if path.startswith("PokoHex/") and not raw.startswith(b"\xef\xbb\xbf"):
        raise RuntimeError(f"Unexpected WinForms encoding/BOM for {path}, stage {stage}")
    # Upstream Web files use plain UTF-8; WinForms files retain UTF-8 BOM.
    return raw.decode("utf-8-sig")


def resolve_main(up: str) -> str:
    up = splice(up, "            Editor_Map.LoadSave(save);\n",
                "            Editor_Map.LoadSave(save);\n            LoadSaveParts();\n")
    up = splice(up, "            Editor_Map.LoadSave(SAV);\n",
                "            Editor_Map.LoadSave(SAV);\n            LoadSaveParts();\n")
    up = splice(up, "    private void UpdateStatus()\n",
                "    private void LoadSaveParts()\n    {\n        DGV_SaveParts.Rows.Clear();\n        if (SAV == null)\n            return;\n        foreach (var component in SAV.SaveParts)\n            DGV_SaveParts.Rows.Add(component.RelativePath, component.Kind,\n                component.SizeBytes.ToString(\"N0\"), component.EditSupport, component.OriginalSha256);\n    }\n\n" + "    private void UpdateStatus()\n")
    for anchor in ("Editor_Skills.SaveChanges();", "Editor_Reactions.SaveChanges();",
                   "LoadSaveParts();", "private void LoadSaveParts()"):
        if anchor not in up:
            raise RuntimeError(f"Missing merged Main feature: {anchor}")
    return up


def resolve_savefolder(ours: str, upstream: str) -> str:
    # Keep the audited inventory, SHA-256 and flat/nested G00 input support.
    # Add upstream's lazily loaded Photo Ops and verified screenshot export.
    ours = splice(ours,
        "    private readonly Dictionary<int, StageSave> Stages = [];\n",
        "    private readonly Dictionary<int, StageSave> Stages = [];\n"
        "    private PhotoOps? Photos;\n")
    ours = splice(ours,
        "    public bool IsEdited => Common.IsEdited || Stages.Values.Any(z => z.IsEdited);",
        "    public bool IsEdited => Common.IsEdited || Stages.Values.Any(z => z.IsEdited) || Photos?.Screenshots.IsEdited == true;")
    first = upstream.index("    public PhotoOps GetPhotoOps()\n")
    last = upstream.index("    public static SaveFolder Read(string path)\n", first)
    photo_method = upstream[first:last]
    ours = splice(ours, "    public static SaveFolder Read(string path)\n",
                  photo_method + "    public static SaveFolder Read(string path)\n")
    first = upstream.index("        if (Photos?.Screenshots.IsEdited == true)\n")
    last = upstream.index("        foreach (var (area, stage) in Stages.Where", first)
    photo_write = upstream[first:last]
    ours = splice(ours, "        foreach (var (area, stage) in Stages.Where",
                  photo_write + "        foreach (var (area, stage) in Stages.Where")
    ours = splice(ours, '"SCSHDAT (unverified)"', '"Photo Ops"')
    ours = splice(ours, '"Preserved unchanged; fields not verified"', '"Verified screenshot editing"')
    ours = splice(ours, "isCommon || isStage);", "isCommon || isStage || isPhoto);")
    for anchor in ("public SavePartInfo[] SaveParts", "public PhotoOps GetPhotoOps()",
                   "Photos.Screenshots.Write()", "if (flatG00 && nestedG00)"):
        if anchor not in ours:
            raise RuntimeError(f"Missing merged SaveFolder feature: {anchor}")
    return ours


def resolve_designer(up: str) -> str:
    up = splice(up, '        var flags = new TabPage("Flags");\n',
                '        var flags = new TabPage("Flags");\n'
                '        var files = new TabPage("Files");\n'
                '        DGV_SaveParts = new DataGridView();\n')
    up = splice(up, "        TC_Editor.TabPages.AddRange([player, items, villagers, map, flags, photos, catalogue]);\n",
                "        TC_Editor.TabPages.AddRange([player, items, villagers, map, flags, photos, catalogue, files]);\n"
                + "        DGV_SaveParts.Dock = DockStyle.Fill;\n        DGV_SaveParts.ReadOnly = true;\n        DGV_SaveParts.AllowUserToAddRows = false;\n        DGV_SaveParts.AllowUserToDeleteRows = false;\n        DGV_SaveParts.AllowUserToResizeRows = false;\n        DGV_SaveParts.RowHeadersVisible = false;\n        DGV_SaveParts.SelectionMode = DataGridViewSelectionMode.FullRowSelect;\n        DGV_SaveParts.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.DisplayedCells;\n        DGV_SaveParts.BackgroundColor = SystemColors.Window;\n        DGV_SaveParts.Columns.Add(\"Path\", \"Path\");\n        DGV_SaveParts.Columns.Add(\"Kind\", \"Type\");\n        DGV_SaveParts.Columns.Add(\"Bytes\", \"Bytes\");\n        DGV_SaveParts.Columns.Add(\"Support\", \"Edit support\");\n        DGV_SaveParts.Columns.Add(\"Hash\", \"Original SHA-256\");\n        files.Controls.Add(DGV_SaveParts);\n")
    up = splice(up, "    private DataGridView DGV_Items = null!;\n",
                "    private DataGridView DGV_Items = null!;\n"
                "    private DataGridView DGV_SaveParts = null!;\n")
    for anchor in ("Editor_Flags = new FlagEditor", "flags.Controls.Add(Editor_Flags)",
                   "Editor_Skills = new AbilityEditor", "Editor_Reactions = new AbilityEditor",
                   "Editor_Photos = new PhotoOpEditor", "Editor_Catalogue = new CatalogueEditor",
                   "DGV_SaveParts.ReadOnly = true", "files.Controls.Add(DGV_SaveParts)"):
        if anchor not in up:
            raise RuntimeError(f"Missing merged Designer feature: {anchor}")
    return up


def resolve_web(up: str) -> str:
    up = splice(up,
                '    private static readonly string[] Tabs = ["Player", "Items", "Villagers", "Map", "Flags", "Photo Ops", "Catalogue"];\n',
                '    private static readonly string[] Tabs = ["Player", "Items", "Villagers", "Map", "Flags", "Photo Ops", "Catalogue", "Files"];\n')
    for anchor in ('private FlagEditor? Editor_Flags;', 'Editor_Flags?.SaveChanges();',
                   '"Flags", "Photo Ops", "Catalogue", "Files"', 'PhotosOpened'):
        if anchor not in up:
            raise RuntimeError(f"Missing merged web feature: {anchor}")
    return up


def main() -> None:
    unresolved = set(subprocess.check_output(
        ["git", "diff", "--name-only", "--diff-filter=U", "-z"]
    ).decode().strip("\0").split("\0"))
    if unresolved != set(FILES):
        raise RuntimeError(f"Refusing unknown conflicts: {sorted(unresolved)}")
    for path, (ours_expected, theirs_expected) in FILES.items():
        ours_sha = git("rev-parse", f":2:{path}")
        theirs_sha = git("rev-parse", f":3:{path}")
        if (ours_sha, theirs_sha) != (ours_expected, theirs_expected):
            raise RuntimeError(
                f"Refusing unreviewed {path} versions: {ours_sha} / {theirs_sha}"
            )

    for path in FILES:
        upstream = load_stage(path, 3)
        if path == "PokoHex.Core/Save/SaveFolder.cs":
            merged = resolve_savefolder(load_stage(path, 2), upstream)
        elif path == "PokoHex/Main.Designer.cs":
            merged = resolve_designer(upstream)
        elif path == "PokoHex.Web/Main.razor.cs":
            merged = resolve_web(upstream)
        else:
            raise RuntimeError(f"Unexpected merge file: {path}")
        if any(marker in merged for marker in ("<<<<<<<", ">>>>>>>")):
            raise RuntimeError(f"Conflict marker in {path}")
        prefix = b"\xef\xbb\xbf" if path.startswith(("PokoHex/", "PokoHex.Core/")) else b""
        Path(path).write_bytes(prefix + merged.encode("utf-8"))
        print(f"Resolved {path}: local Files inspector + upstream Photo Ops/Catalogue")
    print("Verified known-only conflict resolution. Git staging and commit happen in the workflow.")


if __name__ == "__main__":
    try:
        main()
    except (RuntimeError, subprocess.CalledProcessError) as exc:
        print(f"::error::{exc}", file=sys.stderr)
        sys.exit(1)
