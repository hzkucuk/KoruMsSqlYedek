using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using KoruMsSqlYedek.Win.Helpers;

namespace KoruMsSqlYedek.Win
{
    // TAB 2: Loglar — Dosya listeleme, filtreleme, VirtualMode grid, dışa aktarma.
    public partial class MainWindow
    {
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
                _cmbLogFile.Items.Add(Path.GetFileName(file));

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

        private void PopulateLogPlanFilter()
        {
            _cmbLogPlan.Items.Clear();
            _cmbLogPlan.Items.Add(Res.Get("LogViewer_AllPlans"));
            foreach (var plan in _planManager.GetAllPlans())
                _cmbLogPlan.Items.Add(plan.PlanName ?? plan.PlanId);
            _cmbLogPlan.SelectedIndex = 0;
        }

        private void LoadSelectedLogFile()
        {
            _allLogEntries.Clear();
            _filteredLogEntries.Clear();
            _dgvLogs.RowCount = 0;

            if (_cmbLogFile.SelectedIndex < 0) return;
            var fileName = _cmbLogFile.SelectedItem?.ToString();
            if (string.IsNullOrEmpty(fileName) || fileName.StartsWith("(")) return;

            var filePath = Path.Combine(_logDirectory, fileName);
            if (!File.Exists(filePath)) return;

            try
            {
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
                            _allLogEntries.Add(currentEntry);

                        currentEntry = new LogEntry
                        {
                            Timestamp = match.Groups[1].Value,
                            Level = match.Groups[2].Value,
                            Message = match.Groups[3].Value
                        };
                    }
                    else if (currentEntry != null)
                    {
                        currentEntry.Message += Environment.NewLine + line;
                    }
                }

                if (currentEntry != null)
                    _allLogEntries.Add(currentEntry);

                ApplyLogFilter();
                _tslLogTotal.Text = Res.Format("LogViewer_RecordCount", _allLogEntries.Count);
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Log dosyası okunurken hata: {FileName}", fileName);
                _tslLogTotal.Text = Res.Get("LogViewer_ReadError");
            }
        }

        private void ApplyLogFilter()
        {
            string levelFilter = GetSelectedLevelCode();
            string searchText = _txtLogSearch.Text.Trim();
            string planFilter = _cmbLogPlan.SelectedIndex > 0 ? _cmbLogPlan.SelectedItem?.ToString() : null;
            bool hasSearch = !string.IsNullOrEmpty(searchText);

            _filteredLogEntries.Clear();

            foreach (var entry in _allLogEntries)
            {
                if (levelFilter != null && !entry.Level.Equals(levelFilter, StringComparison.OrdinalIgnoreCase))
                    continue;

                if (hasSearch && entry.Message.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) < 0)
                    continue;

                if (planFilter != null && entry.Message.IndexOf(planFilter, StringComparison.OrdinalIgnoreCase) < 0)
                    continue;

                _filteredLogEntries.Add(entry);
            }

            _dgvLogs.RowCount = 0;
            _dgvLogs.RowCount = _filteredLogEntries.Count;

            _tslLogFiltered.Text = Res.Format("LogViewer_FilteredCount", _filteredLogEntries.Count);
        }

        private void OnLogCellValueNeeded(object sender, DataGridViewCellValueEventArgs e)
        {
            if (e.RowIndex < 0 || e.RowIndex >= _filteredLogEntries.Count) return;
            var entry = _filteredLogEntries[e.RowIndex];
            switch (e.ColumnIndex)
            {
                case 0: e.Value = entry.Timestamp; break;
                case 1: e.Value = entry.Level; break;
                case 2: e.Value = entry.Message; break;
            }
        }

        private void OnLogCellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0 || e.RowIndex >= _filteredLogEntries.Count) return;
            var level = _filteredLogEntries[e.RowIndex].Level;
            switch (level)
            {
                case "ERR":
                case "FTL":
                    e.CellStyle.ForeColor = Color.Red;
                    break;
                case "WRN":
                    e.CellStyle.ForeColor = Color.DarkOrange;
                    break;
                case "DBG":
                case "VRB":
                    e.CellStyle.ForeColor = SystemColors.GrayText;
                    break;
            }
        }

        private string GetSelectedLevelCode()
        {
            if (_cmbLevel.SelectedIndex <= 0) return null;
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

        private void OnLogFileChanged(object sender, EventArgs e) => LoadSelectedLogFile();
        private void OnLevelFilterChanged(object sender, EventArgs e) => ApplyLogFilter();
        private void OnLogSearchTextChanged(object sender, EventArgs e) => ApplyLogFilter();
        private void OnLogPlanFilterChanged(object sender, EventArgs e) => ApplyLogFilter();

        private void OnLogRefreshClick(object sender, EventArgs e) => LoadSelectedLogFile();

        private void OnAutoTailToggle(object sender, EventArgs e)
        {
            if (_chkAutoTail.Checked)
                _logTimer.Start();
            else
                _logTimer.Stop();
        }

        private void OnLogAutoRefreshTick(object sender, EventArgs e)
        {
            LoadSelectedLogFile();
            if (_dgvLogs.Rows.Count > 0)
                _dgvLogs.FirstDisplayedScrollingRowIndex = _dgvLogs.Rows.Count - 1;
        }

        private void OnLogExportClick(object sender, EventArgs e)
        {
            using (var sfd = new SaveFileDialog())
            {
                sfd.Title = Res.Get("LogViewer_ExportDialogTitle");
                sfd.Filter = Res.Get("LogViewer_ExportFilter");
                sfd.FileName = "KoruMsSqlYedek_Log_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".txt";

                if (sfd.ShowDialog(this) == DialogResult.OK)
                {
                    try
                    {
                        using (var sw = new StreamWriter(sfd.FileName))
                        {
                            foreach (DataGridViewRow row in _dgvLogs.Rows)
                            {
                                sw.WriteLine("{0}\t[{1}]\t{2}",
                                    row.Cells[0].Value, row.Cells[1].Value, row.Cells[2].Value);
                            }
                        }

                        Theme.ModernMessageBox.Show(
                            Res.Format("LogViewer_ExportSuccessFormat", _dgvLogs.Rows.Count),
                            Res.Get("LogViewer_ExportSuccessTitle"),
                            MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        Theme.ModernMessageBox.Show(Res.Format("LogViewer_ExportError", ex.Message),
                            Res.Get("Error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void OnClearLogFilterClick(object sender, EventArgs e)
        {
            _txtLogSearch.Clear();
            _cmbLevel.SelectedIndex = 0;
            _cmbLogPlan.SelectedIndex = 0;
            ApplyLogFilter();
        }

        private class LogEntry
        {
            public string Timestamp { get; set; }
            public string Level { get; set; }
            public string Message { get; set; }
        }
    }
}
