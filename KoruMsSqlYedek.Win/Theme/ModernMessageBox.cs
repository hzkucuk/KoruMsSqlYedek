using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace KoruMsSqlYedek.Win.Theme
{
    /// <summary>
    /// Dark tema destekli MessageBox alternatifi.
    /// Standart MessageBox.Show() imzalarıyla uyumlu statik metodlar sunar.
    /// ModernFormBase'den türer, her zaman uygulamanın tema renklerini kullanır.
    /// </summary>
    internal sealed class ModernMessageBox : ModernFormBase
    {
        private const int IconAreaWidth = 64;
        private const int ContentPadding = 20;
        private const int ButtonHeight = 36;
        private const int ButtonWidth = 100;
        private const int ButtonSpacing = 10;
        private const int MinFormWidth = 380;
        private const int MaxFormWidth = 520;
        private const int IconSize = 36;

        private readonly Label _lblMessage;
        private readonly PictureBox _picIcon;
        private readonly FlowLayoutPanel _flpButtons;
        private readonly Panel _pnlButtonBar;

        private ModernMessageBox(string text, string caption, MessageBoxButtons buttons, MessageBoxIcon icon)
        {
            Text = caption ?? string.Empty;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.CenterParent;
            Padding = new Padding(0);

            // İkon paneli
            _picIcon = new PictureBox
            {
                Size = new Size(IconAreaWidth, IconSize),
                SizeMode = PictureBoxSizeMode.CenterImage,
                BackColor = Color.Transparent,
                Image = GetIconImage(icon),
                Dock = DockStyle.None,
                Location = new Point(ContentPadding, ContentPadding + 4)
            };

            // Mesaj metni
            _lblMessage = new Label
            {
                Text = text ?? string.Empty,
                ForeColor = ModernTheme.TextPrimary,
                Font = ModernTheme.FontBody,
                AutoSize = false,
                MaximumSize = new Size(MaxFormWidth - IconAreaWidth - ContentPadding * 2, 0),
                AutoEllipsis = false,
                BackColor = Color.Transparent,
                TextAlign = ContentAlignment.MiddleLeft
            };

            // Mesaj boyutunu hesapla
            var textSize = TextRenderer.MeasureText(
                _lblMessage.Text,
                _lblMessage.Font,
                new Size(_lblMessage.MaximumSize.Width, 0),
                TextFormatFlags.WordBreak);

            int textHeight = Math.Max(textSize.Height, IconSize);
            int textWidth = Math.Max(textSize.Width, 200);

            _lblMessage.Size = new Size(textWidth, textHeight);
            _lblMessage.Location = new Point(
                ContentPadding + IconAreaWidth,
                ContentPadding + (textHeight > IconSize ? 0 : (IconSize - textHeight) / 2));

            // Buton çubuğu
            _pnlButtonBar = new Panel
            {
                Height = ButtonHeight + ContentPadding * 2,
                Dock = DockStyle.Bottom,
                BackColor = ModernTheme.SurfaceColor,
                Padding = new Padding(0, 8, ContentPadding, 8)
            };

            _flpButtons = new FlowLayoutPanel
            {
                Dock = DockStyle.Right,
                AutoSize = true,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                BackColor = Color.Transparent,
                Padding = new Padding(0)
            };

            CreateButtons(buttons);
            _pnlButtonBar.Controls.Add(_flpButtons);

            // Ayırıcı çizgi
            var divider = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 1,
                BackColor = ModernTheme.DividerColor
            };

            // Form boyutları
            int formWidth = Math.Max(MinFormWidth,
                ContentPadding + IconAreaWidth + textWidth + ContentPadding + 20);
            formWidth = Math.Min(formWidth, MaxFormWidth);
            int contentHeight = ContentPadding + textHeight + ContentPadding;
            int formHeight = contentHeight + 1 + _pnlButtonBar.Height;

            ClientSize = new Size(formWidth, formHeight);

            Controls.Add(_picIcon);
            Controls.Add(_lblMessage);
            Controls.Add(divider);
            Controls.Add(_pnlButtonBar);
        }

        private void CreateButtons(MessageBoxButtons buttons)
        {
            switch (buttons)
            {
                case MessageBoxButtons.OK:
                    AddButton("Tamam", DialogResult.OK, ModernButtonStyle.Primary, true);
                    break;
                case MessageBoxButtons.OKCancel:
                    AddButton("Tamam", DialogResult.OK, ModernButtonStyle.Primary, true);
                    AddButton("İptal", DialogResult.Cancel, ModernButtonStyle.Secondary, false);
                    break;
                case MessageBoxButtons.YesNo:
                    AddButton("Evet", DialogResult.Yes, ModernButtonStyle.Primary, true);
                    AddButton("Hayır", DialogResult.No, ModernButtonStyle.Secondary, false);
                    break;
                case MessageBoxButtons.YesNoCancel:
                    AddButton("Evet", DialogResult.Yes, ModernButtonStyle.Primary, true);
                    AddButton("Hayır", DialogResult.No, ModernButtonStyle.Secondary, false);
                    AddButton("İptal", DialogResult.Cancel, ModernButtonStyle.Ghost, false);
                    break;
                case MessageBoxButtons.RetryCancel:
                    AddButton("Tekrar Dene", DialogResult.Retry, ModernButtonStyle.Primary, true);
                    AddButton("İptal", DialogResult.Cancel, ModernButtonStyle.Secondary, false);
                    break;
            }
        }

        private void AddButton(string text, DialogResult result, ModernButtonStyle style, bool isAccept)
        {
            var btn = new ModernButton
            {
                Text = text,
                DialogResult = result,
                ButtonStyle = style,
                Size = new Size(ButtonWidth, ButtonHeight),
                Margin = new Padding(ButtonSpacing / 2, 0, ButtonSpacing / 2, 0)
            };

            _flpButtons.Controls.Add(btn);

            if (isAccept)
                AcceptButton = btn;

            if (result == DialogResult.Cancel || result == DialogResult.No)
                CancelButton = btn;
        }

        private static Bitmap GetIconImage(MessageBoxIcon icon)
        {
            switch (icon)
            {
                case MessageBoxIcon.Error:
                    return PhosphorIcons.Render(PhosphorIcons.XCircle, ModernTheme.StatusError, IconSize);
                case MessageBoxIcon.Warning:
                    return PhosphorIcons.Render(PhosphorIcons.Warning, ModernTheme.StatusWarning, IconSize);
                case MessageBoxIcon.Information:
                    return PhosphorIcons.Render(PhosphorIcons.Info, ModernTheme.StatusInfo, IconSize);
                case MessageBoxIcon.Question:
                    return PhosphorIcons.Render(PhosphorIcons.Info, ModernTheme.AccentPrimary, IconSize);
                default:
                    return PhosphorIcons.Render(PhosphorIcons.Info, ModernTheme.TextSecondary, IconSize);
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            // İçerik alanı arka planı
            using (var brush = new SolidBrush(BackColor))
            {
                e.Graphics.FillRectangle(brush, 0, 0, Width, Height - _pnlButtonBar.Height - 1);
            }
        }

        // ═══════════════ STATIC API ═══════════════

        /// <summary>Mesaj gösterir (sadece OK).</summary>
        public static DialogResult Show(string text)
        {
            return Show(text, string.Empty, MessageBoxButtons.OK, MessageBoxIcon.None);
        }

        /// <summary>Mesaj gösterir (başlık ile).</summary>
        public static DialogResult Show(string text, string caption)
        {
            return Show(text, caption, MessageBoxButtons.OK, MessageBoxIcon.None);
        }

        /// <summary>Mesaj gösterir (butonlar ile).</summary>
        public static DialogResult Show(string text, string caption, MessageBoxButtons buttons)
        {
            return Show(text, caption, buttons, MessageBoxIcon.None);
        }

        /// <summary>Mesaj gösterir (tam parametre).</summary>
        public static DialogResult Show(string text, string caption,
            MessageBoxButtons buttons, MessageBoxIcon icon)
        {
            using (var dlg = new ModernMessageBox(text, caption, buttons, icon))
            {
                return dlg.ShowDialog();
            }
        }

        /// <summary>Mesaj gösterir (owner ile).</summary>
        public static DialogResult Show(IWin32Window owner, string text, string caption,
            MessageBoxButtons buttons, MessageBoxIcon icon)
        {
            using (var dlg = new ModernMessageBox(text, caption, buttons, icon))
            {
                return dlg.ShowDialog(owner);
            }
        }

        /// <summary>Mesaj gösterir (owner + defaultButton).</summary>
        public static DialogResult Show(IWin32Window owner, string text, string caption,
            MessageBoxButtons buttons, MessageBoxIcon icon,
            MessageBoxDefaultButton defaultButton)
        {
            using (var dlg = new ModernMessageBox(text, caption, buttons, icon))
            {
                return dlg.ShowDialog(owner);
            }
        }

        /// <summary>Mesaj gösterir (defaultButton ile).</summary>
        public static DialogResult Show(string text, string caption,
            MessageBoxButtons buttons, MessageBoxIcon icon,
            MessageBoxDefaultButton defaultButton)
        {
            using (var dlg = new ModernMessageBox(text, caption, buttons, icon))
            {
                return dlg.ShowDialog();
            }
        }
    }
}
