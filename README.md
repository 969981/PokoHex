PokoHex
=====

![License](https://img.shields.io/badge/License-GPLv3-blue.svg)

Proof of Concept Save Editor for Pokopia (Poko a Pokemon)

Edits savedata dumped from the Nintendo Switch 2.

* Bring your own save file data; this program does not dump it from your console.

## Builds

**Windows**: Download `PokoHex-win-x64.zip` from the [Releases](https://github.com/berichan/PokoHex/releases), extract it, and run `PokoHex.exe`.

**All Platforms/Browser**: Use this repository's GitHub Pages, click here -> [PokoHex.Web](https://berichan.github.io/PokoHex/)

## Pokopia save files (this fork)

This fork recognizes `CMNDAT.BIN`, `STGDATxx.BIN`, and the unverified `SCSHDAT.BIN` in the **Files** tab (Windows and Web). It can open a full save with `G00` / `S00` or a G00-only set of BIN files, and exports a copy with all unknown files unchanged. **G00-only output is not guaranteed to be a complete restorable save.** SCSHDAT is preserved, not edited.

See [save component format and safety notes](docs/pokopia-save-components.md).

## Other

Based on the style of [NHSE](https://github.com/kwsch/NHSE)

**We do not support or condone cheating at the expense of others.** Please be responsible!

> [!WARNING]
> Always keep save backups of your save from your console and edits before editing or restoring any savefiles!
