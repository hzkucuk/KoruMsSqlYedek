using System;
using System.Threading.Tasks;
using System.Windows.Forms;
using KoruMsSqlYedek.Core.IPC;
using KoruMsSqlYedek.Core.Models;
using KoruMsSqlYedek.Win.Helpers;

namespace KoruMsSqlYedek.Win
{
    // Manuel yedekleme başlat/iptal, servis IPC bağlantısı, sonraki çalışma zamanları.
    public partial class MainWindow
    {
        private async void OnStartBackupClick(object sender, EventArgs e)
        {
            var plan = GetSelectedPlan();
            if (plan == null) return;

            if (!_pipeClient.IsConnected)
            {
                Theme.ModernMessageBox.Show(Res.Get("Backup_ServiceNotConnected"), Res.Get("Warning"),
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (_runningPlanIds.Contains(plan.PlanId))
                return;

            _runningPlanIds.Add(plan.PlanId);
            _viewingPlanId = plan.PlanId;
            UpdateBackupButtonStates();

            // Bu plan için önceki log buffer'ını temizle
            _planLogs.Remove(plan.PlanId);
            _planProgress.Remove(plan.PlanId);
            _txtBackupLog.Clear();
            AppendBackupLog(plan.PlanId, string.Format("[{0}] {1}", plan.PlanName, Res.Get("ManualBackup_Starting")), Theme.ModernTheme.LogStarted);

            try
            {
                string backupType = _cmbBackupType.SelectedIndex switch
                {
                    1 => "Differential",
                    2 => "Incremental",
                    _ => "Full"
                };
                await _pipeClient.SendManualBackupCommandAsync(plan.PlanId, backupType);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Manuel yedekleme komutu gönderilemedi: {PlanId}", plan.PlanId);
                AppendBackupLog(plan.PlanId, Res.Format("Backup_SendError", ex.Message), Theme.ModernTheme.LogError);
                _runningPlanIds.Remove(plan.PlanId);
                UpdateBackupButtonStates();
            }
        }

        private async void OnCancelBackupClick(object sender, EventArgs e)
        {
            var plan = GetSelectedPlanSilent();
            if (plan == null || !_runningPlanIds.Contains(plan.PlanId))
                return;

            string targetPlanId = plan.PlanId;

            try
            {
                await _pipeClient.SendCancelCommandAsync(targetPlanId);
                AppendBackupLog(targetPlanId, Res.Get("ManualBackup_Cancelling"), Theme.ModernTheme.LogWarning);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "İptal komutu gönderilemedi: {PlanId}", targetPlanId);
            }
        }

        private void UpdateBackupButtonStates()
        {
            var plan = GetSelectedPlanSilent();
            bool hasPlan = plan != null;
            bool connected = _pipeClient.IsConnected;
            bool selectedRunning = hasPlan && _runningPlanIds.Contains(plan.PlanId);
            bool anyRunning = _runningPlanIds.Count > 0;

            _btnStart.Enabled = hasPlan && !selectedRunning && connected;
            _btnCancelBackup.Enabled = selectedRunning;

            if (!connected)
                _lblBackupStatus.Text = Res.Get("Backup_ServiceDisconnected");
            else if (selectedRunning)
                _lblBackupStatus.Text = Res.Format("Backup_ReadyForPlan", plan.PlanName);
            else if (anyRunning)
                _lblBackupStatus.Text = Res.Format("Backup_TasksRunning", _runningPlanIds.Count);
            else if (hasPlan)
                _lblBackupStatus.Text = Res.Format("Backup_ReadyForPlan", plan.PlanName);
            else
                _lblBackupStatus.Text = Res.Get("ManualBackup_PleaseSelectPlan");
        }

        private void OnPipeConnectionChanged(object sender, bool connected)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => OnPipeConnectionChanged(sender, connected)));
                return;
            }

            UpdateStatusBarConnection(connected);
            UpdateBackupButtonStates();
        }

        private void UpdateStatusBarConnection(bool connected)
        {
            if (connected)
            {
                _tslStatus.Text      = Res.Get("StatusBar_ServiceConnected");
                _tslStatus.ForeColor = Theme.ModernTheme.StatusSuccess;
            }
            else
            {
                _tslStatus.Text      = Res.Get("StatusBar_ServiceDisconnected");
                _tslStatus.ForeColor = Theme.ModernTheme.StatusError;
            }
        }

        private void OnServiceStatusReceived(object sender, ServiceStatusMessage e)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() => OnServiceStatusReceived(sender, e)));
                return;
            }

            if (e.NextFireTimes == null || e.NextFireTimes.Count == 0) return;

            foreach (var kv in e.NextFireTimes)
            {
                if (kv.Value == null)
                {
                    _nextFireTimes.Remove(kv.Key);
                    continue;
                }

                string displayText;
                if (DateTimeOffset.TryParse(kv.Value, null,
                    System.Globalization.DateTimeStyles.RoundtripKind, out DateTimeOffset dto))
                    displayText = dto.ToLocalTime().ToString("dd.MM.yyyy HH:mm");
                else
                    displayText = kv.Value;

                _nextFireTimes[kv.Key] = displayText;
            }

            foreach (DataGridViewRow row in _dgvPlans.Rows)
            {
                var plan = row.Tag as BackupPlan;
                if (plan == null) continue;

                if (_nextFireTimes.TryGetValue(plan.PlanId, out string displayTime))
                    row.Cells[_colNextRun.Index].Value = displayTime;
                else
                    row.Cells[_colNextRun.Index].Value = "—";
            }
        }

        private async void RequestNextFireTimesAsync()
        {
            try
            {
                if (_pipeClient.IsConnected)
                    await _pipeClient.RequestStatusAsync();
            }
            catch (Exception ex)
            {
                Log.Debug(ex, "Sonraki çalışma zamanları isteği gönderilemedi.");
            }
        }
    }
}
