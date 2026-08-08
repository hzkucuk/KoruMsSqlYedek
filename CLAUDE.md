# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

KoruMsSqlYedek — SQL Server backup & cloud sync system. .NET 10 WinForms tray app + Windows Service. Backs up SQL databases and files, uploads to Google Drive / FTP / SFTP / UNC.

## Build & Test

Primary dev environment is **macOS**. All projects target `net10.0-windows`;
`Directory.Build.props` sets `EnableWindowsTargeting` on non-Windows hosts so
compilation works, but the produced binaries only *run* on Windows.

| Task | macOS | Windows |
|------|-------|---------|
| `dotnet build` | ✅ works | ✅ |
| `dotnet test` | ❌ needs `Microsoft.WindowsDesktop.App` runtime | ✅ |
| Run tray app / service | ❌ WinForms is Windows-only | ✅ |
| Build installer | ❌ needs Inno Setup (ISCC.exe) | ✅ |

On macOS, tests and the installer run in CI — push a tag or trigger the
Release workflow manually.

```bash
dotnet build                              # Debug build (works on macOS)
dotnet build -c Release                   # Release build
dotnet test                               # Windows only
dotnet test --filter "TestCategory=Unit"  # Windows only — CI release gate
```

## Release

Releases are built by **GitHub Actions** (`.github/workflows/release.yml`) on a
`windows-latest` runner: restore → build → Unit tests → publish (self-contained
win-x64) → Inno Setup → GitHub Release with the installer attached.

```bash
git tag v0.99.88 && git push origin v0.99.88   # builds + publishes the release
gh workflow run Release                        # dry run: installer as artifact, no release
```

`Deployment/Build-Release.ps1` is the Windows-only fallback for producing an
installer locally; it is no longer the primary path.

## Architecture

```
KoruMsSqlYedek.Core     → Models, interfaces, helpers (shared)
KoruMsSqlYedek.Engine   → Business logic: backup, compression, cloud, scheduling
KoruMsSqlYedek.Win      → WinForms tray UI (partial classes per concern)
KoruMsSqlYedek.Service  → Windows Service (IPC via named pipes to Win)
KoruMsSqlYedek.Tests    → MSTest + FluentAssertions + Moq
```

**Key patterns:**
- Large UI classes split into partial classes: `MainWindow.cs` + `MainWindow.Plans.cs`, `MainWindow.BackupActivity.cs`, `MainWindow.BackupLog.cs`, etc.
- Engine providers implement `ICloudProvider` interface, created via `CloudProviderFactory`
- `BackupJobExecutor` orchestrates the full pipeline: SQL backup → compress → upload → retention → notify
- `CloudUploadOrchestrator` handles multi-target upload with retry and recovery
- Plans stored as JSON in `%ProgramData%\KoruMsSqlYedek\Plans\`
- Quartz.NET cron scheduling via `QuartzSchedulerService`

## Version Management

Version must be updated in **4 places** simultaneously:
1. `KoruMsSqlYedek.Win/KoruMsSqlYedek.Win.csproj` → `<Version>`, `<AssemblyVersion>`, `<FileVersion>`, `<ApplicationVersion>`
2. `KoruMsSqlYedek.Win/Properties/AssemblyInfo.cs` → `AssemblyVersion` + `AssemblyFileVersion`
3. `KoruMsSqlYedek.Service/KoruMsSqlYedek.Service.csproj` → `<Version>`, `<AssemblyVersion>`, `<FileVersion>`
4. `Deployment/InnoSetup/KoruMsSqlYedek.iss` → `#define MyAppVersion`

⚠️ The Service csproj is **not UTF-8** (Windows-1254). `grep` treats it as binary
and silently skips it — use `grep -a` / `LC_ALL=C sed` and verify the byte diff.
CI reads the version from `AssemblyInfo.cs`, so that file is the source of truth.

SemVer: breaking=MAJOR, feature=MINOR, fix=PATCH.

## Git Strategy

- `master`: release merges only (no direct commits)
- `develop`: daily work
- Commit types: `feat`, `fix`, `refactor`, `docs`, `chore`

## Post-Task Automation

After completing each task:
1. Update version in 3 places (SemVer)
2. Update CHANGELOG.md
3. `dotnet build` to verify
4. `git add && git commit && git push origin develop`

## Critical Rules

### UI Thread Safety
All UI access must go through `Invoke`/`BeginInvoke`. Background threads (timers, callbacks, engine events) never touch controls directly.

### PlanId Propagation
Every Engine→UI event/callback must carry `PlanId`. `AppendBackupLog` requires PlanId. New event args must include `string PlanId`.

### Dictionary Safety
Never call `Clear()` on state dictionaries (`_nextFireTimes`, `_planLogs`, `_planProgress`). Only update/remove individual keys.

### Event Handler Completeness
Adding a new `BackupActivityType` requires updating **5 points**: `OnBackupActivityChanged` switch, `BuildActivityLogLine`, `GetLogColor`, `UpdatePlanRowStatus`, and progress bar handling.

### High-Risk Files
| File | Risk | Note |
|------|------|------|
| `MainWindow.OnBackupActivityChanged` | 🔴 | Missing case = silent failure |
| `MainWindow.AppendBackupLog` | 🔴 | Buffer+UI+color+progress |
| `GoogleDriveProvider.EmptyTrashAsync` | 🔴 | `Files.EmptyTrash()` FORBIDDEN; use folder-scoped query |
| `CloudUploadOrchestrator.cs` | 🟡 | PlanId required |
| `BackupJobExecutor.cs` | 🟡 | PlanId required |

### SQL Server Express
Express edition does not support `BackupCompressionOptions.On`. Always check `isExpress` before enabling native SQL compression.
