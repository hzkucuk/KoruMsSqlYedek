using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using KoruMsSqlYedek.Core.Helpers;
using KoruMsSqlYedek.Core.Models;
using KoruMsSqlYedek.Win.Forms;
using KoruMsSqlYedek.Win.Helpers;

namespace KoruMsSqlYedek.Win
{
    // TAB 1: Planlar — Grid, CRUD, filtreleme, sıralama, şifre koruması.
    public partial class MainWindow
    {
        private void RefreshPlanList()
        {
            try
            {
                var plans = _planManager.GetAllPlans();
                _allPlanRows = new List<PlanRowData>(plans.Count);

                foreach (var plan in plans)
                {
                    var dbList = plan.Databases != null && plan.Databases.Count > 0
                        ? string.Join(", ", plan.Databases)
                        : "—";

                    var strategy = GetStrategyDisplayName(plan.Strategy?.Type ?? BackupStrategyType.Full);
                    var schedule = CronDisplayHelper.ToReadableText(plan.Strategy?.FullSchedule);
                    var cloudCount = plan.CloudTargets?.Count(t => t.IsEnabled) ?? 0;
                    string storageLabel = cloudCount > 0
                        ? $"\u2601 Bulut ({cloudCount})"
                        : "\U0001f4be Yerel";

                    string statusText = Res.Get("PlanStatus_Ready");
                    Color statusColor = Theme.ModernTheme.TextSecondary;
                    DateTime? lastRunAt = null;
                    bool lastBackupFailed = false;

                    try
                    {
                        var lastResult = _historyManager.GetHistoryByPlan(plan.PlanId, 1).FirstOrDefault();
                        if (lastResult != null)
                        {
                            lastRunAt = lastResult.StartedAt;
                            string icon;
                            switch (lastResult.Status)
                            {
                                case BackupResultStatus.Success:
                                    icon = "✓";
                                    statusColor = Theme.ModernTheme.StatusSuccess;
                                    break;
                                case BackupResultStatus.PartialSuccess:
                                    icon = "⚠";
                                    statusColor = Theme.ModernTheme.StatusWarning;
                                    break;
                                case BackupResultStatus.Failed:
                                    icon = "✕";
                                    statusColor = Theme.ModernTheme.StatusError;
                                    lastBackupFailed = true;
                                    break;
                                case BackupResultStatus.Cancelled:
                                    icon = "■";
                                    statusColor = Color.Gray;
                                    break;
                                default:
                                    icon = "";
                                    break;
                            }
                            statusText = lastResult.StartedAt.ToString("dd.MM.yyyy HH:mm") + " " + icon;
                        }
                    }
                    catch { /* history okunamazsa varsayılan "Hazır" kalır */ }

                    _allPlanRows.Add(new PlanRowData
                    {
                        Plan = plan,
                        DbList = dbList,
                        Strategy = strategy,
                        Schedule = schedule,
                        Storage = storageLabel,
                        StatusText = statusText,
                        StatusColor = statusColor,
                        LastRunAt = lastRunAt,
                        LastBackupFailed = lastBackupFailed
                    });
                }

                ApplyPlanFilter();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Plan listesi yüklenirken hata oluştu.");
                Theme.ModernMessageBox.Show(Res.Format("PlanList_LoadError", ex.Message),
                    Res.Get("Error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private string GetStrategyDisplayName(BackupStrategyType type)
        {
            switch (type)
            {
                case BackupStrategyType.Full: return Res.Get("PlanList_StratFull");
                case BackupStrategyType.FullPlusDifferential: return Res.Get("PlanList_StratFullDiff");
                case BackupStrategyType.FullPlusDifferentialPlusIncremental: return Res.Get("PlanList_StratFullDiffInc");
                default: return type.ToString();
            }
        }

        private void ApplyPlanFilter()
        {
            string search = _tstSearch?.Text?.Trim() ?? string.Empty;

            IEnumerable<PlanRowData> rows = _allPlanRows;

            if (!string.IsNullOrEmpty(search))
            {
                rows = rows.Where(r =>
                    (r.Plan.PlanName ?? string.Empty).IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0 ||
                    r.DbList.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0 ||
                    r.Strategy.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0 ||
                    r.Storage.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0);
            }

            if (_planSortColumn >= 0)
            {
                Func<PlanRowData, IComparable> key;
                switch (_planSortColumn)
                {
                    case 0:  key = r => (IComparable)(r.Plan.IsEnabled ? 0 : 1); break;
                    case 1:  key = r => (IComparable)(r.Plan.PlanName ?? string.Empty); break;
                    case 2:  key = r => (IComparable)r.Strategy; break;
                    case 3:  key = r => (IComparable)r.DbList; break;
                    case 4:  key = r => (IComparable)r.Schedule; break;
                    case 5:  key = r => (IComparable)r.Storage; break;
                    case 6:  key = r => (IComparable)r.Plan.CreatedAt; break;
                    case 7:  key = r => (IComparable)(r.LastRunAt ?? DateTime.MinValue); break;
                    default: key = null; break;
                }

                if (key != null)
                    rows = _planSortAscending ? rows.OrderBy(key) : rows.OrderByDescending(key);
            }

            var sorted = rows.ToList();
            _dgvPlans.SuspendLayout();
            _dgvPlans.Rows.Clear();

            foreach (var row in sorted)
            {
                var plan = row.Plan;
                var rowIndex = _dgvPlans.Rows.Add(
                    plan.IsEnabled,
                    plan.PlanName ?? Res.Get("PlanList_Unnamed"),
                    row.Strategy,
                    row.DbList,
                    row.Schedule,
                    row.Storage,
                    plan.CreatedAt.ToString("dd.MM.yyyy"),
                    row.StatusText,
                    _planProgress.TryGetValue(plan.PlanId, out int pct) ? pct : 0,
                    _nextFireTimes.TryGetValue(plan.PlanId, out string nft) ? nft : "—");

                _dgvPlans.Rows[rowIndex].Tag = plan;
                _dgvPlans.Rows[rowIndex].Cells[_colStatus.Index].Style.ForeColor = row.StatusColor;

                if (row.LastBackupFailed)
                {
                    _dgvPlans.Rows[rowIndex].DefaultCellStyle.BackColor = Theme.ModernTheme.GridErrorRow;
                    _dgvPlans.Rows[rowIndex].DefaultCellStyle.ForeColor = Theme.ModernTheme.TextPrimary;
                }
            }

            _dgvPlans.ResumeLayout(false);

            // Sekme geçişi sonrası seçili görevi geri yükle
            if (!string.IsNullOrEmpty(_viewingPlanId))
            {
                for (int i = 0; i < _dgvPlans.Rows.Count; i++)
                {
                    if (_dgvPlans.Rows[i].Tag is BackupPlan p && p.PlanId == _viewingPlanId)
                    {
                        _dgvPlans.ClearSelection();
                        _dgvPlans.Rows[i].Selected = true;
                        _dgvPlans.FirstDisplayedScrollingRowIndex = i;
                        break;
                    }
                }
            }

            _tslPlanCount.Text = search.Length > 0
                ? $"{sorted.Count} / {_allPlanRows.Count} görev"
                : Res.Format("PlanList_TotalFormat", _allPlanRows.Count);

            UpdateBackupButtonStates();
        }

        private void OnPlanGridColumnHeaderClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.ColumnIndex < 0 || e.ColumnIndex == _colProgress.Index)
                return;

            if (_planSortColumn == e.ColumnIndex)
                _planSortAscending = !_planSortAscending;
            else
            {
                _planSortColumn = e.ColumnIndex;
                _planSortAscending = true;
            }

            foreach (DataGridViewColumn col in _dgvPlans.Columns)
                col.HeaderCell.SortGlyphDirection = SortOrder.None;

            _dgvPlans.Columns[_planSortColumn].HeaderCell.SortGlyphDirection =
                _planSortAscending ? SortOrder.Ascending : SortOrder.Descending;

            ApplyPlanFilter();
        }

        private void OnPlanSearchTextChanged(object sender, EventArgs e)
        {
            ApplyPlanFilter();
        }

        private sealed class PlanRowData
        {
            public BackupPlan Plan;
            public string DbList;
            public string Strategy;
            public string Schedule;
            public string Storage;
            public string StatusText;
            public Color StatusColor;
            public DateTime? LastRunAt;
            public bool LastBackupFailed;
        }

        private bool CheckPlanPassword(BackupPlan plan = null)
        {
            bool hasMaster = _settings != null && _settings.IsPasswordProtected;
            bool hasPlanPw = plan != null && plan.HasPlanPassword;

            if (!hasMaster && !hasPlanPw)
                return true;

            string planHash = hasPlanPw ? plan.PasswordHash : null;
            string recoveryHash = hasPlanPw ? plan.RecoveryPasswordHash : null;

            using (var dlg = new PasswordDialog(_settings ?? new AppSettings(), _settingsManager, planHash, recoveryHash))
            {
                if (dlg.ShowDialog(this) != DialogResult.OK)
                    return false;

                if (dlg.PlanPasswordReset && plan != null)
                {
                    plan.PasswordHash = null;
                    plan.RecoveryPasswordHash = null;
                    _planManager.SavePlan(plan);
                }

                return true;
            }
        }

        private async void OnNewPlanClick(object sender, EventArgs e)
        {
            if (!CheckPlanPassword()) return;

            using (var form = new PlanEditForm(_planManager, _sqlBackupService, _settingsManager))
            {
                if (form.ShowDialog(this) == DialogResult.OK)
                {
                    RefreshPlanList();
                }
            }
        }

        private async void OnEditPlanClick(object sender, EventArgs e)
        {
            var plan = GetSelectedPlan();
            if (plan == null) return;

            if (!CheckPlanPassword(plan)) return;

            using (var form = new PlanEditForm(_planManager, _sqlBackupService, _settingsManager, plan))
            {
                if (form.ShowDialog(this) == DialogResult.OK)
                {
                    RefreshPlanList();
                }
            }
        }

        private async void OnDeletePlanClick(object sender, EventArgs e)
        {
            var plan = GetSelectedPlan();
            if (plan == null) return;

            if (!CheckPlanPassword(plan)) return;

            var result = Theme.ModernMessageBox.Show(
                Res.Format("PlanList_DeleteConfirm", plan.PlanName),
                Res.Get("PlanList_DeleteTitle"),
                MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2);

            if (result == DialogResult.Yes)
            {
                try
                {
                    _planManager.DeletePlan(plan.PlanId);
                    Log.Information("Plan silindi: {PlanName} ({PlanId})", plan.PlanName, plan.PlanId);
                    RefreshPlanList();
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Plan silinirken hata: {PlanId}", plan.PlanId);
                    Theme.ModernMessageBox.Show(Res.Format("PlanList_DeleteError", ex.Message),
                        Res.Get("Error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void OnExportPlanClick(object sender, EventArgs e)
        {
            var plan = GetSelectedPlan();
            if (plan == null) return;

            using (var sfd = new SaveFileDialog())
            {
                sfd.Title = Res.Get("PlanList_ExportDialogTitle");
                sfd.Filter = Res.Get("PlanList_ExportFilter");
                sfd.FileName = $"{plan.PlanName ?? "plan"}.json";

                if (sfd.ShowDialog(this) == DialogResult.OK)
                {
                    try
                    {
                        _planManager.ExportPlan(plan.PlanId, sfd.FileName);
                        Theme.ModernMessageBox.Show(Res.Get("PlanList_ExportSuccess"), Res.Get("Info"),
                            MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "Plan export hatası: {PlanId}", plan.PlanId);
                        Theme.ModernMessageBox.Show(Res.Format("PlanList_ExportError", ex.Message),
                            Res.Get("Error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private async void OnImportPlanClick(object sender, EventArgs e)
        {
            using (var ofd = new OpenFileDialog())
            {
                ofd.Title = Res.Get("PlanList_ImportDialogTitle");
                ofd.Filter = Res.Get("PlanList_ExportFilter");

                if (ofd.ShowDialog(this) == DialogResult.OK)
                {
                    try
                    {
                        var plan = _planManager.ImportPlan(ofd.FileName);
                        Log.Information("Plan içe aktarıldı: {PlanName} ({PlanId})", plan.PlanName, plan.PlanId);
                        RefreshPlanList();
                        Theme.ModernMessageBox.Show(Res.Format("PlanList_ImportSuccess", plan.PlanName), Res.Get("Info"),
                            MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "Plan import hatası: {FilePath}", ofd.FileName);
                        Theme.ModernMessageBox.Show(Res.Format("PlanList_ImportError", ex.Message),
                            Res.Get("Error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void OnRefreshPlansClick(object sender, EventArgs e)
        {
            RefreshPlanList();
        }

        private BackupPlan GetSelectedPlan()
        {
            var plan = GetSelectedPlanSilent();
            if (plan == null)
                Theme.ModernMessageBox.Show(Res.Get("ManualBackup_PleaseSelectPlan"), Res.Get("Warning"),
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return plan;
        }

        private BackupPlan GetSelectedPlanSilent()
        {
            if (_dgvPlans.SelectedRows.Count == 0)
                return null;
            return _dgvPlans.SelectedRows[0].Tag as BackupPlan;
        }

        private void OnContextMenuOpening(object sender, System.ComponentModel.CancelEventArgs e)
        {
            var plan = GetSelectedPlanSilent();
            bool hasPlan = plan != null;
            bool running = hasPlan && _runningPlanIds.Contains(plan.PlanId);

            _ctxBackupNow.Enabled = hasPlan && !running && _pipeClient.IsConnected;
            _ctxStopBackup.Enabled = running && _pipeClient.IsConnected;
            _ctxEditPlan.Enabled = hasPlan && !running;
            _ctxDeletePlan.Enabled = hasPlan && !running;
            _ctxExportPlan.Enabled = hasPlan;
            _ctxViewPlanLogs.Enabled = hasPlan;
        }

        private void OnCtxBackupNowClick(object sender, EventArgs e) => OnStartBackupClick(sender, e);

        private void OnCtxStopBackupClick(object sender, EventArgs e) => OnCancelBackupClick(sender, e);

        private void OnCtxViewPlanLogsClick(object sender, EventArgs e)
        {
            if (GetSelectedPlanSilent() == null) return;
            _tabControl.SelectedIndex = 2;
        }

        private void OnCtxRestoreClick(object sender, EventArgs e)
        {
            BackupPlan plan = GetSelectedPlanSilent();
            if (plan == null) return;

            using RestoreDialog dlg = new RestoreDialog(plan, _historyManager, _sqlBackupService, _compressionService);
            dlg.ShowDialog(this);
        }

        private void OnPlanGridDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            OnEditPlanClick(sender, e);
        }

        private void OnPlanGridSelectionChanged(object sender, EventArgs e)
        {
            var selected = GetSelectedPlanSilent();
            if (selected == null) return;

            // Seçilen planı aktif olarak takip et
            _viewingPlanId = selected.PlanId;

            // Seçilen planın progress durumunu göster
            RestoreProgressBarForPlan(selected.PlanId);

            UpdateBackupButtonStates();

            // Seçilen plana ait renkli log buffer'ını göster
            _txtBackupLog.Clear();
            if (_planLogs.TryGetValue(selected.PlanId, out var logs) && logs.Count > 0)
            {
                _txtBackupLog.SuspendLayout();
                foreach (var (text, color) in logs)
                {
                    _txtBackupLog.SelectionStart = _txtBackupLog.TextLength;
                    _txtBackupLog.SelectionLength = 0;
                    _txtBackupLog.SelectionColor = color;
                    _txtBackupLog.AppendText(text + Environment.NewLine);
                }
                _txtBackupLog.SelectionColor = Theme.ModernTheme.LogDefault;
                _txtBackupLog.SelectionStart = _txtBackupLog.TextLength;
                _txtBackupLog.ScrollToCaret();
                _txtBackupLog.ResumeLayout();
            }
        }

        /// <summary>
        /// Seçilen planın mevcut progress durumunu ana progress bar'a yansıtır.
        /// Çalışan plan varsa tracker'dan son değeri alır, yoksa sıfırlar.
        /// </summary>
        private void RestoreProgressBarForPlan(string planId)
        {
            if (_runningPlanIds.Contains(planId) && _planProgress.TryGetValue(planId, out int pct))
            {
                _progressBar.DisplayMode = Theme.ProgressBarDisplayMode.Percentage;
                _progressBar.ShowPercentage = true;
                _progressBar.Value = pct;
            }
            else if (!_runningPlanIds.Contains(planId))
            {
                _progressBar.ShowPercentage = false;
                _progressBar.DisplayMode = Theme.ProgressBarDisplayMode.Percentage;
                _progressBar.Value = 0;
            }
        }
    }
}
