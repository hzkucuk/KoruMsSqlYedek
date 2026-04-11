using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace KoruMsSqlYedek.Win.Theme
{
    /// <summary>
    /// Modern arama kutusu — sol tarafta arama ikonu, sağ tarafta temizle (X) butonu.
    /// LogViewer ve PlanList gibi formlarda filtreleme için kullanılır.
    /// </summary>
    internal class ModernSearchBox : UserControl
    {
        private readonly TextBox _innerTextBox;
        private readonly Label _iconLabel;
        private string _placeholder = "Ara...";
        private bool _isFocused;
        private bool _showClearButton = true;
        private bool _isClearHovered;
        private Rectangle _clearButtonRect;

        public ModernSearchBox()
        {
            SetStyle(
                ControlStyles.UserPaint |
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.DoubleBuffer |
                ControlStyles.ResizeRedraw |
                ControlStyles.SupportsTransparentBackColor,
                true);

            Size = new Size(250, 32);
            BackColor = ModernTheme.SurfaceColor;
            Cursor = Cursors.IBeam;

            _iconLabel = new Label
            {
                Text = "\uE721", // Search icon (Segoe MDL2 Assets)
                Font = new Font("Segoe MDL2 Assets", 10f),
                ForeColor = ModernTheme.TextDisabled,
                AutoSize = false,
                Size = new Size(24, 24),
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent
            };

            _innerTextBox = new TextBox
            {
                BorderStyle = BorderStyle.None,
                BackColor = ModernTheme.SurfaceColor,
                ForeColor = ModernTheme.TextPrimary,
                Font = ModernTheme.FontBody
            };

            _innerTextBox.GotFocus += (s, e) => { _isFocused = true; Invalidate(); };
            _innerTextBox.LostFocus += (s, e) => { _isFocused = false; Invalidate(); };
            _innerTextBox.TextChanged += (s, e) => { OnTextChanged(e); Invalidate(); };
            _innerTextBox.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Escape && !string.IsNullOrEmpty(_innerTextBox.Text))
                {
                    _innerTextBox.Clear();
                    e.Handled = true;
                }
            };

            Controls.Add(_iconLabel);
            Controls.Add(_innerTextBox);
            PositionControls();
        }

        [Category("Modern"), Description("Placeholder metni.")]
        public string Placeholder
        {
            get => _placeholder;
            set { _placeholder = value ?? string.Empty; Invalidate(); }
        }

        [Category("Modern"), Description("Temizle butonu gösterilsin mi?")]
        public bool ShowClearButton
        {
            get => _showClearButton;
            set { _showClearButton = value; Invalidate(); }
        }

        [Browsable(true)]
        public override string Text
        {
            get => _innerTextBox.Text;
            set
            {
                _innerTextBox.Text = value;
                Invalidate();
            }
        }

        /// <summary>Metin değiştiğinde tetiklenir.</summary>
        public new event EventHandler TextChanged
        {
            add => _innerTextBox.TextChanged += value;
            remove => _innerTextBox.TextChanged -= value;
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            PositionControls();
        }

        private void PositionControls()
        {
            int iconX = 6;
            int textX = iconX + 24;
            int clearW = _showClearButton ? 24 : 0;
            int y = (Height - _innerTextBox.PreferredHeight) / 2;

            _iconLabel.Location = new Point(iconX, (Height - 24) / 2);
            _innerTextBox.Location = new Point(textX, Math.Max(2, y));
            _innerTextBox.Width = Width - textX - clearW - 6;

            _clearButtonRect = new Rectangle(Width - 28, (Height - 20) / 2, 20, 20);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            ModernTheme.SetHighQuality(g);

            var rect = new Rectangle(0, 0, Width - 1, Height - 1);
            var borderColor = _isFocused ? ModernTheme.AccentPrimary : ModernTheme.BorderColor;
            float borderWidth = _isFocused ? 1.5f : 1f;

            using (var path = ModernTheme.CreateRoundedRectanglePath(rect, Height / 2))
            {
                using (var bgBrush = new SolidBrush(ModernTheme.SurfaceColor))
                {
                    g.FillPath(bgBrush, path);
                }

                using (var borderPen = new Pen(borderColor, borderWidth))
                {
                    g.DrawPath(borderPen, path);
                }
            }

            // Placeholder
            if (string.IsNullOrEmpty(_innerTextBox.Text) && !_isFocused && !string.IsNullOrEmpty(_placeholder))
            {
                using (var phBrush = new SolidBrush(ModernTheme.TextDisabled))
                {
                    int textX = 30;
                    g.DrawString(_placeholder, ModernTheme.FontBody, phBrush, textX, (Height - ModernTheme.FontBody.Height) / 2f);
                }
            }

            // Clear button
            if (_showClearButton && !string.IsNullOrEmpty(_innerTextBox.Text))
            {
                var clearColor = _isClearHovered
                    ? ModernTheme.TextPrimary
                    : ModernTheme.TextDisabled;

                using (var clearFont = new Font("Segoe MDL2 Assets", 8f))
                using (var clearBrush = new SolidBrush(clearColor))
                {
                    var sf = new StringFormat
                    {
                        Alignment = StringAlignment.Center,
                        LineAlignment = StringAlignment.Center
                    };
                    g.DrawString("\uE711", clearFont, clearBrush, _clearButtonRect, sf); // X icon
                }
            }
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            if (Parent != null)
            {
                using (var bgBrush = new SolidBrush(Parent.BackColor))
                {
                    e.Graphics.FillRectangle(bgBrush, ClientRectangle);
                }
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            bool wasHovered = _isClearHovered;
            _isClearHovered = _clearButtonRect.Contains(e.Location);
            Cursor = _isClearHovered ? Cursors.Default : Cursors.IBeam;
            if (wasHovered != _isClearHovered) Invalidate();
        }

        protected override void OnMouseClick(MouseEventArgs e)
        {
            base.OnMouseClick(e);
            if (_showClearButton && _clearButtonRect.Contains(e.Location))
            {
                _innerTextBox.Clear();
                _innerTextBox.Focus();
            }
            else
            {
                _innerTextBox.Focus();
            }
        }

        /// <summary>Inner TextBox'a focus verir.</summary>
        public new void Focus()
        {
            _innerTextBox.Focus();
        }

        /// <summary>Arama metnini temizler.</summary>
        public void Clear()
        {
            _innerTextBox.Clear();
        }

        /// <summary>Tema değişikliğinde cache'lenmiş renkleri günceller.</summary>
        internal void RefreshThemeColors()
        {
            BackColor = ModernTheme.SurfaceColor;
            _iconLabel.ForeColor = ModernTheme.TextDisabled;
            _innerTextBox.BackColor = ModernTheme.SurfaceColor;
            _innerTextBox.ForeColor = ModernTheme.TextPrimary;
            Invalidate();
        }
    }
}
