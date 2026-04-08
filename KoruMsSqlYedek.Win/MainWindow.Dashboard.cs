using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using KoruMsSqlYedek.Core.Models;
using KoruMsSqlYedek.Win.Helpers;

namespace KoruMsSqlYedek.Win
{
    // TAB 0: Dashboard — Durum özeti, son yedeklemeler (gruplanmış collapsible paneller).
    public partial class MainWindow
    {
        private void LoadDashboardData()
        {
            try
            {
                LoadStatusSummary();
                LoadRecentBackups();
                _tslStatus.Text = Res.Format("Dashboard_LastUpdate", DateTime.Now.ToString("HH:mm:ss"));
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Dashboard verileri yüklenirken hata oluştu.");
                _tslStatus.Text = Res.Get("Dashboard_DataLoadError");
            }
        }

        private void LoadStatusSummary()
        {
            var plans = _planManager.GetAllPlans();
            int activePlanCount = plans.Count(p => p.IsEnabled);
            _lblActivePlansValue.Text = activePlanCount.ToString();

            var recentHistory = _historyManager.GetRecentHistory(1);
            if (recentHistory.Count > 0)
            {
                UpdateLastBackupStatus(recentHistory[0]);
            }
            else
            {
                _lblStatusValue.Text = Res.Get("Dashboard_NoBackupYet");
                _lblStatusValue.ForeColor = SystemColors.GrayText;
                _lblNextBackupValue.Text = "—";
            }
        }

        private void UpdateLastBackupStatus(BackupResult last)
        {
            switch (last.Status)
            {
                case BackupResultStatus.Success:
                    _lblStatusValue.Text = Res.Get("Dashboard_StatusSuccess");
                    _lblStatusValue.ForeColor = Color.LimeGreen;
                    break;
                case BackupResultStatus.PartialSuccess:
                    _lblStatusValue.Text = Res.Get("Dashboard_StatusPartial");
                    _lblStatusValue.ForeColor = Color.Orange;
                    break;
                case BackupResultStatus.Failed:
                    _lblStatusValue.Text = Res.Get("Dashboard_StatusFailed");
                    _lblStatusValue.ForeColor = Color.Red;
                    break;
                case BackupResultStatus.Cancelled:
                    _lblStatusValue.Text = Res.Get("Dashboard_StatusCancelled");
                    _lblStatusValue.ForeColor = SystemColors.GrayText;
                    break;
            }

            if (last.CompletedAt.HasValue)
            {
                var ago = DateTime.Now - last.CompletedAt.Value;
                _lblNextBackupValue.Text = FormatTimeAgo(ago) + " " + Res.Get("Dashboard_TimeAgo") + " — " + last.PlanName;
            }
        }

        private void LoadRecentBackups()
        {
            var history = _historyManager.GetRecentHistory(50);
            _olvLastBackups.SetData(history);
        }

        private static string FormatEta(long bytesRemaining, long speedBytesPerSecond)
        {
            if (speedBytesPerSecond <= 0 || bytesRemaining <= 0) return string.Empty;
            var eta = TimeSpan.FromSeconds(bytesRemaining / (double)speedBytesPerSecond);
            if (eta.TotalSeconds < 60) return $"{(int)eta.TotalSeconds} sn";
            if (eta.TotalMinutes < 60) return $"{(int)eta.TotalMinutes} dk {eta.Seconds} sn";
            return $"{(int)eta.TotalHours} sa {eta.Minutes} dk";
        }

        internal static string FormatFileSize(long bytes)
        {
            if (bytes <= 0) return "—";
            if (bytes < 1024) return bytes + " B";
            if (bytes < 1024 * 1024) return (bytes / 1024.0).ToString("F1") + " KB";
            if (bytes < 1024 * 1024 * 1024) return (bytes / (1024.0 * 1024)).ToString("F1") + " MB";
            return (bytes / (1024.0 * 1024 * 1024)).ToString("F2") + " GB";
        }

        private static string FormatTimeAgo(TimeSpan span)
        {
            if (span.TotalMinutes < 1) return Res.Get("Dashboard_TimeJustNow");
            if (span.TotalMinutes < 60) return Res.Format("Dashboard_TimeMinFormat", (int)span.TotalMinutes);
            if (span.TotalHours < 24) return Res.Format("Dashboard_TimeHourFormat", (int)span.TotalHours);
            return Res.Format("Dashboard_TimeDayFormat", (int)span.TotalDays);
        }
    }
}
