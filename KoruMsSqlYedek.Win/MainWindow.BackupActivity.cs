using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using KoruMsSqlYedek.Core;
using KoruMsSqlYedek.Core.Events;
using KoruMsSqlYedek.Core.Models;
using KoruMsSqlYedek.Win.Helpers;

namespace KoruMsSqlYedek.Win
{
    // Backup activity event handler, plan satır durumu, log satır oluşturma, renk eşlemesi.
    public partial class MainWindow
    {
        /// <summary>Completed sonrası %100'ü 3 saniye gösterip sıfırlayan timer.</summary>
        private System.Windows.Forms.Timer _progressResetTimer;

        /// <summary>Plan başına son raporlanan bulut dosya indeksi (dosya değişiminde satır dondurulur).</summary>
        private readonly Dictionary<string, int> _lastCloudFileIdx = new();

        private void OnBackupActivityChanged(object sender, BackupActivityEventArgs e)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => OnBackupActivityChanged(sender, e)));
                return;
            }

            switch (e.ActivityType)
            {
                case BackupActivityType.Started:
                    if (!string.IsNullOrEmpty(e.PlanId))
                    {
                        _runningPlanIds.Add(e.PlanId);
                        _planProgressTracker[e.PlanId] = new PlanProgressTracker
                        {
                            DbIndex = 0,
                            DbTotal = e.TotalCount > 0 ? e.TotalCount : 1,
                            SqlDbCount = e.TotalCount,
                            MaxPercent = 0,
                            HasFileBackup = e.HasFileBackup,
                            HasCloudTargets = e.HasCloudTargets
                        };
                    }
                    // Progress bar'ı sadece şu an görüntülenen plan başlıyorsa sıfırla
                    if (e.PlanId == _viewingPlanId)
                    {
                        _progressBar.Value = 0;
                        _progressBar.ShowPercentage = true;
                        _progressBar.DisplayMode = Theme.ProgressBarDisplayMode.Percentage;
                    }
                    UpdatePlanRowProgress(e.PlanId, 0);
                    break;

                case BackupActivityType.DatabaseProgress:
                    if (e.TotalCount > 0)
                    {
                        string dbPlanId = !string.IsNullOrEmpty(e.PlanId) ? e.PlanId : _viewingPlanId;

                        if (!_planProgressTracker.TryGetValue(dbPlanId, out PlanProgressTracker dbTracker))
                        {
                            dbTracker = new PlanProgressTracker { MaxPercent = 0 };
                            _planProgressTracker[dbPlanId] = dbTracker;
                        }

                        int pct = Math.Min(dbTracker.CalculateDatabaseProgress(e.CurrentIndex, e.TotalCount), 99);
                        Log.Debug("[UI] DatabaseProgress: plan={PlanId}, idx={Idx}/{Total}, pct={Pct}", dbPlanId, e.CurrentIndex, e.TotalCount, pct);

                        if (dbPlanId == _viewingPlanId)
                        {
                            _progressBar.DisplayMode = Theme.ProgressBarDisplayMode.Percentage;
                            _progressBar.Value = pct;
                        }
                        UpdatePlanRowProgress(dbPlanId, pct);
                    }
                    break;

                case BackupActivityType.StepChanged:
                    {
                        string stepPlanId = !string.IsNullOrEmpty(e.PlanId) ? e.PlanId : _viewingPlanId;
                        if (_planProgressTracker.TryGetValue(stepPlanId, out PlanProgressTracker stepTracker))
                        {
                            int stepPct = -1;

                            if (e.StepName == "VSS")
                                stepTracker.HasVssUpload = true;
                            else if (e.StepName == "VSS Bulut Yükleme")
                                stepTracker.IsVssPhase = true;
                            else if (e.StepName == "Bulut Yükleme" && !stepTracker.IsConsolidatedCloudPhase)
                                stepPct = stepTracker.StartConsolidatedCloudPhase();
                            else if (e.StepName == "Bulut Yükleme" && stepTracker.IsConsolidatedCloudPhase)
                                stepPct = 99; // Bulut yükleme fazı tamamlandı — Completed'a kadar %99'da tut
                            else if (e.StepName == "Dosya Yedekleme" && !stepTracker.IsFileBackupPhase)
                                stepPct = stepTracker.CalculateFileBackupPhaseStart();
                            else if (e.StepName == "Dosya Yedekleme" && stepTracker.IsFileBackupPhase && e.TotalCount > 0)
                                stepPct = stepTracker.CalculateFileSourceProgress(e.CurrentIndex, e.TotalCount);
                            else if (e.StepName == "Dosya Sıkıştırma" && stepTracker.IsFileBackupPhase)
                                stepPct = stepTracker.CalculateFileCompressionProgress();
                            else if (e.StepName == "Temizlik" && stepTracker.IsFileBackupPhase && !stepTracker.HasCloudTargets)
                                stepPct = stepTracker.CalculateFileCleanupProgress();

                            if (stepPct >= 0)
                            {
                                stepPct = Math.Min(stepPct, 99);
                                Log.Debug("[UI] StepChanged(specific): plan={PlanId}, step={Step}, stepPct={Pct}", stepPlanId, e.StepName, stepPct);
                                if (stepPlanId == _viewingPlanId)
                                {
                                    _progressBar.DisplayMode = Theme.ProgressBarDisplayMode.Percentage;
                                    _progressBar.Value = stepPct;
                                }
                                UpdatePlanRowProgress(stepPlanId, stepPct);
                            }

                            // Local-mode SQL adım bazlı ilerleme
                            int localPct = stepTracker.CalculateLocalStepProgress(e.StepName);
                            if (localPct >= 0)
                            {
                                localPct = Math.Min(localPct, 99);
                                Log.Debug("[UI] StepChanged(local): plan={PlanId}, step={Step}, localPct={Pct}", stepPlanId, e.StepName, localPct);
                                if (stepPlanId == _viewingPlanId)
                                {
                                    _progressBar.DisplayMode = Theme.ProgressBarDisplayMode.Percentage;
                                    _progressBar.Value = localPct;
                                }
                                UpdatePlanRowProgress(stepPlanId, localPct);
                            }
                        }
                    }
                    break;

                case BackupActivityType.CloudUploadProgress:
                    {
                        string uploadPlanId = !string.IsNullOrEmpty(e.PlanId) ? e.PlanId : _viewingPlanId;

                        int cumPct;
                        if (_planProgressTracker.TryGetValue(uploadPlanId, out PlanProgressTracker upTracker)
                            && upTracker.IsConsolidatedCloudPhase)
                        {
                            cumPct = upTracker.CalculateCloudUploadProgress(e.ProgressPercent);
                        }
                        else
                        {
                            cumPct = e.ProgressPercent;
                        }

                        cumPct = Math.Min(cumPct, 99);
                        Log.Debug("[UI] CloudUploadProgress: plan={PlanId}, batchPct={BatchPct}, cumPct={CumPct}, isConsolidated={IsCons}",
                            uploadPlanId, e.ProgressPercent, cumPct,
                            upTracker?.IsConsolidatedCloudPhase ?? false);

                        if (uploadPlanId == _viewingPlanId)
                        {
                            _progressBar.Value = cumPct;
                            if (e.BytesTotal > 0)
                            {
                                _progressBar.DisplayMode = Theme.ProgressBarDisplayMode.CustomText;
                                _progressBar.Text = string.Format("%{0}  {1}/{2}  {3}/s",
                                    cumPct,
                                    FormatFileSize(e.BytesSent),
                                    FormatFileSize(e.BytesTotal),
                                    FormatFileSize(e.SpeedBytesPerSecond));
                            }
                        }
                        UpdatePlanRowProgress(uploadPlanId, cumPct);
                    }
                    break;

                case BackupActivityType.CloudUploadStarted:
                case BackupActivityType.CloudUploadCompleted:
                    break;

                case BackupActivityType.CloudUploadAbandoned:
                    UpdatePlanRowStatusCustom(e.PlanId, "⚠ " + DateTime.Now.ToString("HH:mm"), Theme.ModernTheme.LogWarning);
                    break;

                case BackupActivityType.Completed:
                case BackupActivityType.Failed:
                case BackupActivityType.Cancelled:
                    if (!string.IsNullOrEmpty(e.PlanId))
                    {
                        _runningPlanIds.Remove(e.PlanId);
                        _planProgressTracker.Remove(e.PlanId);
                        _lastCloudFileIdx.Remove(e.PlanId);
                    }

                    if (e.ActivityType == BackupActivityType.Completed && e.IsSuccess)
                    {
                        // Başarılı tamamlanma: %100'ü 3 saniye göster, sonra sıfırla
                        if (e.PlanId == _viewingPlanId)
                        {
                            _progressBar.DisplayMode = Theme.ProgressBarDisplayMode.Percentage;
                            _progressBar.ShowPercentage = true;
                            _progressBar.Value = 100;
                        }
                        UpdatePlanRowProgress(e.PlanId, 100);

                        // Önceki timer'ı iptal et
                        _progressResetTimer?.Stop();
                        _progressResetTimer?.Dispose();

                        string completedPlanId = e.PlanId;
                        _progressResetTimer = new System.Windows.Forms.Timer { Interval = 3000 };
                        _progressResetTimer.Tick += (_, _) =>
                        {
                            _progressResetTimer.Stop();
                            _progressResetTimer.Dispose();
                            _progressResetTimer = null;

                            if (completedPlanId == _viewingPlanId)
                            {
                                _progressBar.ShowPercentage = false;
                                _progressBar.DisplayMode = Theme.ProgressBarDisplayMode.Percentage;
                                _progressBar.Value = 0;
                            }
                            UpdatePlanRowProgress(completedPlanId, 0);
                        };
                        _progressResetTimer.Start();
                    }
                    else
                    {
                        // Failed/Cancelled veya başarısız Completed: hemen sıfırla
                        if (e.PlanId == _viewingPlanId)
                        {
                            _progressBar.ShowPercentage = false;
                            _progressBar.DisplayMode = Theme.ProgressBarDisplayMode.Percentage;
                            _progressBar.Value = 0;
                        }
                        UpdatePlanRowProgress(e.PlanId, 0);
                    }

                    if (e.ActivityType == BackupActivityType.Completed && !e.IsSuccess && !string.IsNullOrEmpty(e.Message))
                        UpdatePlanRowStatusCustom(e.PlanId, "⚠ " + DateTime.Now.ToString("HH:mm"), Theme.ModernTheme.LogWarning);
                    else
                        UpdatePlanRowStatus(e.PlanId, e.ActivityType);
                    break;

                default:
                    Log.Warning("Unhandled BackupActivityType: {ActivityType} — OnBackupActivityChanged güncellenmelidir.", e.ActivityType);
                    break;
            }

            UpdateBackupButtonStates();

            // Yedekleme tamamlandığında sonraki çalışma zamanını güncelle
            if (e.ActivityType is BackupActivityType.Completed
                or BackupActivityType.Failed
                or BackupActivityType.Cancelled)
            {
                RequestNextFireTimesAsync();
            }

            bool isProgress;
            if (e.ActivityType == BackupActivityType.CloudUploadProgress)
            {
                // Per-file tracking: dosya değiştiğinde önceki satır dondurulur, yeni satır eklenir
                string upPlanId = !string.IsNullOrEmpty(e.PlanId) ? e.PlanId : _viewingPlanId;
                int lastIdx = _lastCloudFileIdx.GetValueOrDefault(upPlanId, -1);
                if (e.CloudFileIndex != lastIdx)
                {
                    _lastCloudFileIdx[upPlanId] = e.CloudFileIndex;
                    isProgress = false; // Yeni dosya — önceki satır dondurul, yeni satır eklenir
                }
                else
                {
                    isProgress = true; // Aynı dosya — mevcut satırı güncelle
                }
            }
            else if (e.ActivityType == BackupActivityType.StepChanged
                && !string.IsNullOrEmpty(e.Message)
                && e.Message.Contains("ıkıştırılıyor"))
            {
                isProgress = true;
            }
            else
            {
                isProgress = false;
            }
            Color logColor = GetLogColor(e.ActivityType);

            if (e.ActivityType == BackupActivityType.Completed && !e.IsSuccess && !string.IsNullOrEmpty(e.Message))
                logColor = Theme.ModernTheme.LogWarning;

            AppendBackupLog(e.PlanId, BuildActivityLogLine(e), logColor, isProgress);
        }

        private void UpdatePlanRowStatus(string planId, BackupActivityType activityType)
        {
            (string icon, Color color) = GetStatusDisplay(activityType);
            UpdatePlanRowStatusCustom(planId, icon, color);
        }

        private void UpdatePlanRowStatusCustom(string planId, string icon, Color color)
        {
            foreach (DataGridViewRow row in _dgvPlans.Rows)
            {
                var plan = row.Tag as BackupPlan;
                if (plan == null || plan.PlanId != planId) continue;

                if (row.Cells[_colStatus.Index] != null)
                {
                    row.Cells[_colStatus.Index].Value = icon;
                    row.Cells[_colStatus.Index].Style.ForeColor = color;
                }
                break;
            }
        }

        private static (string Icon, Color Color) GetStatusDisplay(BackupActivityType activityType) => activityType switch
        {
            BackupActivityType.Completed => ("✓ " + DateTime.Now.ToString("HH:mm"), Theme.ModernTheme.StatusSuccess),
            BackupActivityType.Failed    => ("✕ " + DateTime.Now.ToString("HH:mm"), Theme.ModernTheme.StatusError),
            BackupActivityType.Cancelled => ("■ " + DateTime.Now.ToString("HH:mm"), Color.Gray),
            _                            => ("⟳ " + DateTime.Now.ToString("HH:mm"), Theme.ModernTheme.AccentPrimary),
        };

        private void UpdatePlanRowProgress(string planId, int percent)
        {
            if (string.IsNullOrEmpty(planId)) return;

            // Per-plan progress değerini sakla (plan değişiminde geri yüklemek için)
            _planProgress[planId] = percent;

            foreach (DataGridViewRow row in _dgvPlans.Rows)
            {
                var p = row.Tag as BackupPlan;
                if (p == null || p.PlanId != planId) continue;

                if (_colProgress != null && _colProgress.Index >= 0 && _colProgress.Index < row.Cells.Count)
                    row.Cells[_colProgress.Index].Value = percent;
                break;
            }
        }

        private string BuildActivityLogLine(BackupActivityEventArgs e) => e.ActivityType switch
        {
            BackupActivityType.Started
                => Res.Format("Activity_BackupStarted", e.PlanName ?? e.PlanId),

            BackupActivityType.DatabaseProgress
                => Res.Format("Activity_DbProcessing", e.DatabaseName, e.CurrentIndex, e.TotalCount),

            BackupActivityType.StepChanged
                => !string.IsNullOrEmpty(e.Message) ? e.Message : Res.Format("Activity_Step", e.StepName),

            BackupActivityType.CloudUploadStarted
                => "",

            BackupActivityType.CloudUploadProgress
                => BuildCloudUploadLogLine(e),

            BackupActivityType.CloudUploadCompleted
                => "",

            BackupActivityType.CloudUploadAbandoned
                => e.AbandonedFiles is { Count: > 0 }
                    ? Res.Format("Activity_CloudAbandoned", e.AbandonedFiles.Count, string.Join(", ", e.AbandonedFiles))
                    : Res.Format("Activity_CloudAbandonedMsg", e.Message ?? Res.Get("Activity_MaxRetryExceeded")),

            BackupActivityType.Completed
                => e.IsSuccess
                    ? (string.IsNullOrEmpty(e.Message)
                        ? Res.Format("Activity_BackupCompleted", e.PlanName ?? e.PlanId)
                        : Res.Format("Activity_BackupCompletedMsg", e.PlanName ?? e.PlanId, e.Message))
                    : (string.IsNullOrEmpty(e.Message)
                        ? Res.Format("Activity_BackupCompleted", e.PlanName ?? e.PlanId)
                        : Res.Format("Activity_BackupCompletedWarn", e.PlanName ?? e.PlanId, e.Message)),

            BackupActivityType.Failed
                => Res.Format("Activity_BackupFailed", e.PlanName ?? e.PlanId, e.Message),

            BackupActivityType.Cancelled
                => Res.Format("Activity_BackupCancelled", e.PlanName ?? e.PlanId),

            _ => throw new ArgumentOutOfRangeException(
                nameof(e.ActivityType), e.ActivityType,
                $"Unhandled BackupActivityType: {e.ActivityType}")
        };

        private string BuildCloudUploadLogLine(BackupActivityEventArgs e)
        {
            // %100'de satır gösterme — dosya satırı son progress değerinde donmuş kalır
            if (e.ProgressPercent >= 100)
                return "";

            string cloudPrefix = Res.Get("Cloud_UploadPrefix");
            string uploading = Res.Get("Cloud_Uploading");
            string sent = Res.Get("Cloud_Sent");
            string remaining = Res.Get("Cloud_Remaining");
            string speed = Res.Get("Cloud_Speed");
            string eta = Res.Get("Cloud_ETA");

            // Dosya adı prefix
            string filePrefix;
            if (!string.IsNullOrEmpty(e.CloudFileName))
                filePrefix = $"{cloudPrefix} ({e.CloudFileName}) ";
            else if (e.CloudFileTotal > 1)
                filePrefix = $"{cloudPrefix} ({e.CloudFileIndex}/{e.CloudFileTotal}) ";
            else
                filePrefix = $"{cloudPrefix} ";

            if (e.BytesTotal > 0)
            {
                // Batch modunda dosya bazlı boyut/ilerleme göster; tekli modda batch = dosya
                long fileTotalBytes = e.LocalFileSizeBytes > 0 ? e.LocalFileSizeBytes : e.BytesTotal;
                long fileSentBytes = e.LocalFileSizeBytes > 0 ? e.FileBytesSent : e.BytesSent;
                fileSentBytes = Math.Clamp(fileSentBytes, 0, fileTotalBytes);

                int filePct = fileTotalBytes > 0
                    ? (int)(fileSentBytes * 100.0 / fileTotalBytes)
                    : e.ProgressPercent;

                long bytesRemaining = fileTotalBytes - fileSentBytes;
                string etaStr = e.SpeedBytesPerSecond > 0
                    ? FormatEta(bytesRemaining, e.SpeedBytesPerSecond)
                    : "";
                string etaPart = etaStr.Length > 0 ? $" | {eta} {etaStr}" : "";
                return $"{filePrefix}{uploading} %{filePct} | {sent} {FormatFileSize(fileSentBytes)}/{FormatFileSize(fileTotalBytes)} | {remaining} {FormatFileSize(bytesRemaining)} | {speed} {FormatFileSize(e.SpeedBytesPerSecond)}/s{etaPart}";
            }
            return $"{filePrefix}{uploading} %{e.ProgressPercent}";
        }

        private static Color GetLogColor(BackupActivityType activityType) => activityType switch
        {
            BackupActivityType.Started => Theme.ModernTheme.LogStarted,
            BackupActivityType.Completed => Theme.ModernTheme.LogSuccess,
            BackupActivityType.Failed => Theme.ModernTheme.LogError,
            BackupActivityType.Cancelled => Theme.ModernTheme.LogWarning,
            BackupActivityType.DatabaseProgress => Theme.ModernTheme.LogInfo,
            BackupActivityType.StepChanged => Theme.ModernTheme.LogInfo,
            BackupActivityType.CloudUploadStarted => Theme.ModernTheme.LogCloud,
            BackupActivityType.CloudUploadProgress => Theme.ModernTheme.LogProgress,
            BackupActivityType.CloudUploadCompleted => Theme.ModernTheme.LogCloud,
            BackupActivityType.CloudUploadAbandoned => Theme.ModernTheme.LogWarning,
            _ => throw new ArgumentOutOfRangeException(
                nameof(activityType), activityType,
                $"Unhandled BackupActivityType: {activityType}. GetLogColor güncellenmelidir.")
        };
    }
}
