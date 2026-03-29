using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace KoruMsSqlYedek.Win.Theme
{
    /// <summary>
    /// Modern progress bar — yuvarlatılmış köşeler, gradient dolgu, animasyonlu ilerleme.
    /// Yedekleme işlemleri sırasında kullanılır.
    /// </summary>
    internal class ModernProgressBar : Control
    {
        private int _value;
        private int _minimum;
        private int _maximum = 100;
        private Color _progressColor = ModernTheme.AccentPrimary;
        private Color _trackColor = ModernTheme.BorderColor;
        private bool _showPercentage = true;
        private int _radius = 6;
        private ProgressBarDisplayMode _displayMode = ProgressBarDisplayMode.Percentage;

        public ModernProgressBar()
        {
            SetStyle(
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.UserPaint |
                ControlStyles.DoubleBuffer |
                ControlStyles.ResizeRedraw |
                ControlStyles.SupportsTransparentBackColor,
                true);

            Size = new Size(300, 24);
            BackColor = Color.Transparent;
        }

        [Category("Modern")]
        public int Value
        {
            get => _value;
            set
            {
                _value = Math.Max(_minimum, Math.Min(_maximum, value));
                Invalidate();
            }
        }

        [Category("Modern")]
        public int Minimum
        {
            get => _minimum;
            set { _minimum = value; Invalidate(); }
        }

        [Category("Modern")]
        public int Maximum
        {
            get => _maximum;
            set { _maximum = Math.Max(1, value); Invalidate(); }
        }

        [Category("Modern"), Description("İlerleme rengi.")]
        public Color ProgressColor
        {
            get => _progressColor;
            set { _progressColor = value; Invalidate(); }
        }

        [Category("Modern"), Description("Yüzde gösterilsin mi?")]
        public bool ShowPercentage
        {
            get => _showPercentage;
            set { _showPercentage = value; Invalidate(); }
        }

        [Category("Modern"), Description("Gösterim modu.")]
        public ProgressBarDisplayMode DisplayMode
        {
            get => _displayMode;
            set { _displayMode = value; Invalidate(); }
        }

        [Category("Modern"), Description("Köşe yuvarlaklık yarıçapı.")]
        public int CornerRadius
        {
            get => _radius;
            set { _radius = Math.Max(0, value); Invalidate(); }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            ModernTheme.SetHighQuality(g);

            var trackRect = new Rectangle(0, 0, Width - 1, Height - 1);

            // Track (arka plan)
            using (var trackPath = ModernTheme.CreateRoundedRectanglePath(trackRect, _radius))
            using (var trackBrush = new SolidBrush(_trackColor))
            {
                g.FillPath(trackBrush, trackPath);
            }

            // Progress (dolgu)
            float percentage = (_maximum > _minimum)
                ? (float)(_value - _minimum) / (_maximum - _minimum)
                : 0f;

            if (percentage > 0)
            {
                int progressWidth = (int)(trackRect.Width * percentage);
                if (progressWidth < _radius * 2) progressWidth = _radius * 2;

                var progressRect = new Rectangle(trackRect.X, trackRect.Y, progressWidth, trackRect.Height);

                using (var progressPath = ModernTheme.CreateRoundedRectanglePath(progressRect, _radius))
                {
                    // Gradient dolgu
                    var lighterColor = ControlPaint.Light(_progressColor, 0.3f);
                    using (var gradBrush = new LinearGradientBrush(
                        progressRect, lighterColor, _progressColor, LinearGradientMode.Vertical))
                    {
                        g.FillPath(gradBrush, progressPath);
                    }
                }
            }

            // Metin
            if (_showPercentage && Height >= 16)
            {
                string text;
                switch (_displayMode)
                {
                    case ProgressBarDisplayMode.ValueOfMax:
                        text = $"{_value}/{_maximum}";
                        break;
                    case ProgressBarDisplayMode.CustomText:
                        text = Text;
                        break;
                    default:
                        text = $"{(int)(percentage * 100)}%";
                        break;
                }

                if (!string.IsNullOrEmpty(text))
                {
                    // Metin rengi — koyu arkaplan üzerinde beyaz, açık üzerinde koyu
                    var textColor = percentage > 0.5f ? ModernTheme.TextOnAccent : ModernTheme.TextPrimary;
                    using (var textBrush = new SolidBrush(textColor))
                    {
                        var sf = new StringFormat
                        {
                            Alignment = StringAlignment.Center,
                            LineAlignment = StringAlignment.Center
                        };
                        g.DrawString(text, ModernTheme.FontCaption, textBrush, trackRect, sf);
                    }
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

        /// <summary>
        /// Hata durumunda progress bar'ı kırmızıya çevirir.
        /// </summary>
        internal void SetError()
        {
            _progressColor = ModernTheme.StatusError;
            Invalidate();
        }

        /// <summary>
        /// Başarı durumunda progress bar'ı yeşile çevirir.
        /// </summary>
        internal void SetSuccess()
        {
            _progressColor = ModernTheme.StatusSuccess;
            Invalidate();
        }

        /// <summary>
        /// Varsayılan accent rengine döner.
        /// </summary>
        internal void ResetColor()
        {
            _progressColor = ModernTheme.AccentPrimary;
            Invalidate();
        }
    }

    internal enum ProgressBarDisplayMode
    {
        Percentage,
        ValueOfMax,
        CustomText
    }
}
