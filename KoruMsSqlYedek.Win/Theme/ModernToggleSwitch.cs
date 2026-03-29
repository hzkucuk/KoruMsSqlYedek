using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace KoruMsSqlYedek.Win.Theme
{
    /// <summary>
    /// Modern toggle switch — iOS/Android tarzı on/off anahtar.
    /// CheckBox yerine kullanılarak modern bir görünüm sağlar.
    /// </summary>
    internal class ModernToggleSwitch : Control
    {
        private bool _isChecked;
        private bool _isHovered;
        private float _animationProgress; // 0.0 = off, 1.0 = on
        private readonly Timer _animTimer;

        private Color _onColor = ModernTheme.AccentPrimary;
        private Color _offColor = Color.FromArgb(70, 70, 78);
        private string _onText = "ON";
        private string _offText = "OFF";
        private bool _showText = true;

        public ModernToggleSwitch()
        {
            SetStyle(
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.UserPaint |
                ControlStyles.DoubleBuffer |
                ControlStyles.ResizeRedraw |
                ControlStyles.SupportsTransparentBackColor,
                true);

            Size = new Size(48, 24);
            Cursor = Cursors.Hand;
            BackColor = Color.Transparent;

            _animTimer = new Timer { Interval = 16 }; // ~60fps
            _animTimer.Tick += OnAnimationTick;
        }

        [Category("Modern")]
        public bool Checked
        {
            get => _isChecked;
            set
            {
                if (_isChecked == value) return;
                _isChecked = value;
                _animTimer.Start();
                CheckedChanged?.Invoke(this, EventArgs.Empty);
                Invalidate();
            }
        }

        [Category("Modern"), Description("Açık rengi.")]
        public Color OnColor
        {
            get => _onColor;
            set { _onColor = value; Invalidate(); }
        }

        [Category("Modern"), Description("Kapalı rengi.")]
        public Color OffColor
        {
            get => _offColor;
            set { _offColor = value; Invalidate(); }
        }

        [Category("Modern"), Description("Açık durum metni.")]
        public string OnText
        {
            get => _onText;
            set { _onText = value ?? string.Empty; Invalidate(); }
        }

        [Category("Modern"), Description("Kapalı durum metni.")]
        public string OffText
        {
            get => _offText;
            set { _offText = value ?? string.Empty; Invalidate(); }
        }

        [Category("Modern"), Description("Metin gösterilsin mi?")]
        public bool ShowText
        {
            get => _showText;
            set { _showText = value; Invalidate(); }
        }

        public event EventHandler CheckedChanged;

        protected override void OnMouseClick(MouseEventArgs e)
        {
            base.OnMouseClick(e);
            Checked = !Checked;
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            _isHovered = true;
            Invalidate();
            base.OnMouseEnter(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            _isHovered = false;
            Invalidate();
            base.OnMouseLeave(e);
        }

        private void OnAnimationTick(object sender, EventArgs e)
        {
            float target = _isChecked ? 1.0f : 0.0f;
            float step = 0.15f;

            if (Math.Abs(_animationProgress - target) < step)
            {
                _animationProgress = target;
                _animTimer.Stop();
            }
            else
            {
                _animationProgress += _isChecked ? step : -step;
            }

            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            ModernTheme.SetHighQuality(g);

            int switchWidth = 44;
            int switchHeight = 22;
            int knobSize = 16;
            int knobPadding = 3;

            var trackRect = new Rectangle(0, (Height - switchHeight) / 2, switchWidth, switchHeight);

            // Track rengi (animasyonlu geçiş)
            var trackColor = InterpolateColor(_offColor, _onColor, _animationProgress);
            if (_isHovered)
            {
                trackColor = _isChecked
                    ? ModernTheme.AccentPrimaryHover
                    : Color.FromArgb(85, 85, 95);
            }

            // Track çiz
            using (var trackPath = ModernTheme.CreateRoundedRectanglePath(trackRect, switchHeight / 2))
            using (var trackBrush = new SolidBrush(trackColor))
            {
                g.FillPath(trackBrush, trackPath);
            }

            // Knob pozisyonu (animasyonlu)
            float knobMinX = trackRect.X + knobPadding;
            float knobMaxX = trackRect.Right - knobSize - knobPadding;
            float knobX = knobMinX + (knobMaxX - knobMinX) * _animationProgress;
            float knobY = trackRect.Y + (trackRect.Height - knobSize) / 2f;

            // Knob gölge
            using (var shadowBrush = new SolidBrush(Color.FromArgb(30, 0, 0, 0)))
            {
                g.FillEllipse(shadowBrush, knobX + 1, knobY + 1, knobSize, knobSize);
            }

            // Knob
            using (var knobBrush = new SolidBrush(Color.White))
            {
                g.FillEllipse(knobBrush, knobX, knobY, knobSize, knobSize);
            }

            // Metin (toggle sağında)
            if (_showText)
            {
                string label = _isChecked ? _onText : _offText;
                var textColor = _isChecked ? _onColor : ModernTheme.TextSecondary;
                using (var textBrush = new SolidBrush(textColor))
                {
                    var textRect = new RectangleF(switchWidth + 8, 0, Width - switchWidth - 8, Height);
                    var sf = new StringFormat { LineAlignment = StringAlignment.Center };
                    g.DrawString(label, ModernTheme.FontCaption, textBrush, textRect, sf);
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

        private static Color InterpolateColor(Color from, Color to, float t)
        {
            t = Math.Max(0, Math.Min(1, t));
            int r = (int)(from.R + (to.R - from.R) * t);
            int g = (int)(from.G + (to.G - from.G) * t);
            int b = (int)(from.B + (to.B - from.B) * t);
            return Color.FromArgb(r, g, b);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _animTimer.Stop();
                _animTimer.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
