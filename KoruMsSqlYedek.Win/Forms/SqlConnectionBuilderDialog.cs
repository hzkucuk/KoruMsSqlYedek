using System;
using System.Windows.Forms;
using KoruMsSqlYedek.Win.Theme;

namespace KoruMsSqlYedek.Win.Forms
{
    /// <summary>
    /// SQL Server DataSource bağlantı dizisi yapılandırıcısı.
    /// Sunucu/IP, Instance adı ve port bilgilerinden DataSource string üretir.
    /// </summary>
    internal partial class SqlConnectionBuilderDialog : ModernFormBase
    {
        /// <summary>
        /// Dialog kapatıldıktan sonra oluşturulan DataSource değeri.
        /// Örnek: "SUNUCU\SQLEXPRESS,1434" veya "192.168.1.10"
        /// </summary>
        public string DataSource { get; private set; } = string.Empty;

        public SqlConnectionBuilderDialog()
        {
            InitializeComponent();
            UpdatePreview();
        }

        /// <summary>
        /// Mevcut bir DataSource değerini parse ederek alanlara doldurur.
        /// </summary>
        public void LoadFromDataSource(string dataSource)
        {
            if (string.IsNullOrWhiteSpace(dataSource))
                return;

            // Port ayrıştırma: son virgül sonrası sayısal ise port
            string hostPart = dataSource;
            int commaIndex = dataSource.LastIndexOf(',');
            if (commaIndex >= 0 && int.TryParse(dataSource[(commaIndex + 1)..].Trim(), out int port))
            {
                if (port is >= 1 and <= 65535)
                {
                    _chkUsePort.Checked = true;
                    _nudPort.Value = port;
                    hostPart = dataSource[..commaIndex].Trim();
                }
            }

            // Instance ayrıştırma: ters bölü sonrası
            int backslashIndex = hostPart.IndexOf('\\');
            if (backslashIndex >= 0)
            {
                _txtHost.Text = hostPart[..backslashIndex].Trim();
                _txtInstance.Text = hostPart[(backslashIndex + 1)..].Trim();
            }
            else
            {
                _txtHost.Text = hostPart.Trim();
                _txtInstance.Text = string.Empty;
            }

            UpdatePreview();
        }

        private void OnFieldChanged(object? sender, EventArgs e) => UpdatePreview();

        private void OnUsePortChanged(object? sender, EventArgs e)
        {
            _lblPort.Visible = _chkUsePort.Checked;
            _nudPort.Visible = _chkUsePort.Checked;
            UpdatePreview();
        }

        private void OnOkClick(object? sender, EventArgs e)
        {
            DataSource = BuildDataSource();
            if (string.IsNullOrWhiteSpace(DataSource))
            {
                ModernMessageBox.Show(
                    "Sunucu adı veya IP adresi boş olamaz.",
                    "Eksik Bilgi",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                DialogResult = DialogResult.None;
            }
        }

        private void UpdatePreview() => _txtPreview.Text = BuildDataSource();

        private string BuildDataSource()
        {
            string host = _txtHost.Text.Trim();
            if (string.IsNullOrWhiteSpace(host))
                return string.Empty;

            string instance = _txtInstance.Text.Trim();

            // Sunucu\Instance
            string dataSource = string.IsNullOrWhiteSpace(instance)
                ? host
                : $"{host}\\{instance}";

            // Port sadece instance olmadığında anlamlıdır; instance varsa SMO protokolüyle çözülür.
            // Ancak bazı senaryolarda instance+port birlikte kullanılabilir (named instance + statik port).
            if (_chkUsePort.Checked)
                dataSource += $",{(int)_nudPort.Value}";

            return dataSource;
        }
    }
}
