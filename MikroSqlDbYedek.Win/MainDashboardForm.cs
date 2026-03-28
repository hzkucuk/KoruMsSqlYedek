using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using MikroSqlDbYedek.Core.Interfaces;
using MikroSqlDbYedek.Core.Models;
using MikroSqlDbYedek.Win.Helpers;
using Serilog;

namespace MikroSqlDbYedek.Win
{
    /// <summary>
    /// Ana dashboard formu — son yedeklemeler, durum özeti, sonraki zamanlama.
    /// Tray ikonundan açılır; kapatıldığında gizlenir, uygulama kapanmaz.
    /// </summary>
    public partial class MainDashboardForm : Form
    {
        private static readonly ILogger Log = Serilog.Log.ForContext<MainDashboardForm>();
        private readonly IPlanManager _planManager;
        private readonly IBackupHistoryManager _historyManager;
        private readonly Timer _refreshTimer;

        public MainDashboardForm(IPlanManager planManager, IBackupHistoryManager historyManager)
        {
            if (planManager == null) throw new ArgumentNullException(nameof(planManager));
            if (historyManager == null) throw new ArgumentNullException(nameof(historyManager));

            InitializeComponent();
            ApplyLocalization();

            _planManager = planManager;
            _historyManager = historyManager;

            _refreshTimer = new Timer { Interval = 30000 }; // 30 saniye
            _refreshTimer.Tick += OnRefreshTimerTick;
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            LoadDashboardData();
            _refreshTimer.Start();
            Log.Debug("Dashboard gösterildi.");
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                Hide();
                _refreshTimer.Stop();
                Log.Debug("Dashboard gizlendi (tray'de çalışmaya devam ediyor).");

                return;
            }

            _refreshTimer.Stop();
            _refreshTimer.Dispose();
            base.OnFormClosing(e);
        }

        private void OnRefreshTimerTick(object sender, EventArgs e)
        {
            LoadDashboardData();
        }

        #region Data Loading

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
                var last = recentHistory[0];
                UpdateLastBackupStatus(last);
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
                    _lblStatusValue.ForeColor = Color.Green;
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
            _lvLastBackups.Items.Clear();

            foreach (var result in history)
            {
                var item = new ListViewItem(result.StartedAt.ToString("yyyy-MM-dd HH:mm"));
                item.SubItems.Add(result.PlanName ?? "—");
                item.SubItems.Add(result.DatabaseName ?? "—");
                item.SubItems.Add(GetBackupTypeName(result.BackupType));
                item.SubItems.Add(GetStatusName(result.Status));
                item.SubItems.Add(FormatFileSize(result.CompressedSizeBytes > 0
                    ? result.CompressedSizeBytes
                    : result.FileSizeBytes));
                item.Tag = result;

                // Renk kodlaması
                switch (result.Status)
                {
                    case BackupResultStatus.Failed:
                        item.ForeColor = Color.Red;
                        break;
                    case BackupResultStatus.PartialSuccess:
                        item.ForeColor = Color.Orange;
                        break;
                    case BackupResultStatus.Cancelled:
                        item.ForeColor = SystemColors.GrayText;
                        break;
                }

                _lvLastBackups.Items.Add(item);
            }
        }

        #endregion

        #region Formatting Helpers

        private static string GetBackupTypeName(SqlBackupType type)
        {
            switch (type)
            {
                case SqlBackupType.Full: return Res.Get("Dashboard_TypeFull");
                case SqlBackupType.Differential: return Res.Get("Dashboard_TypeDiff");
                case SqlBackupType.Incremental: return Res.Get("Dashboard_TypeInc");
                default: return type.ToString();
            }
        }

        private static string GetStatusName(BackupResultStatus status)
        {
            switch (status)
            {
                case BackupResultStatus.Success: return Res.Get("Dashboard_ResultSuccess");
                case BackupResultStatus.PartialSuccess: return Res.Get("Dashboard_ResultPartial");
                case BackupResultStatus.Failed: return Res.Get("Dashboard_ResultFailed");
                case BackupResultStatus.Cancelled: return Res.Get("Dashboard_ResultCancelled");
                default: return status.ToString();
            }
        }

        private static string FormatFileSize(long bytes)
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

        #endregion

        #region Localization

        private void ApplyLocalization()
        {
            Text = Res.Get("Dashboard_Title");
            _lblTitle.Text = "MikroSqlDbYedek";
            _lblSubtitle.Text = Res.Get("Dashboard_Title");
            _lblStatusCaption.Text = Res.Get("Dashboard_StatusCaption");
            _lblStatusValue.Text = Res.Get("Dashboard_Ready");
            _lblNextBackupCaption.Text = Res.Get("Dashboard_NextBackupCaption");
            _lblActivePlansCaption.Text = Res.Get("Dashboard_ActivePlansCaption");
            _lblGridTitle.Text = Res.Get("Dashboard_LastBackupsGroup");
            _colDate.Text = Res.Get("Dashboard_ColDate");
            _colPlan.Text = Res.Get("Dashboard_ColPlan");
            _colDatabase.Text = Res.Get("Dashboard_ColDatabase");
            _colResult.Text = Res.Get("Dashboard_ColResult");
            _colSize.Text = Res.Get("Dashboard_ColSize");
            _tslStatus.Text = Res.Get("Dashboard_Ready");
        }

        #endregion
    }
}
