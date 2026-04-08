using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using BrightIdeasSoftware;
using KoruMsSqlYedek.Core.Models;
using KoruMsSqlYedek.Win.Helpers;

namespace KoruMsSqlYedek.Win
{
    // TAB 0: Dashboard — Durum özeti, son yedeklemeler, sıralama/gruplama.
    public partial class MainWindow
    {
        /// <summary>
        /// ObjectListView kolonlarını, renderer'ları ve gruplama mantığını ayarlar.
        /// Constructor'dan (InitializeComponent sonrası) bir kez çağrılır.
        /// </summary>
        private void SetupDashboardOlv()
        {
            // OwnerDraw — OLV kendi GDI+ renderer'ıyla hücre metnini kolon
            // sınırlarına clip eder; native ListView modunda metin taşması olabiliyor.
            _olvLastBackups.OwnerDraw = true;

            // Tarih kolonu — AspectToStringConverter ile biçimli metin
            _olvColDate.AspectToStringConverter = obj =>
                obj is DateTime dt ? dt.ToString("yyyy-MM-dd HH:mm") : "—";

            // Plan kolonu — null güvenliği
            _olvColPlan.AspectToStringConverter = obj =>
                obj is string s && !string.IsNullOrEmpty(s) ? s : "—";

            // Veritabanı kolonu
            _olvColDatabase.AspectToStringConverter = obj =>
                obj is string s && !string.IsNullOrEmpty(s) ? s : "—";

            // Tür kolonu — enum → Türkçe metin
            _olvColType.AspectToStringConverter = obj =>
                obj is SqlBackupType t ? GetBackupTypeName(t) : "—";

            // Sonuç kolonu — enum → Türkçe metin
            _olvColResult.AspectToStringConverter = obj =>
                obj is BackupResultStatus st ? GetStatusName(st) : "—";

            // Boyut kolonu — bayt → okunur metin
            _olvColSize.AspectGetter = rowObj =>
            {
                if (rowObj is not BackupResult r) return 0L;
                return r.CompressedSizeBytes > 0 ? r.CompressedSizeBytes : r.FileSizeBytes;
            };
            _olvColSize.AspectToStringConverter = obj =>
                obj is long b ? FormatFileSize(b) : "—";

            // Sıralama: varsayılan tarih azalan
            _olvLastBackups.PrimarySortColumn = _olvColDate;
            _olvLastBackups.PrimarySortOrder = SortOrder.Descending;

            // Gruplama: Plan adına göre
            _olvColPlan.GroupKeyGetter = rowObj =>
                (rowObj as BackupResult)?.PlanName ?? "—";
            _olvLastBackups.AlwaysGroupByColumn = _olvColPlan;
            _olvLastBackups.AlwaysGroupBySortOrder = SortOrder.Ascending;
            _olvLastBackups.ShowItemCountOnGroups = true;
            _olvLastBackups.GroupWithItemCountFormat = "{0}  —  {1} " + Res.Get("Dashboard_GroupBackupCount");
            _olvLastBackups.GroupWithItemCountSingularFormat = "{0}  —  {1} " + Res.Get("Dashboard_GroupBackupCount");

            // Satır renklendirme — duruma göre
            _olvLastBackups.FormatRow += (sender, e) =>
            {
                if (e.Model is not BackupResult r) return;
                switch (r.Status)
                {
                    case BackupResultStatus.Failed:
                        e.Item.ForeColor = Theme.ModernTheme.StatusError;
                        break;
                    case BackupResultStatus.PartialSuccess:
                        e.Item.ForeColor = Theme.ModernTheme.StatusWarning;
                        break;
                    case BackupResultStatus.Cancelled:
                        e.Item.ForeColor = Theme.ModernTheme.StatusCancelled;
                        break;
                    default:
                        e.Item.ForeColor = Theme.ModernTheme.TextPrimary;
                        break;
                }
            };

            // Dark theme renkleri
            ApplyOlvTheme();
        }

        /// <summary>
        /// OLV'ye ModernTheme renklerini uygular. Tema değişimlerinde de çağrılabilir.
        /// </summary>
        private void ApplyOlvTheme()
        {
            _olvLastBackups.BackColor = Theme.ModernTheme.SurfaceColor;
            _olvLastBackups.ForeColor = Theme.ModernTheme.TextPrimary;
            _olvLastBackups.AlternateRowBackColor = Theme.ModernTheme.GridAlternateRow;
            _olvLastBackups.UseAlternatingBackColors = true;

            _olvLastBackups.HeaderUsesThemes = false;
            _olvLastBackups.HeaderFormatStyle = new HeaderFormatStyle();
            _olvLastBackups.HeaderFormatStyle.SetBackColor(Theme.ModernTheme.GridHeaderBack);
            _olvLastBackups.HeaderFormatStyle.SetForeColor(Theme.ModernTheme.GridHeaderText);
            _olvLastBackups.HeaderFormatStyle.SetFont(Theme.ModernTheme.FontCaptionBold);

            _olvLastBackups.SelectedBackColor = Theme.ModernTheme.GridSelectionBack;
            _olvLastBackups.SelectedForeColor = Theme.ModernTheme.TextPrimary;
            _olvLastBackups.UnfocusedSelectedBackColor = Theme.ModernTheme.GridSelectionBackUnfocused;
            _olvLastBackups.UnfocusedSelectedForeColor = Theme.ModernTheme.TextPrimary;

            _olvLastBackups.UseExplorerTheme = false;
            _olvLastBackups.GroupHeaderForeColor = Theme.ModernTheme.AccentPrimaryHover;

            Theme.ModernTheme.ApplyScrollBarTheme(_olvLastBackups);
        }

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
            _olvLastBackups.SetObjects(history);
        }

        private static string GetBackupTypeName(SqlBackupType type) => type switch
        {
            SqlBackupType.Full => Res.Get("Dashboard_TypeFull"),
            SqlBackupType.Differential => Res.Get("Dashboard_TypeDiff"),
            SqlBackupType.Incremental => Res.Get("Dashboard_TypeInc"),
            _ => type.ToString()
        };

        private static string GetStatusName(BackupResultStatus status) => status switch
        {
            BackupResultStatus.Success => Res.Get("Dashboard_ResultSuccess"),
            BackupResultStatus.PartialSuccess => Res.Get("Dashboard_ResultPartial"),
            BackupResultStatus.Failed => Res.Get("Dashboard_ResultFailed"),
            BackupResultStatus.Cancelled => Res.Get("Dashboard_ResultCancelled"),
            _ => status.ToString()
        };

        private static string FormatFileSize(long bytes)
        {
            if (bytes <= 0) return "—";
            if (bytes < 1024) return bytes + " B";
            if (bytes < 1024 * 1024) return (bytes / 1024.0).ToString("F1") + " KB";
            if (bytes < 1024 * 1024 * 1024) return (bytes / (1024.0 * 1024)).ToString("F1") + " MB";
            return (bytes / (1024.0 * 1024 * 1024)).ToString("F2") + " GB";
        }

        private static string FormatEta(long bytesRemaining, long speedBytesPerSecond)
        {
            if (speedBytesPerSecond <= 0 || bytesRemaining <= 0) return string.Empty;
            var eta = TimeSpan.FromSeconds(bytesRemaining / (double)speedBytesPerSecond);
            if (eta.TotalSeconds < 60) return $"{(int)eta.TotalSeconds} sn";
            if (eta.TotalMinutes < 60) return $"{(int)eta.TotalMinutes} dk {eta.Seconds} sn";
            return $"{(int)eta.TotalHours} sa {eta.Minutes} dk";
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
