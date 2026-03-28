using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace MikroSqlDbYedek.Win.Theme
{
    /// <summary>
    /// Modern Loading Overlay — yarı saydam arka plan üzerinde dönen spinner ve durum mesajı.
    /// Form veya Panel üzerine eklenerek uzun işlemler sırasında gösterilir.
    /// </summary>
    internal class ModernLoadingOverlay : UserControl
    {
        private float _angle;
        private string _message = "Yükleniyor...";
        private readonly Timer _spinTimer;
        private int _overlayAlpha = 200;

        public ModernLoadingOverlay()
        {
            SetStyle(
                ControlStyles.UserPaint |
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.SupportsTransparentBackColor,
                true);

            BackColor = Color.Transparent;
            Dock = DockStyle.Fill;
            Visible = false;
            Font = ModernTheme.FontBody;

            _spinTimer = new Timer { Interval = 30 };
            _spinTimer.Tick += (s, e) =>
            {
                _angle = (_angle + 6f) % 360f;
                Invalidate();
            };
        }

        /// <summary>Overlay üzerinde gösterilecek mesaj.</summary>
        [Category("Modern"), Description("Spinner altında gösterilecek durum mesajı.")]
        [DefaultValue("Yükleniyor...")]
        public string Message
        {
            get => _message;
            set { _message = value; Invalidate(); }
        }

        /// <summary>Arka plan saydamlık (0-255).</summary>
        [Category("Modern"), Description("Arka plan opaklığı (0=saydam, 255=opak).")]
        [DefaultValue(200)]
        public int OverlayAlpha
        {
            get => _overlayAlpha;
            set { _overlayAlpha = Math.Max(0, Math.Min(255, value)); Invalidate(); }
        }

        /// <summary>Overlay'i gösterir ve spinner'ı başlatır.</summary>
        public void ShowOverlay(string message = null)
        {
            if (message != null) _message = message;
            BringToFront();
            Visible = true;
            _spinTimer.Start();
        }

        /// <summary>Overlay'i gizler ve spinner'ı durdurur.</summary>
        public void HideOverlay()
        {
            _spinTimer.Stop();
            Visible = false;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            ModernTheme.SetHighQuality(g);

            // Yarı saydam arka plan
            using (var bgBrush = new SolidBrush(Color.FromArgb(_overlayAlpha, 245, 245, 248)))
            {
                g.FillRectangle(bgBrush, ClientRectangle);
            }

            int centerX = Width / 2;
            int centerY = Height / 2 - 20;
            int spinnerRadius = 24;
            int dotCount = 12;
            int dotRadius = 4;

            // Dönen noktalar
            for (int i = 0; i < dotCount; i++)
            {
                float dotAngle = _angle + (i * (360f / dotCount));
                double rad = dotAngle * Math.PI / 180.0;

                int x = centerX + (int)(spinnerRadius * Math.Cos(rad));
                int y = centerY + (int)(spinnerRadius * Math.Sin(rad));

                // Opaklık gradyanı — arkadaki noktalar soluk
                int alpha = (int)(255 * ((float)i / dotCount));
                Color dotColor = Color.FromArgb(alpha, ModernTheme.AccentPrimary);

                using (var brush = new SolidBrush(dotColor))
                {
                    g.FillEllipse(brush,
                        x - dotRadius, y - dotRadius,
                        dotRadius * 2, dotRadius * 2);
                }
            }

            // Mesaj metni
            if (!string.IsNullOrEmpty(_message))
            {
                SizeF textSize = g.MeasureString(_message, Font);
                float textX = centerX - textSize.Width / 2;
                float textY = centerY + spinnerRadius + 20;

                using (var brush = new SolidBrush(ModernTheme.TextSecondary))
                {
                    g.DrawString(_message, Font, brush, textX, textY);
                }
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _spinTimer?.Stop();
                _spinTimer?.Dispose();
            }
            base.Dispose(disposing);
        }

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= 0x20; // WS_EX_TRANSPARENT
                return cp;
            }
        }
    }
}
