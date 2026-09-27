# PromptRun for Command Palette

Prompt manager extension for PowerToys **Command Palette (CmdPal)**: summon anywhere, search prompts instantly, copy with one key, sync via a private GitHub repo.

- Fully independent — zero code/data links to PromptPal or the PowerToys Run version of PromptRun
- Self-owned single-file storage: `%LOCALAPPDATA%\Microsoft\PowerToys\CmdPal\PromptRun\prompts.json`
- First-run **one-click import** from the Run version's `prompts.json` (favorites and useCount history preserved; the original file is never modified)

## Features

- **Search**: multi-token AND matching (title / tags / content), ranked by favorite → useCount → match weight → updatedAt
- **Copy**: Enter copies the content verbatim (`{{placeholders}}` are not substituted; items are annotated), useCount +1 on copy
- **Details**: Markdown-rendered preview plus metadata
- **Manage**: empty query lists all prompts followed by actions (open data folder / Push / Pull / first-run import)
- **Sync**: single-file GitHub private-repo sync (Contents API); Pull creates a `.bak` backup automatically

## Install (from GitHub Releases)

1. Download `PromptRun.CmdPal_<version>_x64.msix` and `promptrun-sign.cer` from Releases
2. Double-click the `.cer` → Install Certificate → **Local Machine** → Place all certificates in the following store → **Trusted People**
3. Double-click the `.msix` to install (or `Add-AppxPackage -Path <msix>`)
4. Open Command Palette (`Win+Alt+Space`) → type PromptRun

Upgrades: install the new msix over the old one; your data file is untouched.

## First run

- If the Run version's data file exists, an **Import from PowerToys Run** item appears at the top; confirm to migrate. Once a local file exists the entry disappears.
- Otherwise a template `prompts.json` with examples is generated; hand-edit it freely (UTF-8, 2-space indent) — changes hot-reload within 300 ms.

## GitHub sync setup

1. Create a **private** repo (e.g. `prompts-vault`, no README init)
2. GitHub → Settings → Developer settings → Fine-grained personal access tokens:
   - Repository access: only that repo
   - Permissions → Contents: **Read and write**
3. Command Palette → right-click / context-key on the PromptRun entry → **Settings** → fill in `owner/repo` and the PAT

Push overwrites the remote; Pull overwrites the local file (a `.bak` is created). Without credentials the actions show guidance instead of hitting the network.

## Development

```powershell
dotnet build src/PromptRun.CmdPal/PromptRun.CmdPal.csproj -p:Platform=x64
dotnet test tests/PromptRun.Tests/PromptRun.Tests.csproj
# Sideload MSIX (sign separately with signtool, see .github/workflows/release.yml)
dotnet build src/PromptRun.CmdPal/PromptRun.CmdPal.csproj -p:Platform=x64 -p:Configuration=Release `
  -p:RuntimeIdentifier=win-x64 -p:UapAppxPackageBuildMode=SideloadOnly `
  -p:AppxPackageSigningEnabled=false -p:GenerateAppxPackageOnBuild=true `
  -p:AppxPackageDir="outpkg/"
```

Layout: `src/PromptRun.Core` (pure logic: schema/search/sync, host-free) · `src/PromptRun.CmdPal` (CmdPal host adapter) · `tests` (xUnit). Specs live in `openspec/changes/add-promptrun-cmdpal/`.

## License

MIT
