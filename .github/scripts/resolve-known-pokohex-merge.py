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
    "PokoHex/Main.cs": (
        "91172dd3b1e0f13a9f9855d5cf5e318b45a9ecf9",
        "d234365796c97104b2bccf642faa71f856221454",
    ),
    "PokoHex/Main.Designer.cs": (
        "c432711433d73f9a90fb42577a45e2aadcff443d",
        "834a7e04e9411003c4e1b4dd75d6c691302b2e51",
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
    if not raw.startswith(b"\\xef\\xbb\\xbf"):
        raise RuntimeError(f"Unexpected encoding/BOM for {path}, stage {stage}")
    return raw.decode("utf-8-sig")


def resolve_main(up: str) -> str:
    up = splice(up, "            Editor_Map.LoadSave(save);\\n",
                "            Editor_Map.LoadSave(save);\\n            LoadSaveParts();\\n")
    up = splice(up, "            Editor_Map.LoadSave(SAV);\\n",
                "            Editor_Map.LoadSave(SAV);\\n            LoadSaveParts();\\n")
    up = splice(up, "    private void UpdateStatus()\\n",
                "    private void LoadSaveParts()\n    {\n        DGV_SaveParts.Rows.Clear();\n        if (SAV == null)\n            return;\n        foreach (var component in SAV.SaveParts)\n            DGV_SaveParts.Rows.Add(component.RelativePath, component.Kind,\n                component.SizeBytes.ToString(\"N0\"), component.EditSupport, component.OriginalSha256);\n    }\n\n" + "    private void UpdateStatus()\\n")
    for anchor in ("Editor_Skills.SaveChanges();", "Editor_Reactions.SaveChanges();",
                   "LoadSaveParts();", "private void LoadSaveParts()"):
        if anchor not in up:
            raise RuntimeError(f"Missing merged Main feature: {anchor}")
    return up


def resolve_designer(up: str) -> str:
    up = splice(up, '        var map = new TabPage("Map");\\n',
                '        var map = new TabPage("Map");\\n'
                '        var files = new TabPage("Files");\\n'
                '        DGV_SaveParts = new DataGridView();\\n')
    up = splice(up, "        TC_Editor.TabPages.AddRange([player, items, villagers, map]);\\n",
                "        TC_Editor.TabPages.AddRange([player, items, villagers, map, files]);\\n"
                + "        DGV_SaveParts.Dock = DockStyle.Fill;\n        DGV_SaveParts.ReadOnly = true;\n        DGV_SaveParts.AllowUserToAddRows = false;\n        DGV_SaveParts.AllowUserToDeleteRows = false;\n        DGV_SaveParts.AllowUserToResizeRows = false;\n        DGV_SaveParts.RowHeadersVisible = false;\n        DGV_SaveParts.SelectionMode = DataGridViewSelectionMode.FullRowSelect;\n        DGV_SaveParts.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.DisplayedCells;\n        DGV_SaveParts.BackgroundColor = SystemColors.Window;\n        DGV_SaveParts.Columns.Add(\"Path\", \"Path\");\n        DGV_SaveParts.Columns.Add(\"Kind\", \"Type\");\n        DGV_SaveParts.Columns.Add(\"Bytes\", \"Bytes\");\n        DGV_SaveParts.Columns.Add(\"Support\", \"Edit support\");\n        DGV_SaveParts.Columns.Add(\"Hash\", \"Original SHA-256\");\n        files.Controls.Add(DGV_SaveParts);\n")
    up = splice(up, "    private DataGridView DGV_Items = null!;\\n",
                "    private DataGridView DGV_Items = null!;\\n"
                "    private DataGridView DGV_SaveParts = null!;\\n")
    for anchor in ("Editor_Skills = new AbilityEditor", "Editor_Reactions = new AbilityEditor",
                   "DGV_SaveParts.ReadOnly = true", "files.Controls.Add(DGV_SaveParts)"):
        if anchor not in up:
            raise RuntimeError(f"Missing merged Designer feature: {anchor}")
    return up


def main() -> None:
    unresolved = set(subprocess.check_output(
        ["git", "diff", "--name-only", "--diff-filter=U", "-z"]
    ).decode().strip("\\0").split("\\0"))
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
        merged = resolve_main(upstream) if path.endswith("/Main.cs") else resolve_designer(upstream)
        if any(marker in merged for marker in ("<<<<<<<", ">>>>>>>")):
            raise RuntimeError(f"Conflict marker in {path}")
        Path(path).write_bytes(b"\\xef\\xbb\\xbf" + merged.encode("utf-8"))
        print(f"Resolved {path}: Files inspector + upstream Skills/Reactions")
    print("Verified known-only conflict resolution. Git staging and commit happen in the workflow.")


if __name__ == "__main__":
    try:
        main()
    except (RuntimeError, subprocess.CalledProcessError) as exc:
        print(f"::error::{exc}", file=sys.stderr)
        sys.exit(1)
