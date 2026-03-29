using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace KoruMsSqlYedek.Win.Theme
{
    /// <summary>
    /// Modern NumericUpDown — özel kenar çizgisi, focus accent, düz stil spin butonları.
    /// UserControl içinde TextBox + iki buton olarak oluşturulur.
    /// </summary>
    internal class ModernNumericUpDown : UserControl
    {
        private decimal _value;
        private decimal _minimum;
        private decimal _maximum = 100m;
        private decimal _increment = 1m;
        private int _decimalPlaces;
        private Color _borderColor = ModernTheme.BorderColor;
        private Color _focusBorderColor = ModernTheme.AccentPrimary;
        private bool _isFocused;

        private readonly TextBox _textBox;
        private readonly Panel _btnUp;
        private readonly Panel _btnDown;

        public ModernNumericUpDown()
        {
            SetStyle(
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw |
                ControlStyles.UserPaint,
                true);

            Height = 28;
            MinimumSize = new Size(60, 24);
            BackColor = ModernTheme.SurfaceColor;
            Font = ModernTheme.FontBody;

            _textBox = new TextBox
            {
                BorderStyle = BorderStyle.None,
                Font = Font,
                BackColor = ModernTheme.SurfaceColor,
                ForeColor = ModernTheme.TextPrimary,
                TextAlign = HorizontalAlignment.Left,
                Text = "0"
            };
            _textBox.Enter += (s, e) => { _isFocused = true; Invalidate(); };
            _textBox.Leave += (s, e) => { _isFocused = false; ParseText(); Invalidate(); };
            _textBox.KeyDown += OnTextKeyDown;
            _textBox.KeyPress += OnTextKeyPress;

            _btnUp = new Panel { Cursor = Cursors.Hand, BackColor = Color.Transparent };
            _btnDown = new Panel { Cursor = Cursors.Hand, BackColor = Color.Transparent };
            _btnUp.Paint += PaintUpArrow;
            _btnDown.Paint += PaintDownArrow;
            _btnUp.Click += (s, e) => { Value += _increment; };
            _btnDown.Click += (s, e) => { Value -= _increment; };

            Controls.Add(_textBox);
            Controls.Add(_btnUp);
            Controls.Add(_btnDown);

            LayoutControls();
        }

        /// <summary>Geçerli değer.</summary>
        [Category("Modern"), Description("Geçerli sayısal değer.")]
        [DefaultValue(typeof(decimal), "0")]
        public decimal Value
        {
            get => _value;
            set
            {
                decimal clamped = Math.Max(_minimum, Math.Min(_maximum, value));
                if (_value != clamped)
                {
                    _value = clamped;
                    _textBox.Text = _value.ToString("F" + _decimalPlaces);
                    ValueChanged?.Invoke(this, EventArgs.Empty);
                    Invalidate();
                }
            }
        }

        [Category("Modern"), DefaultValue(typeof(decimal), "0")]
        public decimal Minimum
        {
            get => _minimum;
            set { _minimum = value; if (_value < _minimum) Value = _minimum; }
        }

        [Category("Modern"), DefaultValue(typeof(decimal), "100")]
        public decimal Maximum
        {
            get => _maximum;
            set { _maximum = value; if (_value > _maximum) Value = _maximum; }
        }

        [Category("Modern"), DefaultValue(typeof(decimal), "1")]
        public decimal Increment
        {
            get => _increment;
            set => _increment = Math.Max(0.01m, value);
        }

        [Category("Modern"), DefaultValue(0)]
        public int DecimalPlaces
        {
            get => _decimalPlaces;
            set { _decimalPlaces = Math.Max(0, value); _textBox.Text = _value.ToString("F" + _decimalPlaces); }
        }

        /// <summary>Değer değiştiğinde tetiklenir.</summary>
        public event EventHandler ValueChanged;

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            ModernTheme.SetHighQuality(g);

            var rect = new Rectangle(0, 0, Width - 1, Height - 1);
            int radius = 4;

            // Arka plan
            using (var path = ModernTheme.CreateRoundedRectanglePath(rect, radius))
            using (var brush = new SolidBrush(BackColor))
            {
                g.FillPath(brush, path);
            }

            // Kenar — yuvarlatılmış
            Color borderClr = _isFocused ? _focusBorderColor : _borderColor;
            float borderWidth = _isFocused ? 2f : 1f;

            using (var path = ModernTheme.CreateRoundedRectanglePath(rect, radius))
            using (var pen = new Pen(borderClr, borderWidth))
            {
                g.DrawPath(pen, path);
            }

            // Buton ayırıcı çizgi
            int btnWidth = 20;
            int btnX = Width - btnWidth - 1;
            using (var pen = new Pen(ModernTheme.DividerColor, 1f))
            {
                g.DrawLine(pen, btnX, 4, btnX, Height - 5);
            }
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            if (_textBox == null) return; // constructor henüz tamamlanmadı
            LayoutControls();
        }

        protected override void OnFontChanged(EventArgs e)
        {
            base.OnFontChanged(e);
            if (_textBox == null) return;
            _textBox.Font = Font;
        }

        private void LayoutControls()
        {
            int btnWidth = 20;
            int btnX = Width - btnWidth - 1;
            int halfH = Height / 2;

             _textBox.Location = new Point(6, (Height - _textBox.PreferredHeight) / 2);
            _textBox.Width = btnX - 10;

            _btnUp.SetBounds(btnX, 1, btnWidth, halfH - 1);
            _btnDown.SetBounds(btnX, halfH, btnWidth, Height - halfH - 1);
        }

        private void PaintUpArrow(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            int cx = _btnUp.Width / 2;
            int cy = _btnUp.Height / 2;
            using (var brush = new SolidBrush(ModernTheme.TextSecondary))
            {
                g.FillPolygon(brush, new[]
                {
                    new Point(cx - 4, cy + 2),
                    new Point(cx + 4, cy + 2),
                    new Point(cx, cy - 2)
                });
            }
        }

        private void PaintDownArrow(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            int cx = _btnDown.Width / 2;
            int cy = _btnDown.Height / 2;
            using (var brush = new SolidBrush(ModernTheme.TextSecondary))
            {
                g.FillPolygon(brush, new[]
                {
                    new Point(cx - 4, cy - 2),
                    new Point(cx + 4, cy - 2),
                    new Point(cx, cy + 2)
                });
            }
        }

        private void OnTextKeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Up)
            {
                Value += _increment;
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.Down)
            {
                Value -= _increment;
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.Enter)
            {
                ParseText();
                e.Handled = true;
            }
        }

        private void OnTextKeyPress(object sender, KeyPressEventArgs e)
        {
            // Sadece sayılar, ondalık ayraç, negatif işareti ve kontrol tuşları
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar)
                && e.KeyChar != '.' && e.KeyChar != ',' && e.KeyChar != '-')
            {
                e.Handled = true;
            }
        }

        private void ParseText()
        {
            if (decimal.TryParse(_textBox.Text, out decimal parsed))
            {
                Value = parsed;
            }
            else
            {
                _textBox.Text = _value.ToString("F" + _decimalPlaces);
            }
        }
    }
}
