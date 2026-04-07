# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

KoruMsSqlYedek — SQL Server backup & cloud sync system. .NET 10 WinForms tray app + Windows Service. Backs up SQL databases and files, uploads to Google Drive / FTP / SFTP / UNC.

## Build & Test

```bash
dotnet build                              # Debug build
dotnet build -c Release                   # Release build
dotnet test                               # Run all tests
dotnet test --filter "FullyQualifiedName~CloudProviderFactory"  # Single test class
dotnet test --filter "TestCategory=Unit"  # Category filter

# Full release (publish + ZIP + Inno Setup installer)
powershell -ExecutionPolicy Bypass -File Deployment/Build-Release.ps1 -SkipTests
```

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

Version must be updated in **3 places** simultaneously:
1. `KoruMsSqlYedek.Win/KoruMsSqlYedek.Win.csproj` → `<ApplicationVersion>`
2. `KoruMsSqlYedek.Win/Properties/AssemblyInfo.cs` → `AssemblyVersion` + `AssemblyFileVersion`
3. `Deployment/InnoSetup/KoruMsSqlYedek.iss` → `#define MyAppVersion`

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
