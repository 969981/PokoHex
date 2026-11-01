# Pokopia save components and safe editing

This fork recognizes the three Pokopia save filenames shown in G00:

| File | What is known | Editor behavior |
| --- | --- | --- |
| `CMNDAT.BIN` | Common/player data; PokoHex already parses player name, 130 inventory slots and (on supported versions) villagers | Editable in Player, Items and Villagers tabs |
| `STGDATxx.BIN` | Region/stage content; recognized by two-digit stage suffix and current StageSave parser | Limited supported map objects; unsupported versions fail closed |
| `SCSHDAT.BIN` | Opaque file. DQB2 uses the same filename for photo data; Pokopia's layout has **not** been independently established | Read-only inventory entry; byte-for-byte preserved during export |

## Supported import layouts

- Full save: `save/G00/CMNDAT.BIN`, optionally `save/G00/SCSHDAT.BIN`, one or more `save/G00/STGDATxx.BIN`, and `save/S00/**` when available.
- G00-only: `G00/CMNDAT.BIN` with other loose `*.BIN` files, or a directory containing `CMNDAT.BIN` directly.
- ZIP: either full layout or a G00-only set, optionally wrapped inside a parent folder.

The Windows client exports a **new folder**. The Web client exports a ZIP. The output always normalizes a loose G00-only input to a `G00/` directory. Unrecognized files are included unchanged and the Files tab shows each *original* file's SHA-256.

**Important:** A G00-only import is a partial save; its output may be insufficient to restore on a console if the original save also had an `S00` directory or other files. Keep the full original backup. Do not treat SCSHDAT as known Pokopia photo data until actual binary samples and a single-variable in-game photo test confirm the format.

## Tests

`PokoHex.Tests/SaveFolderTests.cs` covers G00-only imports and verifies SCSHDAT byte-for-byte preservation after editing Common data. No Pokopia production save or photo samples were used in these tests.

## Further reading

- [Public PokoHex implementation](https://github.com/berichan/PokoHex) (GPLv3).
- [Pokopia 2.0.1 inventory / ELF crosswalk](https://github.com/969981/HOME-Live-Plugin-CHS/blob/home-core-editor/docs/research/pokopia/2.0.1/phase10-pokohex-save-elf-crosswalk.md).
- [DQB2 file naming comparison](https://github.com/turtle-insect/DQB2). Same filename does **not** establish identical Pokopia structure.
