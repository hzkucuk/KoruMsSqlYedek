using System;
using System.Drawing;
using System.Windows.Forms;

namespace KoruMsSqlYedek.Win.Theme
{
    /// <summary>
    /// DataGridView satırı içinde çubuk+yüzde gösteren özel sütun.
    /// Aktif yedekleme sırasında plan bazlı ilerlemeyi görselleştirir.
    /// Değer 0 veya null olduğunda hiçbir şey çizmez.
    /// </summary>
    internal class DataGridViewProgressBarColumn : DataGridViewColumn
    {
        public DataGridViewProgressBarColumn() : base(new DataGridViewProgressBarCell())
        {
            ReadOnly = true;
        }
    }

    internal class DataGridViewProgressBarCell : DataGridViewTextBoxCell
    {
        protected override void Paint(
            Graphics graphics,
            Rectangle clipBounds,
            Rectangle cellBounds,
            int rowIndex,
            DataGridViewElementStates cellState,
            object value,
            object formattedValue,
            string errorText,
            DataGridViewCellStyle cellStyle,
            DataGridViewAdvancedBorderStyle advancedBorderStyle,
            DataGridViewPaintParts paintParts)
        {
            // Arka plan + kenarlık çiz
            base.Paint(graphics, clipBounds, cellBounds, rowIndex, cellState,
                value, formattedValue, errorText, cellStyle, advancedBorderStyle,
                DataGridViewPaintParts.Background | DataGridViewPaintParts.Border);

            int pct = 0;
            if (value is int intVal)
                pct = Math.Max(0, Math.Min(100, intVal));

            if (pct <= 0)
                return;

            var inner = new Rectangle(
                cellBounds.X + 6,
                cellBounds.Y + 5,
                cellBounds.Width - 12,
                cellBounds.Height - 10);

            if (inner.Width <= 4 || inner.Height <= 4)
                return;

            // Track
            using (var trackBrush = new SolidBrush(ModernTheme.BorderColor))
                graphics.FillRectangle(trackBrush, inner);

            // Filled portion
            int filledWidth = (int)(inner.Width * pct / 100.0);
            if (filledWidth > 0)
            {
                var fillRect = new Rectangle(inner.X, inner.Y, filledWidth, inner.Height);
                using (var fillBrush = new SolidBrush(ModernTheme.AccentPrimary))
                    graphics.FillRectangle(fillBrush, fillRect);
            }

            // Percentage text
            var textColor = pct > 55 ? ModernTheme.TextOnAccent : ModernTheme.TextPrimary;
            using (var textBrush = new SolidBrush(textColor))
            using (var sf = new StringFormat
            {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Center
            })
            {
                graphics.DrawString(pct + "%", ModernTheme.FontCaption, textBrush, inner, sf);
            }
        }
    }
}
