using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using KoruMsSqlYedek.Core.Helpers;
using KoruMsSqlYedek.Core.Interfaces;
using KoruMsSqlYedek.Core.Models;
using KoruMsSqlYedek.Win.Helpers;
using Serilog;

namespace KoruMsSqlYedek.Win.Forms
{
    /// <summary>
    /// Yedek geçmişinden seçim yaparak SQL Server veritabanı geri yükleme diyaloğu.
    /// ISqlBackupService üzerinden RESTORE VERIFYONLY ve RESTORE DATABASE komutlarını çalıştırır.
    /// </summary>
    internal partial class RestoreDialog : Theme.ModernFormBase
    {
        private static readonly ILogger Log = Serilog.Log.ForContext<RestoreDialog>();

        private readonly BackupPlan _plan;
        private readonly IBackupHistoryManager _historyManager;
        private readonly ISqlBackupService _sqlService;
        private readonly ICompressionService _compressionService;

        private List<BackupResult> _history;
        private BackupResult _selectedResult;
        private CancellationTokenSource _cts;
        private bool _isBusy;

        private const double BytesPerMb = 1_048_576.0;

        public RestoreDialog(
            BackupPlan plan,
            IBackupHistoryManager historyManager,
            ISqlBackupService sqlService,
            ICompressionService compressionService)
        {
            ArgumentNullException.ThrowIfNull(plan);
            ArgumentNullException.ThrowIfNull(historyManager);
            ArgumentNullException.ThrowIfNull(sqlService);
            ArgumentNullException.ThrowIfNull(compressionService);

            _plan               = plan;
            _historyManager     = historyManager;
            _sqlService         = sqlService;
            _compressionService = compressionService;

            InitializeComponent();

            _lblHeader.Text = $"{Res.Get("Restore_Title")} — {plan.PlanName}";
            Text            = $"{Res.Get("Restore_Title")} — {plan.PlanName}";

            LocalizeColumns();
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            // _btnClose konumunu sağa yasla
            _btnClose.Left = _pnlButtons.Width - _btnClose.Width;
            _pnlButtons.Resize += (s, _) =>
                _btnClose.Left = _pnlButtons.Width - _btnClose.Width;

            LoadHistory();
        }

        // ── Lokalizasyon ─────────────────────────────────────────────────────

        private void LocalizeColumns()
        {
            _colDate.HeaderText     = Res.Get("Restore_ColDate");
            _colDatabase.HeaderText = Res.Get("Restore_ColDatabase");
            _colType.HeaderText     = Res.Get("Restore_ColType");
            _colFile.HeaderText     = Res.Get("Restore_ColFile");
            _colSize.HeaderText     = Res.Get("Restore_ColSize");
            _colStatus.HeaderText   = Res.Get("Restore_ColStatus");
            _lblTargetDb.Text       = Res.Get("Restore_LblTargetDb");
            _chkPreBackup.Text      = Res.Get("Restore_ChkPreBackup");
            _btnVerify.Text         = Res.Get("Restore_BtnVerify");
            _btnRestore.Text        = Res.Get("Restore_BtnRestore");
            _btnClose.Text          = Res.Get("Restore_BtnClose");
        }

        // ── Tarihçe yükleme ──────────────────────────────────────────────────

        private void LoadHistory()
        {
            _history = _historyManager.GetHistoryByPlan(_plan.PlanId, maxRecords: 100)
                .Where(r => r.Status == BackupResultStatus.Success)
                .OrderByDescending(r => r.StartedAt)
                .ToList();

            _dgvHistory.Rows.Clear();

            if (_history.Count == 0)
            {
                AppendLog(Res.Get("Restore_NoHistory"));
                return;
            }

            foreach (BackupResult r in _history)
            {
                string filePath  = r.CompressedFilePath ?? r.BackupFilePath ?? string.Empty;
                string fileName  = string.IsNullOrEmpty(filePath) ? "-" : Path.GetFileName(filePath);
                long   sizeBytes = r.CompressedSizeBytes > 0 ? r.CompressedSizeBytes : r.FileSizeBytes;
                string sizeStr   = sizeBytes > 0 ? $"{sizeBytes / BytesPerMb:F1} MB" : "-";

                int rowIdx = _dgvHistory.Rows.Add(
                    r.StartedAt.ToString("dd.MM.yyyy HH:mm"),
                    r.DatabaseName ?? "-",
                    r.BackupType.ToString(),
                    fileName,
                    sizeStr,
                    r.Status.ToString());

                _dgvHistory.Rows[rowIdx].Tag = r;
            }
        }

        // ── Grid seçim ───────────────────────────────────────────────────────

        private void OnHistorySelectionChanged(object sender, EventArgs e)
        {
            _selectedResult = null;
            _btnVerify.Enabled  = false;
            _btnRestore.Enabled = false;

            if (_dgvHistory.SelectedRows.Count == 0)
                return;

            var result = _dgvHistory.SelectedRows[0].Tag as BackupResult;
            if (result == null)
                return;

            _selectedResult = result;

            string filePath = result.CompressedFilePath ?? result.BackupFilePath;

            if (!string.IsNullOrEmpty(filePath) && File.Exists(filePath))
            {
                _btnVerify.Enabled  = true;
                _btnRestore.Enabled = true;

                if (string.IsNullOrWhiteSpace(_txtTargetDb.Text))
                    _txtTargetDb.Text = result.DatabaseName;
            }
            else
            {
                AppendLog(Res.Format("Restore_NoFile", filePath ?? "-"));
            }
        }

        // ── Doğrula ──────────────────────────────────────────────────────────

        private async void OnVerifyClick(object sender, EventArgs e)
        {
            if (_selectedResult == null)
            {
                AppendLog(Res.Get("Restore_NoSelection"));
                return;
            }

            string filePath = _selectedResult.CompressedFilePath ?? _selectedResult.BackupFilePath;
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
            {
                AppendLog(Res.Format("Restore_NoFile", filePath ?? "-"));
                return;
            }

            SetBusy(true);
            AppendLog($"[VERIFY] {Path.GetFileName(filePath)}");

            try
            {
                _cts = new CancellationTokenSource(TimeSpan.FromMinutes(5));
                bool ok = await _sqlService.VerifyBackupAsync(
                    _plan.SqlConnection,
                    filePath,
                    _cts.Token);

                AppendLog(ok ? Res.Get("Restore_VerifyOk") : Res.Get("Restore_VerifyFail"));
            }
            catch (OperationCanceledException)
            {
                AppendLog(Res.Get("Restore_VerifyTimeout"));
            }
            catch (Exception ex)
            {
                AppendLog(Res.Format("Restore_Error", ex.Message));
                Log.Error(ex, "Restore doğrulama hatası: {File}", filePath);
            }
            finally
            {
                SetBusy(false);
            }
        }

        // ── Geri Yükle ───────────────────────────────────────────────────────

        private async void OnRestoreClick(object sender, EventArgs e)
        {
            if (_selectedResult == null)
            {
                AppendLog(Res.Get("Restore_NoSelection"));
                return;
            }

            string targetDb = _txtTargetDb.Text.Trim();
            if (string.IsNullOrWhiteSpace(targetDb))
            {
                AppendLog(Res.Get("Restore_TargetDbEmpty"));
                _txtTargetDb.Focus();
                return;
            }

            string filePath = _selectedResult.CompressedFilePath ?? _selectedResult.BackupFilePath;
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
            {
                AppendLog(Res.Format("Restore_NoFile", filePath ?? "-"));
                return;
            }

            var confirm = Theme.ModernMessageBox.Show(
                Res.Format("Restore_Confirm", targetDb),
                Res.Get("Restore_ConfirmTitle"),
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning,
                MessageBoxDefaultButton.Button2);

            if (confirm != DialogResult.Yes)
                return;

            SetBusy(true);
            _progressBar.Value = 0;
            AppendLog($"[RESTORE] {targetDb} ← {Path.GetFileName(filePath)}");

            string tempExtractDir = null;

            try
            {
                _cts = new CancellationTokenSource(TimeSpan.FromHours(2));
                var progress = new Progress<int>(pct =>
                {
                    if (InvokeRequired)
                        Invoke(new Action(() => _progressBar.Value = Math.Min(pct, 100)));
                    else
                        _progressBar.Value = Math.Min(pct, 100);
                });

                string restoreFilePath = filePath;

                // .7z arşiv ise önce çıkar
                if (filePath.EndsWith(".7z", StringComparison.OrdinalIgnoreCase))
                {
                    AppendLog(Res.Get("Restore_Extracting"));
                    tempExtractDir = Path.Combine(Path.GetTempPath(), "KoruRestore_" + Guid.NewGuid().ToString("N"));
                    Directory.CreateDirectory(tempExtractDir);

                    string password = !string.IsNullOrEmpty(_plan.Compression?.ArchivePassword)
                        ? PasswordProtector.Unprotect(_plan.Compression.ArchivePassword)
                        : null;

                    await _compressionService.ExtractAsync(
                        filePath, tempExtractDir, password, progress, _cts.Token);

                    string bakFile = Directory.EnumerateFiles(tempExtractDir, "*.bak", SearchOption.AllDirectories)
                        .FirstOrDefault();

                    if (string.IsNullOrEmpty(bakFile))
                    {
                        AppendLog(Res.Get("Restore_NoBakInArchive"));
                        return;
                    }

                    restoreFilePath = bakFile;
                    _progressBar.Value = 0;
                }

                bool ok = await _sqlService.RestoreDatabaseAsync(
                    _plan.SqlConnection,
                    targetDb,
                    restoreFilePath,
                    _chkPreBackup.Checked,
                    progress,
                    _cts.Token);

                _progressBar.Value = ok ? 100 : 0;
                AppendLog(ok ? Res.Get("Restore_Success") : Res.Get("Restore_Failed"));

                if (ok)
                    Log.Information("Restore tamamlandı: {TargetDb} ← {File}", targetDb, filePath);
            }
            catch (OperationCanceledException)
            {
                AppendLog(Res.Get("Restore_Cancelled"));
            }
            catch (Exception ex)
            {
                AppendLog(Res.Format("Restore_Error", ex.Message));
                Log.Error(ex, "Restore hatası: {TargetDb} ← {File}", targetDb, filePath);
            }
            finally
            {
                CleanupTempDirectory(tempExtractDir);
                SetBusy(false);
            }
        }

        // ── Kapat ────────────────────────────────────────────────────────────

        private void OnCloseClick(object sender, EventArgs e)
        {
            if (_isBusy)
            {
                var result = Theme.ModernMessageBox.Show(
                    Res.Get("Restore_CancelConfirm"),
                    Res.Get("Restore_CancelConfirmTitle"),
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning,
                    MessageBoxDefaultButton.Button2);

                if (result != DialogResult.Yes)
                    return;

                _cts?.Cancel();
                return;
            }

            Close();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (_isBusy)
            {
                _cts?.Cancel();
            }

            base.OnFormClosing(e);
        }

        // ── Yardımcılar ──────────────────────────────────────────────────────

        private void SetBusy(bool busy)
        {
            _isBusy = busy;
            _btnVerify.Enabled  = !busy;
            _btnRestore.Enabled = !busy;
            _btnClose.Text = busy ? Res.Get("Restore_BtnCancel") : Res.Get("Restore_BtnClose");
            Cursor = busy ? Cursors.WaitCursor : Cursors.Default;
        }

        private void AppendLog(string message)
        {
            string line = $"[{DateTime.Now:HH:mm:ss}] {message}";
            _txtLog.AppendText(line + Environment.NewLine);
        }

        private static void CleanupTempDirectory(string tempDir)
        {
            if (string.IsNullOrEmpty(tempDir) || !Directory.Exists(tempDir))
                return;

            try
            {
                Directory.Delete(tempDir, recursive: true);
            }
            catch (Exception ex)
            {
                Serilog.Log.Warning(ex, "Geçici dizin silinemedi: {Dir}", tempDir);
            }
        }
    }
}
