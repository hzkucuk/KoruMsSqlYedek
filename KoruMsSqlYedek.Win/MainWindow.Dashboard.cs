using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using KoruMsSqlYedek.Core.Models;
using KoruMsSqlYedek.Win.Helpers;

namespace KoruMsSqlYedek.Win
{
    // TAB 0: Dashboard — Durum özeti, son yedeklemeler, sıralama.
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

            _lvLastBackups.BeginUpdate();
            _lvLastBackups.Items.Clear();
            _lvLastBackups.Groups.Clear();

            var groups = new Dictionary<string, ListViewGroup>(StringComparer.OrdinalIgnoreCase);

            foreach (var result in history)
            {
                string planName = result.PlanName ?? "—";

                if (!groups.TryGetValue(planName, out ListViewGroup? group))
                {
                    group = new ListViewGroup
                    {
                        Name = planName,
                        Header = planName,
                        CollapsedState = ListViewGroupCollapsedState.Expanded
                    };
                    groups[planName] = group;
                    _lvLastBackups.Groups.Add(group);
                }

                var item = new ListViewItem(result.StartedAt.ToString("yyyy-MM-dd HH:mm"))
                {
                    Group = group,
                    Tag = result
                };
                item.SubItems.Add(planName);
                item.SubItems.Add(result.DatabaseName ?? "—");
                item.SubItems.Add(GetBackupTypeName(result.BackupType));
                item.SubItems.Add(GetStatusName(result.Status));
                item.SubItems.Add(FormatFileSize(result.CompressedSizeBytes > 0
                    ? result.CompressedSizeBytes
                    : result.FileSizeBytes));

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

            // Grup başlıklarına yedekleme sayısını ekle
            foreach (ListViewGroup grp in _lvLastBackups.Groups)
            {
                grp.Header = $"{grp.Name}  —  {grp.Items.Count} " + Res.Get("Dashboard_GroupBackupCount");
            }

            _lvLastBackups.ListViewItemSorter = new LastBackupsItemComparer(_lvSortColumn, _lvSortAscending);
            _lvLastBackups.EndUpdate();

            // Groups.Clear() sonrası ShowGroups hâlâ true ise .NET setter'ı tekrar true atamayı
            // "değişiklik yok" diye yutarak LVM_ENABLEGROUPVIEW(TRUE) göndermez.
            // Force-toggle + P/Invoke ile native grup görünümünü her çağrıda garanti et.
            if (_lvLastBackups.Groups.Count > 0)
            {
                _lvLastBackups.ShowGroups = false;
                _lvLastBackups.ShowGroups = true;
                _headerPainter?.EnableGroupView();
            }
            else
            {
                _lvLastBackups.ShowGroups = false;
            }

            AutoResizeListViewColumns(_lvLastBackups);
        }

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

        private void OnLastBackupsColumnClick(object sender, ColumnClickEventArgs e)
        {
            if (_lvSortColumn == e.Column)
                _lvSortAscending = !_lvSortAscending;
            else
            {
                _lvSortColumn = e.Column;
                _lvSortAscending = true;
            }

            _lvLastBackups.ListViewItemSorter = new LastBackupsItemComparer(_lvSortColumn, _lvSortAscending);
            _headerPainter?.SetSortState(_lvSortColumn, _lvSortAscending);
            _lvLastBackups.Invalidate();
        }

        private static void AutoResizeListViewColumns(ListView lv)
        {
            for (int i = 0; i < lv.Columns.Count; i++)
            {
                int maxWidth = TextRenderer.MeasureText(lv.Columns[i].Text, Theme.ModernTheme.FontCaptionBold).Width + 28;
                foreach (ListViewItem item in lv.Items)
                {
                    if (i < item.SubItems.Count)
                    {
                        int w = TextRenderer.MeasureText(item.SubItems[i].Text, Theme.ModernTheme.FontBody).Width + 20;
                        if (w > maxWidth) maxWidth = w;
                    }
                }
                lv.Columns[i].Width = maxWidth;
            }
        }

        private sealed class LastBackupsItemComparer : System.Collections.IComparer
        {
            private readonly int _col;
            private readonly bool _asc;

            public LastBackupsItemComparer(int column, bool ascending)
            {
                _col = column;
                _asc = ascending;
            }

            public int Compare(object x, object y)
            {
                var ix = (ListViewItem)x;
                var iy = (ListViewItem)y;
                var rx = ix.Tag as BackupResult;
                var ry = iy.Tag as BackupResult;
                int result = CompareItems(ix, iy, rx, ry);
                return _asc ? result : -result;
            }

            private int CompareItems(ListViewItem ix, ListViewItem iy, BackupResult rx, BackupResult ry)
            {
                switch (_col)
                {
                    case 0: // Tarih
                        if (rx != null && ry != null)
                            return DateTime.Compare(rx.StartedAt, ry.StartedAt);
                        break;
                    case 4: // Sonuç (enum sırası: Success < PartialSuccess < Failed < Cancelled)
                        if (rx != null && ry != null)
                            return rx.Status.CompareTo(ry.Status);
                        break;
                    case 5: // Boyut (bayt cinsinden)
                        if (rx != null && ry != null)
                        {
                            long bx = rx.CompressedSizeBytes > 0 ? rx.CompressedSizeBytes : rx.FileSizeBytes;
                            long by = ry.CompressedSizeBytes > 0 ? ry.CompressedSizeBytes : ry.FileSizeBytes;
                            return bx.CompareTo(by);
                        }
                        break;
                }
                string tx = _col < ix.SubItems.Count ? ix.SubItems[_col].Text : string.Empty;
                string ty = _col < iy.SubItems.Count ? iy.SubItems[_col].Text : string.Empty;
                return string.Compare(tx, ty, StringComparison.CurrentCultureIgnoreCase);
            }
        }
    }
}
