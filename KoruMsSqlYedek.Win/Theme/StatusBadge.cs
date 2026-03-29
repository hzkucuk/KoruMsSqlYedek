using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace KoruMsSqlYedek.Win.Theme
{
    /// <summary>
    /// Modern durum göstergesi — hap şeklinde renkli badge.
    /// Dashboard ve listelerde durum bilgisi göstermek için kullanılır.
    /// </summary>
    internal class StatusBadge : Control
    {
        private StatusBadgeType _badgeType = StatusBadgeType.Info;
        private string _badgeText = string.Empty;

        public StatusBadge()
        {
            SetStyle(
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.UserPaint |
                ControlStyles.DoubleBuffer |
                ControlStyles.ResizeRedraw |
                ControlStyles.SupportsTransparentBackColor,
                true);

            BackColor = Color.Transparent;
            Size = new Size(80, 22);
        }

        [Category("Modern"), Description("Badge tipi.")]
        public StatusBadgeType BadgeType
        {
            get => _badgeType;
            set { _badgeType = value; Invalidate(); }
        }

        [Category("Modern"), Description("Badge metin.")]
        public string BadgeText
        {
            get => _badgeText;
            set { _badgeText = value ?? string.Empty; Invalidate(); }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            ModernTheme.SetHighQuality(g);

            Color bgColor, fgColor;
            GetBadgeColors(out bgColor, out fgColor);

            // Hafif arka plan rengi (opaklık %15)
            var lightBg = Color.FromArgb(30, bgColor.R, bgColor.G, bgColor.B);

            var rect = new Rectangle(0, 0, Width - 1, Height - 1);

            using (var path = ModernTheme.CreateRoundedRectanglePath(rect, ModernTheme.BadgeRadius))
            {
                using (var fillBrush = new SolidBrush(lightBg))
                {
                    g.FillPath(fillBrush, path);
                }
            }

            // Metin
            var displayText = !string.IsNullOrEmpty(_badgeText) ? _badgeText : Text;
            if (!string.IsNullOrEmpty(displayText))
            {
                using (var textBrush = new SolidBrush(bgColor))
                {
                    var sf = new StringFormat
                    {
                        Alignment = StringAlignment.Center,
                        LineAlignment = StringAlignment.Center,
                        Trimming = StringTrimming.EllipsisCharacter
                    };
                    g.DrawString(displayText, ModernTheme.FontBadge, textBrush, rect, sf);
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

        private void GetBadgeColors(out Color bg, out Color fg)
        {
            switch (_badgeType)
            {
                case StatusBadgeType.Success:
                    bg = ModernTheme.StatusSuccess;
                    fg = ModernTheme.TextOnAccent;
                    break;
                case StatusBadgeType.Warning:
                    bg = ModernTheme.StatusWarning;
                    fg = ModernTheme.TextOnAccent;
                    break;
                case StatusBadgeType.Error:
                    bg = ModernTheme.StatusError;
                    fg = ModernTheme.TextOnAccent;
                    break;
                case StatusBadgeType.Cancelled:
                    bg = ModernTheme.StatusCancelled;
                    fg = ModernTheme.TextOnAccent;
                    break;
                default:
                    bg = ModernTheme.StatusInfo;
                    fg = ModernTheme.TextOnAccent;
                    break;
            }
        }

        /// <summary>
        /// Badge'ı verilen durum tipine ve metne göre günceller.
        /// </summary>
        internal void SetStatus(StatusBadgeType type, string text)
        {
            _badgeType = type;
            _badgeText = text ?? string.Empty;
            Invalidate();
        }
    }

    /// <summary>
    /// StatusBadge görsel tipi.
    /// </summary>
    internal enum StatusBadgeType
    {
        Info,
        Success,
        Warning,
        Error,
        Cancelled
    }
}
