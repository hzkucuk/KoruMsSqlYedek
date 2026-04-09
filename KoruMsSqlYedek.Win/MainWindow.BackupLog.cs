using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using KoruMsSqlYedek.Core.Models;

namespace KoruMsSqlYedek.Win
{
    /// <summary>
    /// MainWindow — Yedekleme log buffer yönetimi ve RichTextBox rendering.
    /// Buffer (planId → satır listesi) ile UI arasındaki senkronizasyonu sağlar.
    /// 
    /// Sorumluluklar:
    ///   1. Plan bazlı log buffer yönetimi (_planLogs)
    ///   2. RichTextBox'a renkli satır ekleme
    ///   3. İlerleme satırını yerinde güncelleme (tek satır, yeni satır eklenmez)
    /// </summary>
    public partial class MainWindow
    {
        // İlerleme satırı tespiti için önek sabitleri
            private const string CloudUploadLineMarker = "Bulut yükleme:";
            private const string CompressProgressMarker = "\u0131k\u0131\u015ft\u0131r\u0131l\u0131yor";

        // Per-plan log buffer (planId → satır listesi + renk)
        private readonly Dictionary<string, List<(string Text, Color Color)>> _planLogs = new Dictionary<string, List<(string Text, Color Color)>>();

        /// <summary>
        /// Metnin ilerleme satırı olup olmadığını kontrol eder.
        /// Bulut yükleme progress ve sıkıştırma progress satırlarını kapsar.
        /// </summary>
        private static bool IsProgressLine(string text)
            => (text.Contains(CloudUploadLineMarker) && text.Contains("Yükleniyor"))
            || text.Contains(CompressProgressMarker);

        /// <summary>
        /// Plan'a ait log buffer'ına satır ekler ve seçili plan ise UI'yı günceller.
        /// <paramref name="isProgressLine"/> true ise son ilerleme satırı yerinde güncellenir.
        /// </summary>
        private void AppendBackupLog(string planId, string line, Color color, bool isProgressLine = false)
        {
            if (string.IsNullOrEmpty(line)) return;

            if (InvokeRequired)
            {
                Invoke(new Action(() => AppendBackupLog(planId, line, color, isProgressLine)));
                return;
            }

            // PlanId yoksa çalışan plan'ın id'sini kullan (fallback)
            string effectivePlanId = !string.IsNullOrEmpty(planId) ? planId : _viewingPlanId;

            string formatted = "[" + DateTime.Now.ToString("HH:mm:ss") + "] " + line;

            // Plan'a ait buffer'a ekle (ilerleme satırı ise son ilerleme satırını güncelle)
            if (!string.IsNullOrEmpty(effectivePlanId))
            {
                if (!_planLogs.ContainsKey(effectivePlanId))
                    _planLogs[effectivePlanId] = new List<(string, Color)>();

                var logList = _planLogs[effectivePlanId];
                if (isProgressLine && logList.Count > 0 && IsProgressLine(logList[logList.Count - 1].Text))
                    logList[logList.Count - 1] = (formatted, color);
                else
                    logList.Add((formatted, color));
            }

            // Sadece seçili plan ile eşleşiyorsa UI'yi güncelle
            var selected = GetSelectedPlanSilent();
            if (selected != null && selected.PlanId == effectivePlanId)
            {
                if (isProgressLine)
                    ReplaceLastProgressLine(formatted, color);
                else
                    AppendColoredLine(formatted, color);
            }
        }

        /// <summary>
        /// RichTextBox'a renkli satır ekler.
        /// </summary>
        private void AppendColoredLine(string text, Color color)
        {
            _txtBackupLog.SelectionStart = _txtBackupLog.TextLength;
            _txtBackupLog.SelectionLength = 0;
            _txtBackupLog.SelectionColor = color;
            _txtBackupLog.AppendText(text + Environment.NewLine);
            _txtBackupLog.SelectionColor = Theme.ModernTheme.LogDefault;
            _txtBackupLog.ScrollToCaret();
        }

        /// <summary>
        /// RichTextBox'taki son ilerleme satırını yenisiyle değiştirir (renkli).
        /// RichTextBox dahili olarak \n kullanır; Select() ile Text indeksi uyumsuz olduğundan
        /// Lines[] + GetFirstCharIndexFromLine() ile doğru pozisyon hesaplanır.
        /// </summary>
        private void ReplaceLastProgressLine(string newLine, Color color)
        {
            int lineCount = _txtBackupLog.Lines.Length;
            if (lineCount == 0)
            {
                AppendColoredLine(newLine, color);
                return;
            }

            // Son boş olmayan satırı bul (RichTextBox.Lines sona boş eleman ekleyebilir)
            int lastLineIdx = lineCount - 1;
            if (lastLineIdx > 0 && string.IsNullOrEmpty(_txtBackupLog.Lines[lastLineIdx]))
                lastLineIdx--;

            string lastLine = _txtBackupLog.Lines[lastLineIdx];

            if (IsProgressLine(lastLine))
            {
                int charIdx = _txtBackupLog.GetFirstCharIndexFromLine(lastLineIdx);
                _txtBackupLog.Select(charIdx, _txtBackupLog.TextLength - charIdx);
                _txtBackupLog.SelectionColor = color;
                _txtBackupLog.SelectedText = newLine + Environment.NewLine;
                _txtBackupLog.SelectionStart = _txtBackupLog.TextLength;
                _txtBackupLog.ScrollToCaret();
                return;
            }

            // Son satır ilerleme satırı değilse normal append
            AppendColoredLine(newLine, color);
        }
    }
}
