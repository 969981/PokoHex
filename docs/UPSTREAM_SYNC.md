# Upstream synchronization and automatic Windows Releases

This repository follows [berichan/PokoHex](https://github.com/berichan/PokoHex) while keeping local automation.

## Automated workflow

Workflow: [`.github/workflows/upstream-sync-release.yml`](../.github/workflows/upstream-sync-release.yml)

- GitHub Actions checks the upstream `main` branch at minute **23 of every hour (UTC)**. GitHub may delay scheduled runs.
- If upstream has commits not present locally, Actions performs a normal Git merge into this repository's `main`. It never force-pushes and never discards local changes. An unresolved merge conflict fails the job.
- The workflow determines the upstream SHA and associated Release tag `upstream-<first 12 SHA characters>`.
- When the tag has no Release (or lacks the Windows ZIP), it builds the synchronized source. This includes an initial baseline Release and automatic retries of previously failed builds.
- When a Release with the Windows ZIP already exists for the current upstream SHA, the scheduled job exits without rebuilding.
- A read-only Windows build job restores and compiles `PokoHex.slnx` using **.NET 10**, runs `PokoHex.Tests`, publishes a **self-contained Windows x64** app, and packs `LICENSE.txt` with the app into `PokoHex-win-x64.zip`.
- Only after a successful build/test/package does a separate Release job with write permission download the tested artifact and create or update a GitHub Release with the ZIP. The Release is tagged at the **synced source commit** while the tag name identifies the upstream commit.
- A release is not created on a build failure. Subsequent scheduled runs retry it automatically while the Release is missing.

## Run manually

Open **Actions → Sync upstream and publish Windows release → Run workflow**.

- **rebuild = false**: check/sync upstream, then publish only if the current upstream SHA has no Release.
- **rebuild = true**: re-run build/test/package and create a missing Release or replace the ZIP asset on an existing Release. This is useful after repairing a broken CI pipeline.

## Authentication and permissions

The workflow uses only GitHub's built-in `GITHUB_TOKEN`; no PAT, Deploy Key, or user Secret is needed in the normal case. The sync and Release jobs request `contents: write` for pushing the merge and creating Releases. The build/test job uses `contents: read` only, so builds of upstream code do not run with a write-capable GitHub token.

If a push or Release creation fails with a permission error, check **Settings → Actions → General → Workflow permissions**, plus any branch protection/rulesets for `main`. Repository/org restrictions can prevent `GITHUB_TOKEN` writes. Do not turn on force-push to bypass protections.

The original upstream `windows-release.yml` and `web-pages.yml` remain separate. The automatic workflow itself builds and uploads the ZIP; it does **not** rely on a `release: published` workflow event, because GitHub may suppress recursively triggered workflows when using `GITHUB_TOKEN`.

## Platforms and versioning

- **Windows**: `PokoHex-win-x64.zip` in this repository's Releases; .NET runtime bundled.
- **Web**: the existing Blazor WebAssembly workflow builds a static site and can deploy to GitHub Pages manually when Pages is configured. It is not auto-deployed by the upstream sync workflow.
- **macOS/Linux**: no native desktop package, since the desktop project uses WinForms (`net10.0-windows`).
- **Version tag**: `upstream-<SHA12>` avoids version collisions with the upstream's human-assigned releases such as `0.2.0`. Each published Release documents both the upstream SHA and the merged local source SHA.

## Troubleshooting

| Symptom | What to check |
| --- | --- |
| No scheduled runs | The workflow must be on default branch `main`; scheduled tasks can be delayed, and GitHub may disable them after prolonged inactivity on public repositories. |
| Merge conflicts | Inspect the failed `Sync upstream main` step, resolve via a normal merge/commit, then manually rerun. |
| Push denied | Verify Actions token write permissions and `main` branch protection. |
| Compilation or tests fail | Inspect the Windows job; no Release is published. The next poll retries if the Release is missing its Windows ZIP. |
| Release/asset upload denied | Verify `contents: write` and Actions policies; manually rerun with **rebuild** after repair. |
| Web not deployed | Use the separate `Web build and Pages deployment` workflow with its `deploy` input, and configure GitHub Pages first. |

Remember to back up save data before using any save editor.
