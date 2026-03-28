using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using MikroSqlDbYedek.Core.Interfaces;
using MikroSqlDbYedek.Core.Models;
using MikroSqlDbYedek.Win.Helpers;
using Serilog;

namespace MikroSqlDbYedek.Win.Forms
{
    /// <summary>
    /// Manuel yedekleme diyalogu.
    /// Plan seçimi, veritabanı seçimi, yedek türü ve ilerleme çubuğu ile
    /// anında yedekleme işlemi başlatır.
    /// </summary>
    public partial class ManualBackupDialog : Form
    {
        private static readonly ILogger Log = Serilog.Log.ForContext<ManualBackupDialog>();

        private readonly IPlanManager _planManager;
        private readonly ISqlBackupService _sqlBackupService;
        private CancellationTokenSource _cts;
        private bool _isRunning;

        public ManualBackupDialog(IPlanManager planManager, ISqlBackupService sqlBackupService)
        {
            if (planManager == null) throw new ArgumentNullException(nameof(planManager));
            if (sqlBackupService == null) throw new ArgumentNullException(nameof(sqlBackupService));

            InitializeComponent();
            _planManager = planManager;
            _sqlBackupService = sqlBackupService;
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            LoadPlans();
            UpdateButtonStates();
        }

        #region Data Loading

        private void LoadPlans()
        {
            _cmbPlan.Items.Clear();
            _cmbPlan.Items.Add(Res.Get("ManualBackup_SelectPlanDefault"));

            try
            {
                var plans = _planManager.GetAllPlans();
                foreach (var plan in plans.Where(p => p.IsEnabled))
                {
                    _cmbPlan.Items.Add(plan);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Planlar yüklenemedi.");
            }

            _cmbPlan.SelectedIndex = 0;
            _cmbPlan.DisplayMember = "PlanName";
        }

        private void OnPlanSelectedChanged(object sender, EventArgs e)
        {
            _clbDatabases.Items.Clear();

            var plan = _cmbPlan.SelectedItem as BackupPlan;
            if (plan == null)
            {
                UpdateButtonStates();
                return;
            }

            foreach (string db in plan.Databases)
            {
                _clbDatabases.Items.Add(db, true);
            }

            UpdateButtonStates();
        }

        #endregion

        #region Backup Execution

        private async void OnStartClick(object sender, EventArgs e)
        {
            var plan = _cmbPlan.SelectedItem as BackupPlan;
            if (plan == null)
            {
                MessageBox.Show(Res.Get("ManualBackup_PleaseSelectPlan"), Res.Get("Warning"),
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var selectedDatabases = _clbDatabases.CheckedItems.Cast<string>().ToList();
            if (selectedDatabases.Count == 0)
            {
                MessageBox.Show(Res.Get("ManualBackup_PleaseSelectDb"), Res.Get("Warning"),
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            SqlBackupType backupType;
            switch (_cmbBackupType.SelectedIndex)
            {
                case 1: backupType = SqlBackupType.Differential; break;
                case 2: backupType = SqlBackupType.Incremental; break;
                default: backupType = SqlBackupType.Full; break;
            }

            _isRunning = true;
            _cts = new CancellationTokenSource();
            UpdateButtonStates();
            _progressBar.Value = 0;
            _progressBar.Maximum = selectedDatabases.Count * 100;
            _lblStatus.Text = Res.Get("ManualBackup_Starting");
            _txtLog.Clear();

            int successCount = 0;
            int failCount = 0;
            int totalProgress = 0;

            try
            {
                for (int i = 0; i < selectedDatabases.Count; i++)
                {
                    _cts.Token.ThrowIfCancellationRequested();

                    string dbName = selectedDatabases[i];
                    _lblStatus.Text = Res.Format("ManualBackup_ProgressFormat", i + 1, selectedDatabases.Count, dbName);
                    AppendLog(Res.Format("ManualBackup_BackingUpFormat", dbName, backupType));

                    var progress = new Progress<int>(pct =>
                    {
                        int current = totalProgress + pct;
                        if (current <= _progressBar.Maximum)
                            _progressBar.Value = current;
                    });

                    try
                    {
                        var result = await _sqlBackupService.BackupDatabaseAsync(
                            plan.SqlConnection, dbName, backupType, plan.LocalPath,
                            progress, _cts.Token);

                        if (result.Status == BackupResultStatus.Success)
                        {
                            successCount++;
                            string sizeMb = (result.FileSizeBytes / 1048576.0).ToString("F1");
                            AppendLog(Res.Format("ManualBackup_SuccessFormat", sizeMb, result.BackupFilePath));
                        }
                        else
                        {
                            failCount++;
                            AppendLog(Res.Format("ManualBackup_FailedFormat", result.ErrorMessage));
                        }
                    }
                    catch (Exception ex)
                    {
                        failCount++;
                        AppendLog(Res.Format("ManualBackup_ErrorFormat", ex.Message));
                        Log.Error(ex, "Manuel yedekleme hatası: {Database}", dbName);
                    }

                    totalProgress += 100;
                    _progressBar.Value = Math.Min(totalProgress, _progressBar.Maximum);
                }

                _lblStatus.Text = Res.Format("ManualBackup_CompletedFormat", successCount, failCount);
                _lblStatus.ForeColor = failCount == 0 ? Color.Green : Color.OrangeRed;
                AppendLog(Res.Format("ManualBackup_ResultFormat", successCount, failCount));
            }
            catch (OperationCanceledException)
            {
                _lblStatus.Text = Res.Get("ManualBackup_Cancelled");
                _lblStatus.ForeColor = Color.Gray;
                AppendLog(Res.Get("ManualBackup_CancelledLog"));
            }
            catch (Exception ex)
            {
                _lblStatus.Text = Res.Get("ManualBackup_UnexpectedError");
                _lblStatus.ForeColor = Color.Red;
                AppendLog(Res.Format("ManualBackup_UnexpectedErrorLog", ex.Message));
                Log.Error(ex, "Manuel yedekleme genel hatası.");
            }
            finally
            {
                _isRunning = false;
                _cts?.Dispose();
                _cts = null;
                UpdateButtonStates();
            }
        }

        private void OnCancelBackupClick(object sender, EventArgs e)
        {
            if (_cts != null && !_cts.IsCancellationRequested)
            {
                _cts.Cancel();
                _lblStatus.Text = Res.Get("ManualBackup_Cancelling");
                _btnCancelBackup.Enabled = false;
            }
        }

        private void OnCloseClick(object sender, EventArgs e)
        {
            if (_isRunning)
            {
                var dr = MessageBox.Show(
                    Res.Get("ManualBackup_CloseWhileRunning"),
                    Res.Get("Warning"), MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

                if (dr == DialogResult.Yes)
                {
                    _cts?.Cancel();
                }
                else
                {
                    return;
                }
            }

            Close();
        }

        #endregion

        #region Helpers

        private void UpdateButtonStates()
        {
            bool hasPlan = _cmbPlan.SelectedItem is BackupPlan;
            bool hasDb = _clbDatabases.CheckedItems.Count > 0;

            _btnStart.Enabled = !_isRunning && hasPlan && hasDb;
            _btnCancelBackup.Enabled = _isRunning;
            _btnClose.Enabled = !_isRunning;
            _cmbPlan.Enabled = !_isRunning;
            _cmbBackupType.Enabled = !_isRunning;
            _clbDatabases.Enabled = !_isRunning;
        }

        private void AppendLog(string text)
        {
            _txtLog.AppendText(text + Environment.NewLine);
        }

        private void OnDatabaseItemCheck(object sender, ItemCheckEventArgs e)
        {
            // ItemCheck fires before the check state changes, so defer
            BeginInvoke(new Action(UpdateButtonStates));
        }

        #endregion
    }
}
