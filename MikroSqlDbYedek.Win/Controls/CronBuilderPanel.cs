using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace MikroSqlDbYedek.Win.Controls
{
    /// <summary>
    /// Quartz.NET cron ifadesi oluşturmak için kullanıcı dostu panel.
    /// Sıklık (günlük/haftalık/aylık/özel), gün seçimi, saat/dakika ile
    /// cron ifadesi otomatik üretilir ve önizleme gösterilir.
    /// </summary>
    internal sealed class CronBuilderPanel : UserControl
    {
        private ComboBox _cmbFrequency;
        private Panel _pnlDaysOfWeek;
        private CheckBox[] _dayChecks;
        private Theme.ModernNumericUpDown _nudDayOfMonth;
        private Theme.ModernNumericUpDown _nudHour;
        private Theme.ModernNumericUpDown _nudMinute;
        private Label _lblPreview;
        private Label _lblCronRaw;
        private TextBox _txtCustomCron;
        private Label _lblFreq;
        private Label _lblDayOfMonth;
        private Label _lblHour;
        private Label _lblMinute;

        private static readonly string[] DayNames = { "Pzt", "Sal", "Çar", "Per", "Cum", "Cmt", "Paz" };
        // Quartz DOW: SUN=1, MON=2, ... SAT=7
        private static readonly string[] QuartzDayAbbrs = { "MON", "TUE", "WED", "THU", "FRI", "SAT", "SUN" };

        public CronBuilderPanel()
        {
            BuildUi();
            _cmbFrequency.SelectedIndex = 0;
            UpdateVisibility();
            UpdatePreview();
        }

        /// <summary>Oluşturulan Quartz.NET cron ifadesini döndürür.</summary>
        public string GetCronExpression()
        {
            if (_cmbFrequency.SelectedIndex == 3) // Özel
                return _txtCustomCron.Text.Trim();

            int minute = (int)_nudMinute.Value;
            int hour = (int)_nudHour.Value;

            return _cmbFrequency.SelectedIndex switch
            {
                0 => $"0 {minute} {hour} ? * *",           // Günlük
                1 => $"0 {minute} {hour} ? * {BuildDowString()}", // Haftalık
                2 => $"0 {minute} {hour} {(int)_nudDayOfMonth.Value} * ?", // Aylık
                _ => ""
            };
        }

        /// <summary>Mevcut cron ifadesini ayrıştırarak UI'ye yükler.</summary>
        public void SetCronExpression(string cron)
        {
            if (string.IsNullOrWhiteSpace(cron))
            {
                _cmbFrequency.SelectedIndex = 0;
                _nudHour.Value = 2;
                _nudMinute.Value = 0;
                UpdateVisibility();
                UpdatePreview();
                return;
            }

            var parts = cron.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 6)
            {
                _cmbFrequency.SelectedIndex = 3;
                _txtCustomCron.Text = cron;
                UpdateVisibility();
                UpdatePreview();
                return;
            }

            // parts: sec min hour dayOfMonth month dayOfWeek
            if (int.TryParse(parts[1], out int min)) _nudMinute.Value = Math.Clamp(min, 0, 59);
            if (int.TryParse(parts[2], out int hr)) _nudHour.Value = Math.Clamp(hr, 0, 23);

            if (parts[5] == "*" || parts[5] == "?")
            {
                // Günlük veya aylık
                if (parts[3] != "?" && int.TryParse(parts[3], out int dom))
                {
                    _cmbFrequency.SelectedIndex = 2; // Aylık
                    _nudDayOfMonth.Value = Math.Clamp(dom, 1, 28);
                }
                else
                {
                    _cmbFrequency.SelectedIndex = 0; // Günlük
                }
            }
            else
            {
                _cmbFrequency.SelectedIndex = 1; // Haftalık
                ParseDowString(parts[5]);
            }

            UpdateVisibility();
            UpdatePreview();
        }

        private string BuildDowString()
        {
            var days = new List<string>();
            for (int i = 0; i < 7; i++)
            {
                if (_dayChecks[i].Checked)
                    days.Add(QuartzDayAbbrs[i]);
            }

            return days.Count > 0 ? string.Join(",", days) : "MON";
        }

        private void ParseDowString(string dow)
        {
            for (int i = 0; i < 7; i++) _dayChecks[i].Checked = false;

            var parts = dow.Split(',');
            foreach (var part in parts)
            {
                string p = part.Trim().ToUpperInvariant();
                for (int i = 0; i < QuartzDayAbbrs.Length; i++)
                {
                    if (p == QuartzDayAbbrs[i])
                    {
                        _dayChecks[i].Checked = true;
                        break;
                    }
                }
            }
        }

        private string GetHumanReadable()
        {
            if (_cmbFrequency.SelectedIndex == 3)
                return "Özel cron ifadesi";

            int hour = (int)_nudHour.Value;
            int minute = (int)_nudMinute.Value;
            string time = $"{hour:D2}:{minute:D2}";

            return _cmbFrequency.SelectedIndex switch
            {
                0 => $"Her gün saat {time}",
                1 => $"{BuildDowHumanReadable()} saat {time}",
                2 => $"Her ayın {(int)_nudDayOfMonth.Value}. günü saat {time}",
                _ => ""
            };
        }

        private string BuildDowHumanReadable()
        {
            var days = new List<string>();
            for (int i = 0; i < 7; i++)
            {
                if (_dayChecks[i].Checked)
                    days.Add(DayNames[i]);
            }

            return days.Count > 0 ? "Her " + string.Join(", ", days) : "Her Pzt";
        }

        private void UpdatePreview()
        {
            string cron = GetCronExpression();
            _lblCronRaw.Text = cron;
            _lblPreview.Text = GetHumanReadable();
        }

        private void UpdateVisibility()
        {
            int idx = _cmbFrequency.SelectedIndex;
            _pnlDaysOfWeek.Visible = idx == 1;
            _nudDayOfMonth.Visible = idx == 2;
            _lblDayOfMonth.Visible = idx == 2;

            bool isCustom = idx == 3;
            _txtCustomCron.Visible = isCustom;
            _nudHour.Visible = !isCustom;
            _nudMinute.Visible = !isCustom;
            _lblHour.Visible = !isCustom;
            _lblMinute.Visible = !isCustom;
        }

        private void OnValueChanged(object sender, EventArgs e)
        {
            UpdateVisibility();
            UpdatePreview();
        }

        private void BuildUi()
        {
            SuspendLayout();

            Height = 100;
            Dock = DockStyle.None;
            BackColor = Color.Transparent;

            int y = 0;

            // Row 1: Sıklık + Saat/Dakika
            _lblFreq = new Label { Text = "Sıklık:", AutoSize = true, Location = new Point(0, y + 3) };
            Controls.Add(_lblFreq);

            _cmbFrequency = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Location = new Point(52, y),
                Size = new Size(110, 23)
            };
            _cmbFrequency.Items.AddRange(new object[] { "Günlük", "Haftalık", "Aylık", "Özel (Cron)" });
            _cmbFrequency.SelectedIndexChanged += OnValueChanged;
            Controls.Add(_cmbFrequency);

            _lblHour = new Label { Text = "Saat:", AutoSize = true, Location = new Point(172, y + 3) };
            Controls.Add(_lblHour);

            _nudHour = new Theme.ModernNumericUpDown
            {
                Location = new Point(208, y),
                Size = new Size(55, 23),
                Minimum = 0, Maximum = 23, Value = 2
            };
            _nudHour.ValueChanged += OnValueChanged;
            Controls.Add(_nudHour);

            _lblMinute = new Label { Text = ":", AutoSize = true, Location = new Point(265, y + 3) };
            Controls.Add(_lblMinute);

            _nudMinute = new Theme.ModernNumericUpDown
            {
                Location = new Point(275, y),
                Size = new Size(55, 23),
                Minimum = 0, Maximum = 59, Value = 0
            };
            _nudMinute.ValueChanged += OnValueChanged;
            Controls.Add(_nudMinute);

            // Aylık gün seçimi (aynı satırda, sıklık=Aylık olduğunda gösterilir)
            _lblDayOfMonth = new Label { Text = "Gün:", AutoSize = true, Location = new Point(340, y + 3), Visible = false };
            Controls.Add(_lblDayOfMonth);

            _nudDayOfMonth = new Theme.ModernNumericUpDown
            {
                Location = new Point(372, y),
                Size = new Size(55, 23),
                Minimum = 1, Maximum = 28, Value = 1,
                Visible = false
            };
            _nudDayOfMonth.ValueChanged += OnValueChanged;
            Controls.Add(_nudDayOfMonth);

            // Özel cron TextBox (aynı satırda saat/dakika yerine)
            _txtCustomCron = new TextBox
            {
                Location = new Point(172, y),
                Size = new Size(260, 23),
                Visible = false,
                PlaceholderText = "0 0 2 ? * SUN"
            };
            _txtCustomCron.TextChanged += OnValueChanged;
            Controls.Add(_txtCustomCron);

            y += 28;

            // Row 2: Haftalık gün seçimi
            _pnlDaysOfWeek = new Panel
            {
                Location = new Point(0, y),
                Size = new Size(440, 26),
                Visible = false
            };
            _dayChecks = new CheckBox[7];
            for (int i = 0; i < 7; i++)
            {
                _dayChecks[i] = new CheckBox
                {
                    Text = DayNames[i],
                    AutoSize = true,
                    Location = new Point(i * 60, 0),
                    Checked = i == 6 // Pazar varsayılan
                };
                _dayChecks[i].CheckedChanged += OnValueChanged;
                _pnlDaysOfWeek.Controls.Add(_dayChecks[i]);
            }
            Controls.Add(_pnlDaysOfWeek);
            y += 28;

            // Row 3: Önizleme
            _lblPreview = new Label
            {
                AutoSize = true,
                Location = new Point(0, y),
                ForeColor = Theme.ModernTheme.AccentPrimary,
                Font = new Font("Segoe UI", 8.5F, FontStyle.Italic)
            };
            Controls.Add(_lblPreview);

            _lblCronRaw = new Label
            {
                AutoSize = true,
                Location = new Point(250, y),
                ForeColor = Theme.ModernTheme.TextSecondary,
                Font = new Font("Segoe UI", 8F)
            };
            Controls.Add(_lblCronRaw);

            ResumeLayout(false);
        }
    }
}
