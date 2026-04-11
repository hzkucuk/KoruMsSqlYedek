using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace KoruMsSqlYedek.Win.Theme
{
    /// <summary>
    /// Modern TextBox — focus kenarlığı, placeholder (watermark) desteği, yuvarlatılmış köşeler.
    /// Standart TextBox'ın görsel olarak geliştirilmiş versiyonu.
    /// </summary>
    internal class ModernTextBox : UserControl
    {
        private readonly TextBox _innerTextBox;
        private string _placeholder = string.Empty;
        private Color _borderColor = ModernTheme.BorderColor;
        private Color _focusBorderColor = ModernTheme.AccentPrimary;
        private bool _isFocused;
        private int _radius = 4;
        private bool _isPassword;

        public ModernTextBox()
        {
            SetStyle(
                ControlStyles.UserPaint |
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.DoubleBuffer |
                ControlStyles.ResizeRedraw |
                ControlStyles.SupportsTransparentBackColor,
                true);

            Size = new Size(250, 32);
            Padding = new Padding(8, 4, 8, 4);
            BackColor = ModernTheme.SurfaceColor;

            _innerTextBox = new TextBox
            {
                BorderStyle = BorderStyle.None,
                BackColor = ModernTheme.SurfaceColor,
                ForeColor = ModernTheme.TextPrimary,
                Font = ModernTheme.FontBody,
                Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top | AnchorStyles.Bottom
            };

            _innerTextBox.GotFocus += (s, e) => { _isFocused = true; Invalidate(); };
            _innerTextBox.LostFocus += (s, e) => { _isFocused = false; Invalidate(); };
            _innerTextBox.TextChanged += (s, e) => { OnTextChanged(e); Invalidate(); };

            Controls.Add(_innerTextBox);
            PositionInnerTextBox();
        }

        [Category("Modern"), Description("Placeholder / watermark metni.")]
        public string Placeholder
        {
            get => _placeholder;
            set { _placeholder = value ?? string.Empty; Invalidate(); }
        }

        [Category("Modern"), Description("Kenarlık rengi.")]
        public Color BorderColor
        {
            get => _borderColor;
            set { _borderColor = value; Invalidate(); }
        }

        [Category("Modern"), Description("Focus kenarlık rengi.")]
        public Color FocusBorderColor
        {
            get => _focusBorderColor;
            set { _focusBorderColor = value; Invalidate(); }
        }

        [Category("Modern"), Description("Köşe yuvarlaklık yarıçapı.")]
        public int CornerRadius
        {
            get => _radius;
            set { _radius = Math.Max(0, value); Invalidate(); }
        }

        [Category("Modern"), Description("Şifre modu.")]
        public bool IsPassword
        {
            get => _isPassword;
            set
            {
                _isPassword = value;
                _innerTextBox.UseSystemPasswordChar = value;
            }
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

        [Browsable(true)]
        public bool ReadOnly
        {
            get => _innerTextBox.ReadOnly;
            set => _innerTextBox.ReadOnly = value;
        }

        [Browsable(true)]
        public int MaxLength
        {
            get => _innerTextBox.MaxLength;
            set => _innerTextBox.MaxLength = value;
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            PositionInnerTextBox();
        }

        private void PositionInnerTextBox()
        {
            if (_innerTextBox is null)
                return;

            int x = _radius + 6;
            int y = (Height - _innerTextBox.PreferredHeight) / 2;
            _innerTextBox.Location = new Point(x, Math.Max(2, y));
            _innerTextBox.Width = Width - x * 2;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            ModernTheme.SetHighQuality(g);

            var rect = new Rectangle(0, 0, Width - 1, Height - 1);
            var borderClr = _isFocused ? _focusBorderColor : _borderColor;
            float borderWidth = _isFocused ? 1.5f : 1f;

            // Arkaplan
            using (var path = ModernTheme.CreateRoundedRectanglePath(rect, _radius))
            {
                using (var bgBrush = new SolidBrush(Enabled ? ModernTheme.SurfaceColor : ModernTheme.BackgroundColor))
                {
                    g.FillPath(bgBrush, path);
                }

                using (var borderPen = new Pen(borderClr, borderWidth))
                {
                    g.DrawPath(borderPen, path);
                }
            }

            // Placeholder
            if (string.IsNullOrEmpty(_innerTextBox.Text) && !_isFocused && !string.IsNullOrEmpty(_placeholder))
            {
                using (var phBrush = new SolidBrush(ModernTheme.TextDisabled))
                {
                    var phRect = new RectangleF(_radius + 6, (Height - ModernTheme.FontBody.Height) / 2f, Width - _radius * 2 - 12, Height);
                    g.DrawString(_placeholder, ModernTheme.FontBody, phBrush, phRect);
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

        /// <summary>Inner TextBox'a focus verir.</summary>
        public new void Focus()
        {
            _innerTextBox.Focus();
        }

        /// <summary>Metin seçer.</summary>
        public void SelectAll()
        {
            _innerTextBox.SelectAll();
        }

        /// <summary>Tema değişikliğinde cache'lenmiş renkleri günceller.</summary>
        internal void RefreshThemeColors()
        {
            _borderColor = ModernTheme.BorderColor;
            _focusBorderColor = ModernTheme.AccentPrimary;
            BackColor = ModernTheme.SurfaceColor;
            _innerTextBox.BackColor = ModernTheme.SurfaceColor;
            _innerTextBox.ForeColor = ModernTheme.TextPrimary;
            Invalidate();
        }
    }
}
