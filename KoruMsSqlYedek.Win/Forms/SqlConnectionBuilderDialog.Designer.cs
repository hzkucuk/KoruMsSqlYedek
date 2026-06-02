namespace KoruMsSqlYedek.Win.Forms
{
    partial class SqlConnectionBuilderDialog
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();
            base.Dispose(disposing);
        }

        #region Windows Form Designer üretilen kod

        private void InitializeComponent()
        {
            _lblHost = new System.Windows.Forms.Label();
            _txtHost = new System.Windows.Forms.TextBox();
            _lblInstance = new System.Windows.Forms.Label();
            _txtInstance = new System.Windows.Forms.TextBox();
            _lblPort = new System.Windows.Forms.Label();
            _nudPort = new KoruMsSqlYedek.Win.Theme.ModernNumericUpDown();
            _chkUsePort = new System.Windows.Forms.CheckBox();
            _lblPreviewCaption = new System.Windows.Forms.Label();
            _txtPreview = new System.Windows.Forms.TextBox();
            _btnOk = new KoruMsSqlYedek.Win.Theme.ModernButton();
            _btnCancel = new KoruMsSqlYedek.Win.Theme.ModernButton();
            _lblHint = new System.Windows.Forms.Label();

            SuspendLayout();

            // _lblHost
            _lblHost.Text = "Sunucu / IP Adresi:";
            _lblHost.Name = "_lblHost";
            _lblHost.AutoSize = true;
            _lblHost.Location = new System.Drawing.Point(16, 20);

            // _txtHost
            _txtHost.Location = new System.Drawing.Point(180, 17);
            _txtHost.Name = "_txtHost";
            _txtHost.Size = new System.Drawing.Size(260, 23);
            _txtHost.TextChanged += OnFieldChanged;

            // _lblInstance
            _lblInstance.Text = "Instance Ad\u0131 (iste\u011fe ba\u011fl\u0131):";
            _lblInstance.Name = "_lblInstance";
            _lblInstance.AutoSize = true;
            _lblInstance.Location = new System.Drawing.Point(16, 55);

            // _txtInstance
            _txtInstance.Location = new System.Drawing.Point(180, 52);
            _txtInstance.Name = "_txtInstance";
            _txtInstance.Size = new System.Drawing.Size(260, 23);
            _txtInstance.TextChanged += OnFieldChanged;

            // _chkUsePort
            _chkUsePort.Text = "Farkl\u0131 Port Kullan:";
            _chkUsePort.Name = "_chkUsePort";
            _chkUsePort.Location = new System.Drawing.Point(16, 90);
            _chkUsePort.AutoSize = true;
            _chkUsePort.CheckedChanged += OnUsePortChanged;

            // _lblPort
            _lblPort.Text = "Port:";
            _lblPort.Name = "_lblPort";
            _lblPort.AutoSize = true;
            _lblPort.Location = new System.Drawing.Point(180, 90);
            _lblPort.Visible = false;

            // _nudPort
            _nudPort.Location = new System.Drawing.Point(220, 87);
            _nudPort.Name = "_nudPort";
            _nudPort.Size = new System.Drawing.Size(80, 23);
            _nudPort.Minimum = 1;
            _nudPort.Maximum = 65535;
            _nudPort.Value = 1433;
            _nudPort.Visible = false;
            _nudPort.ValueChanged += OnFieldChanged;

            // _lblPreviewCaption
            _lblPreviewCaption.Text = "Olu\u015fturulan Ba\u011flant\u0131 Dizisi:";
            _lblPreviewCaption.Name = "_lblPreviewCaption";
            _lblPreviewCaption.AutoSize = true;
            _lblPreviewCaption.Location = new System.Drawing.Point(16, 130);
            _lblPreviewCaption.Font = new System.Drawing.Font("Segoe UI", 8.25F, System.Drawing.FontStyle.Bold);

            // _txtPreview
            _txtPreview.Location = new System.Drawing.Point(16, 150);
            _txtPreview.Name = "_txtPreview";
            _txtPreview.Size = new System.Drawing.Size(424, 23);
            _txtPreview.ReadOnly = true;
            _txtPreview.TabStop = false;
            _txtPreview.Tag = "surface";

            // _lblHint
            _lblHint.Text = "\u00d6rnekler: localhost \u2022 192.168.1.10 \u2022 SUNUCU\\SQLEXPRESS \u2022 sunucu.local,1434";
            _lblHint.Name = "_lblHint";
            _lblHint.AutoSize = true;
            _lblHint.Location = new System.Drawing.Point(16, 180);
            _lblHint.Font = new System.Drawing.Font("Segoe UI", 7.5F);
            _lblHint.Tag = "secondary";

            // _btnOk
            _btnOk.Text = "Tamam";
            _btnOk.Name = "_btnOk";
            _btnOk.ButtonStyle = Theme.ModernButtonStyle.Primary;
            _btnOk.Size = new System.Drawing.Size(100, 30);
            _btnOk.Location = new System.Drawing.Point(234, 210);
            _btnOk.DialogResult = System.Windows.Forms.DialogResult.OK;
            _btnOk.Click += OnOkClick;

            // _btnCancel
            _btnCancel.Text = "\u0130ptal";
            _btnCancel.Name = "_btnCancel";
            _btnCancel.ButtonStyle = Theme.ModernButtonStyle.Secondary;
            _btnCancel.Size = new System.Drawing.Size(100, 30);
            _btnCancel.Location = new System.Drawing.Point(340, 210);
            _btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;

            // Form
            AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            ClientSize = new System.Drawing.Size(456, 256);
            Controls.Add(_lblHost);
            Controls.Add(_txtHost);
            Controls.Add(_lblInstance);
            Controls.Add(_txtInstance);
            Controls.Add(_chkUsePort);
            Controls.Add(_lblPort);
            Controls.Add(_nudPort);
            Controls.Add(_lblPreviewCaption);
            Controls.Add(_txtPreview);
            Controls.Add(_lblHint);
            Controls.Add(_btnOk);
            Controls.Add(_btnCancel);
            FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "SqlConnectionBuilderDialog";
            Text = "SQL Server Ba\u011flant\u0131 Yap\u0131land\u0131r\u0131c\u0131s\u0131";
            StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            AcceptButton = _btnOk;
            CancelButton = _btnCancel;

            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private System.Windows.Forms.Label _lblHost;
        private System.Windows.Forms.TextBox _txtHost;
        private System.Windows.Forms.Label _lblInstance;
        private System.Windows.Forms.TextBox _txtInstance;
        private System.Windows.Forms.CheckBox _chkUsePort;
        private System.Windows.Forms.Label _lblPort;
        private KoruMsSqlYedek.Win.Theme.ModernNumericUpDown _nudPort;
        private System.Windows.Forms.Label _lblPreviewCaption;
        private System.Windows.Forms.TextBox _txtPreview;
        private System.Windows.Forms.Label _lblHint;
        private KoruMsSqlYedek.Win.Theme.ModernButton _btnOk;
        private KoruMsSqlYedek.Win.Theme.ModernButton _btnCancel;
    }
}
