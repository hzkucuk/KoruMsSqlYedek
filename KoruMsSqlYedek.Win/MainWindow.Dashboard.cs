using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
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
            if (eta.TotalSeconds < 60) return $"{(int)eta.TotalSeconds} {Res.Get("Time_Seconds")}";
            if (eta.TotalMinutes < 60) return $"{(int)eta.TotalMinutes} {Res.Get("Time_Minutes")} {eta.Seconds} {Res.Get("Time_Seconds")}";
            return $"{(int)eta.TotalHours} {Res.Get("Time_Hours")} {eta.Minutes} {Res.Get("Time_Minutes")}";
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

        /// <summary>Dashboard son yedeklemeler grid'ini CSV olarak dışa aktarır.</summary>
        private void OnDashboardExportClick(object? sender, EventArgs e)
        {
            using var sfd = new SaveFileDialog();
            sfd.Title = Res.Get("Dashboard_ExportDialogTitle");
            sfd.Filter = Res.Get("Dashboard_ExportFilter");
            sfd.FileName = "KoruMsSqlYedek_Backups_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".csv";

            if (sfd.ShowDialog(this) != DialogResult.OK)
                return;

            try
            {
                var history = _historyManager.GetRecentHistory(50);

                using var sw = new StreamWriter(sfd.FileName, false, new UTF8Encoding(true));
                // CSV header
                sw.WriteLine("Plan,Database,Type,Status,StartedAt,CompletedAt,Duration,Size,CompressedSize,Error");

                foreach (var r in history)
                {
                    string duration = r.Duration.HasValue
                        ? r.Duration.Value.ToString(@"hh\:mm\:ss")
                        : "";
                    string completedAt = r.CompletedAt.HasValue
                        ? r.CompletedAt.Value.ToString("yyyy-MM-dd HH:mm:ss")
                        : "";

                    sw.WriteLine("{0},{1},{2},{3},{4},{5},{6},{7},{8},{9}",
                        EscapeCsv(r.PlanName),
                        EscapeCsv(r.DatabaseName),
                        r.BackupType,
                        r.Status,
                        r.StartedAt.ToString("yyyy-MM-dd HH:mm:ss"),
                        completedAt,
                        duration,
                        FormatFileSize(r.FileSizeBytes),
                        FormatFileSize(r.CompressedSizeBytes),
                        EscapeCsv(r.ErrorMessage));
                }

                Theme.ModernMessageBox.Show(
                    Res.Format("Dashboard_ExportSuccessFormat", history.Count),
                    Res.Get("Dashboard_ExportSuccessTitle"),
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                Theme.ModernMessageBox.Show(
                    Res.Format("Dashboard_ExportError", ex.Message),
                    Res.Get("Error"),
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private static string EscapeCsv(string? value)
        {
            if (string.IsNullOrEmpty(value)) return "";
            if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
                return "\"" + value.Replace("\"", "\"\"") + "\"";
            return value;
        }
    }
}
