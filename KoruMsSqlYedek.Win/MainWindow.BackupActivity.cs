using System;
using System.Drawing;
using System.Windows.Forms;
using KoruMsSqlYedek.Core;
using KoruMsSqlYedek.Core.Events;
using KoruMsSqlYedek.Core.Models;

namespace KoruMsSqlYedek.Win
{
    // Backup activity event handler, plan satır durumu, log satır oluşturma, renk eşlemesi.
    public partial class MainWindow
    {
        /// <summary>Completed sonrası %100'ü 3 saniye gösterip sıfırlayan timer.</summary>
        private System.Windows.Forms.Timer _progressResetTimer;

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

            bool isProgress = e.ActivityType == BackupActivityType.CloudUploadProgress
                || (e.ActivityType == BackupActivityType.StepChanged
                    && !string.IsNullOrEmpty(e.Message)
                    && e.Message.Contains("ıkıştırılıyor"));
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
                => string.Format("[{0}] Yedekleme başladı.", e.PlanName ?? e.PlanId),

            BackupActivityType.DatabaseProgress
                => string.Format("{0} ({1}/{2}) işleniyor.", e.DatabaseName, e.CurrentIndex, e.TotalCount),

            BackupActivityType.StepChanged
                => !string.IsNullOrEmpty(e.Message) ? e.Message : string.Format("Adım: {0}", e.StepName),

            BackupActivityType.CloudUploadStarted
                => string.Format("Bulut yükleme başladı: {0}", e.CloudTargetName),

            BackupActivityType.CloudUploadProgress
                => BuildCloudUploadLogLine(e),

            BackupActivityType.CloudUploadCompleted
                => BuildCloudUploadCompletedLine(e),

            BackupActivityType.CloudUploadAbandoned
                => e.AbandonedFiles is { Count: > 0 }
                    ? string.Format("⚠ Bulut yükleme terk edildi ({0} dosya): {1}", e.AbandonedFiles.Count, string.Join(", ", e.AbandonedFiles))
                    : string.Format("⚠ Bulut yükleme terk edildi: {0}", e.Message ?? "Maksimum deneme aşıldı"),

            BackupActivityType.Completed
                => e.IsSuccess || string.IsNullOrEmpty(e.Message)
                    ? string.Format("[{0}] Yedekleme tamamlandı. ✓", e.PlanName ?? e.PlanId)
                    : string.Format("[{0}] Yedekleme tamamlandı (bulut yükleme başarısız). ⚠", e.PlanName ?? e.PlanId),

            BackupActivityType.Failed
                => string.Format("[{0}] Yedekleme başarısız: {1}", e.PlanName ?? e.PlanId, e.Message),

            BackupActivityType.Cancelled
                => string.Format("[{0}] Yedekleme iptal edildi.", e.PlanName ?? e.PlanId),

            _ => throw new ArgumentOutOfRangeException(
                nameof(e.ActivityType), e.ActivityType,
                $"Unhandled BackupActivityType: {e.ActivityType}. Tüm 5 sorumluluk noktasını güncelleyin.")
        };

        private string BuildCloudUploadLogLine(BackupActivityEventArgs e)
        {
            string fileInfo = !string.IsNullOrEmpty(e.CloudFileName)
                ? $" [{e.CloudFileIndex}/{Math.Max(e.CloudFileTotal, 1)}] {e.CloudFileName}"
                : "";

            if (e.ProgressPercent >= 100)
            {
                // %100 tamamlandı mesajı — dosya adı dahil
                if (e.BytesTotal > 0)
                    return string.Format("Yükleme tamamlandı{0}: %100 | Gönderilen: {1}",
                        fileInfo, FormatFileSize(e.BytesTotal));

                return string.Format("Yükleme tamamlandı{0}: %100", fileInfo);
            }

            if (e.BytesTotal > 0)
            {
                long bytesRemaining = e.BytesTotal - e.BytesSent;
                string etaStr = e.SpeedBytesPerSecond > 0
                    ? FormatEta(bytesRemaining, e.SpeedBytesPerSecond)
                    : "";
                string etaPart = etaStr.Length > 0 ? $" | Süre: {etaStr}" : "";
                return string.Format("Yükleniyor{0}: %{1} | Gönderilen: {2}/{3} | Kalan: {4} | Hız: {5}/s{6}",
                    fileInfo,
                    e.ProgressPercent,
                    FormatFileSize(e.BytesSent),
                    FormatFileSize(e.BytesTotal),
                    FormatFileSize(bytesRemaining),
                    FormatFileSize(e.SpeedBytesPerSecond),
                    etaPart);
            }
            return string.Format("Yükleniyor{0}: %{1}", fileInfo, e.ProgressPercent);
        }

        private string BuildCloudUploadCompletedLine(BackupActivityEventArgs e)
        {
            string fileName = !string.IsNullOrEmpty(e.CloudFileName)
                ? $" | {e.CloudFileName}"
                : "";

            if (!e.IsSuccess)
                return string.Format("Bulut {0}: Başarısız ✕{1} — {2}", e.CloudTargetName, fileName, e.Message ?? "Bilinmeyen hata");

            // Boyut bilgisi
            string sizeInfo = e.LocalFileSizeBytes > 0
                ? $" [{FormatFileSize(e.LocalFileSizeBytes)}]"
                : "";

            // Sağlık kontrolü sonucu
            string healthCheck = e.IsIntegrityVerified switch
            {
                true  => " — Doğrulandı ✓",
                false => " — Boyut uyuşmazlığı ⚠",
                null  => ""
            };

            return string.Format("Bulut {0}: Başarılı{1}{2}{3}", e.CloudTargetName, fileName, sizeInfo, healthCheck);
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
