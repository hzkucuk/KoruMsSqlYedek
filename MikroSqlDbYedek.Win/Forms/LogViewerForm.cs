using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using MikroSqlDbYedek.Win.Helpers;
using Serilog;

namespace MikroSqlDbYedek.Win.Forms
{
    /// <summary>
    /// Serilog rolling log dosyalarını görüntüleyen form.
    /// Seviye filtreleme, metin arama, dışa aktarma ve otomatik yenileme desteği.
    /// </summary>
    public partial class LogViewerForm : Form
    {
        private static readonly ILogger Log = Serilog.Log.ForContext<LogViewerForm>();

        private readonly string _logDirectory;
        private readonly Timer _autoRefreshTimer;
        private List<LogEntry> _allEntries = new List<LogEntry>();

        // Serilog satır deseni: 2025-07-19 14:30:00.123 [INF] Message...
        private static readonly Regex LogLineRegex = new Regex(
            @"^(\d{4}-\d{2}-\d{2}\s+\d{2}:\d{2}:\d{2}\.\d{3})\s+\[(\w{3})\]\s+(.*)",
            RegexOptions.Compiled);

        public LogViewerForm()
        {
            InitializeComponent();

            _logDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "MikroSqlDbYedek", "Logs");

            _autoRefreshTimer = new Timer { Interval = 5000 }; // 5 saniye
            _autoRefreshTimer.Tick += OnAutoRefreshTick;
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            PopulateLogFiles();
            PopulateLevelFilter();
            LoadSelectedLogFile();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            _autoRefreshTimer.Stop();
            _autoRefreshTimer.Dispose();
            base.OnFormClosing(e);
        }

        #region Population

        private void PopulateLogFiles()
        {
            _cmbLogFile.Items.Clear();

            if (!Directory.Exists(_logDirectory))
            {
                _cmbLogFile.Items.Add(Res.Get("LogViewer_NoDirFound"));
                _cmbLogFile.SelectedIndex = 0;
                return;
            }

            var files = Directory.GetFiles(_logDirectory, "*.txt")
                .Concat(Directory.GetFiles(_logDirectory, "*.log"))
                .OrderByDescending(f => File.GetLastWriteTime(f))
                .ToArray();

            if (files.Length == 0)
            {
                _cmbLogFile.Items.Add(Res.Get("LogViewer_NoFilesFound"));
                _cmbLogFile.SelectedIndex = 0;
                return;
            }

            foreach (var file in files)
            {
                _cmbLogFile.Items.Add(Path.GetFileName(file));
            }

            _cmbLogFile.SelectedIndex = 0;
        }

        private void PopulateLevelFilter()
        {
            _cmbLevel.Items.Clear();
            _cmbLevel.Items.Add(Res.Get("LogViewer_LevelAll"));
            _cmbLevel.Items.Add(Res.Get("LogViewer_LevelVerbose"));
            _cmbLevel.Items.Add(Res.Get("LogViewer_LevelDebug"));
            _cmbLevel.Items.Add(Res.Get("LogViewer_LevelInfo"));
            _cmbLevel.Items.Add(Res.Get("LogViewer_LevelWarning"));
            _cmbLevel.Items.Add(Res.Get("LogViewer_LevelError"));
            _cmbLevel.Items.Add(Res.Get("LogViewer_LevelFatal"));
            _cmbLevel.SelectedIndex = 0;
        }

        #endregion

        #region Log File Reading

        private void LoadSelectedLogFile()
        {
            _allEntries.Clear();
            _dgvLogs.Rows.Clear();

            if (_cmbLogFile.SelectedIndex < 0) return;
            var fileName = _cmbLogFile.SelectedItem.ToString();
            if (fileName.StartsWith("(")) return;

            var filePath = Path.Combine(_logDirectory, fileName);
            if (!File.Exists(filePath)) return;

            try
            {
                // Dosya kilitli olabilir (Serilog yazıyor), FileShare.ReadWrite ile oku
                string[] lines;
                using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                using (var sr = new StreamReader(fs))
                {
                    lines = sr.ReadToEnd().Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
                }

                LogEntry currentEntry = null;
                foreach (var rawLine in lines)
                {
                    var line = rawLine.TrimEnd('\r');
                    var match = LogLineRegex.Match(line);

                    if (match.Success)
                    {
                        if (currentEntry != null)
                            _allEntries.Add(currentEntry);

                        currentEntry = new LogEntry
                        {
                            Timestamp = match.Groups[1].Value,
                            Level = match.Groups[2].Value,
                            Message = match.Groups[3].Value
                        };
                    }
                    else if (currentEntry != null)
                    {
                        // Çok satırlı mesaj devamı (exception stack trace vb.)
                        currentEntry.Message += Environment.NewLine + line;
                    }
                }

                if (currentEntry != null)
                    _allEntries.Add(currentEntry);

                ApplyFilter();
                _tslTotalLines.Text = Res.Format("LogViewer_RecordCount", _allEntries.Count);
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Log dosyası okunurken hata: {FileName}", fileName);
                _tslTotalLines.Text = Res.Get("LogViewer_ReadError");
            }
        }

        #endregion

        #region Filtering

        private void ApplyFilter()
        {
            _dgvLogs.Rows.Clear();

            string levelFilter = GetSelectedLevelCode();
            string searchText = _txtSearch.Text.Trim();
            bool hasSearch = !string.IsNullOrEmpty(searchText);

            foreach (var entry in _allEntries)
            {
                // Seviye filtresi
                if (levelFilter != null && !entry.Level.Equals(levelFilter, StringComparison.OrdinalIgnoreCase))
                    continue;

                // Metin arama
                if (hasSearch && entry.Message.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) < 0)
                    continue;

                int idx = _dgvLogs.Rows.Add(entry.Timestamp, entry.Level, entry.Message);
                ColorizeRow(_dgvLogs.Rows[idx], entry.Level);
            }

            _tslFilteredLines.Text = Res.Format("LogViewer_FilteredCount", _dgvLogs.Rows.Count);
        }

        private string GetSelectedLevelCode()
        {
            if (_cmbLevel.SelectedIndex <= 0) return null; // "Tümü"

            switch (_cmbLevel.SelectedIndex)
            {
                case 1: return "VRB";
                case 2: return "DBG";
                case 3: return "INF";
                case 4: return "WRN";
                case 5: return "ERR";
                case 6: return "FTL";
                default: return null;
            }
        }

        private static void ColorizeRow(DataGridViewRow row, string level)
        {
            switch (level)
            {
                case "ERR":
                case "FTL":
                    row.DefaultCellStyle.ForeColor = System.Drawing.Color.Red;
                    break;
                case "WRN":
                    row.DefaultCellStyle.ForeColor = System.Drawing.Color.DarkOrange;
                    break;
                case "DBG":
                case "VRB":
                    row.DefaultCellStyle.ForeColor = System.Drawing.SystemColors.GrayText;
                    break;
            }
        }

        #endregion

        #region Events

        private void OnLogFileChanged(object sender, EventArgs e)
        {
            LoadSelectedLogFile();
        }

        private void OnLevelFilterChanged(object sender, EventArgs e)
        {
            ApplyFilter();
        }

        private void OnSearchTextChanged(object sender, EventArgs e)
        {
            ApplyFilter();
        }

        private void OnRefreshClick(object sender, EventArgs e)
        {
            LoadSelectedLogFile();
        }

        private void OnAutoTailToggle(object sender, EventArgs e)
        {
            if (_chkAutoTail.Checked)
            {
                _autoRefreshTimer.Start();
            }
            else
            {
                _autoRefreshTimer.Stop();
            }
        }

        private void OnAutoRefreshTick(object sender, EventArgs e)
        {
            LoadSelectedLogFile();

            // Son satıra scroll
            if (_dgvLogs.Rows.Count > 0)
            {
                _dgvLogs.FirstDisplayedScrollingRowIndex = _dgvLogs.Rows.Count - 1;
            }
        }

        private void OnExportClick(object sender, EventArgs e)
        {
            using (var sfd = new SaveFileDialog())
            {
                sfd.Title = Res.Get("LogViewer_ExportDialogTitle");
                sfd.Filter = Res.Get("LogViewer_ExportFilter");
                sfd.FileName = "MikroSqlDbYedek_Log_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".txt";

                if (sfd.ShowDialog(this) == DialogResult.OK)
                {
                    try
                    {
                        using (var sw = new StreamWriter(sfd.FileName))
                        {
                            foreach (DataGridViewRow row in _dgvLogs.Rows)
                            {
                                sw.WriteLine("{0}\t[{1}]\t{2}",
                                    row.Cells[0].Value,
                                    row.Cells[1].Value,
                                    row.Cells[2].Value);
                            }
                        }

                        MessageBox.Show(
                            Res.Format("LogViewer_ExportSuccessFormat", _dgvLogs.Rows.Count),
                            Res.Get("LogViewer_ExportSuccessTitle"),
                            MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(Res.Format("LogViewer_ExportError", ex.Message),
                            Res.Get("Error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void OnClearFilterClick(object sender, EventArgs e)
        {
            _txtSearch.Clear();
            _cmbLevel.SelectedIndex = 0;
            ApplyFilter();
        }

        #endregion

        #region Inner Model

        private class LogEntry
        {
            public string Timestamp { get; set; }
            public string Level { get; set; }
            public string Message { get; set; }
        }

        #endregion
    }
}
