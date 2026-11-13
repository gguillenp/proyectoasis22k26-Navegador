/* Área: Contabilidad. Autor: Oskar Cermeño. Carné: 0901-23-15379.
 * Fecha: 11/11/2026. Propósito: presentación de los filtros y del visor del reporte de pólizas. */
namespace CapaVista_Contabilidad
{
    partial class FrmReportePolizas
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool Disposing)
        {
            if (Disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(Disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.ContabilidadTtpBotones = new System.Windows.Forms.ToolTip(this.components);
            this.ContabilidadPnlFiltros = new CapaVista_Contabilidad.Componentes.ClsPanelRedondeadoContabilidad();
            this.ContabilidadLblTitulo = new System.Windows.Forms.Label();
            this.ContabilidadLblResultado = new System.Windows.Forms.Label();
            this.ContabilidadLblDesde = new System.Windows.Forms.Label();
            this.ContabilidadDtpDesde = new System.Windows.Forms.DateTimePicker();
            this.ContabilidadLblHasta = new System.Windows.Forms.Label();
            this.ContabilidadDtpHasta = new System.Windows.Forms.DateTimePicker();
            this.ContabilidadLblEstado = new System.Windows.Forms.Label();
            this.ContabilidadCboEstado = new System.Windows.Forms.ComboBox();
            this.ContabilidadLblModulo = new System.Windows.Forms.Label();
            this.ContabilidadCboModulo = new System.Windows.Forms.ComboBox();
            this.ContabilidadLblPais = new System.Windows.Forms.Label();
            this.ContabilidadCboPais = new System.Windows.Forms.ComboBox();
            this.ContabilidadBtnGenerar = new System.Windows.Forms.Button();
            this.ContabilidadRpvReporte = new Microsoft.Reporting.WinForms.ReportViewer();
            this.ContabilidadPnlFiltros.SuspendLayout();
            this.SuspendLayout();
            this.ContabilidadPnlFiltros.BackColor = System.Drawing.Color.Transparent;
            this.ContabilidadPnlFiltros.Controls.Add(this.ContabilidadLblTitulo);
            this.ContabilidadPnlFiltros.Controls.Add(this.ContabilidadLblResultado);
            this.ContabilidadPnlFiltros.Controls.Add(this.ContabilidadLblDesde);
            this.ContabilidadPnlFiltros.Controls.Add(this.ContabilidadDtpDesde);
            this.ContabilidadPnlFiltros.Controls.Add(this.ContabilidadLblHasta);
            this.ContabilidadPnlFiltros.Controls.Add(this.ContabilidadDtpHasta);
            this.ContabilidadPnlFiltros.Controls.Add(this.ContabilidadLblEstado);
            this.ContabilidadPnlFiltros.Controls.Add(this.ContabilidadCboEstado);
            this.ContabilidadPnlFiltros.Controls.Add(this.ContabilidadLblModulo);
            this.ContabilidadPnlFiltros.Controls.Add(this.ContabilidadCboModulo);
            this.ContabilidadPnlFiltros.Controls.Add(this.ContabilidadLblPais);
            this.ContabilidadPnlFiltros.Controls.Add(this.ContabilidadCboPais);
            this.ContabilidadPnlFiltros.Controls.Add(this.ContabilidadBtnGenerar);
            this.ContabilidadPnlFiltros.Location = new System.Drawing.Point(24, 16);
            this.ContabilidadPnlFiltros.Name = "ContabilidadPnlFiltros";
            this.ContabilidadPnlFiltros.Size = new System.Drawing.Size(1132, 132);
            this.ContabilidadPnlFiltros.TabIndex = 0;
            this.ContabilidadLblTitulo.BackColor = System.Drawing.Color.White;
            this.ContabilidadLblTitulo.Font = new System.Drawing.Font("Lucida Fax", 18F, ((System.Drawing.FontStyle)((System.Drawing.FontStyle.Bold | System.Drawing.FontStyle.Italic))));
            this.ContabilidadLblTitulo.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(35)))), ((int)(((byte)(50)))), ((int)(((byte)(61)))));
            this.ContabilidadLblTitulo.Location = new System.Drawing.Point(20, 12);
            this.ContabilidadLblTitulo.Name = "ContabilidadLblTitulo";
            this.ContabilidadLblTitulo.Size = new System.Drawing.Size(520, 34);
            this.ContabilidadLblTitulo.TabIndex = 0;
            this.ContabilidadLblTitulo.Text = "Reporte de pólizas";
            this.ContabilidadLblResultado.BackColor = System.Drawing.Color.White;
            this.ContabilidadLblResultado.Font = new System.Drawing.Font("Tahoma", 9F);
            this.ContabilidadLblResultado.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(70)))), ((int)(((byte)(104)))), ((int)(((byte)(125)))));
            this.ContabilidadLblResultado.Location = new System.Drawing.Point(560, 18);
            this.ContabilidadLblResultado.Name = "ContabilidadLblResultado";
            this.ContabilidadLblResultado.Size = new System.Drawing.Size(470, 24);
            this.ContabilidadLblResultado.TabIndex = 0;
            this.ContabilidadLblResultado.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.ContabilidadLblDesde.BackColor = System.Drawing.Color.White;
            this.ContabilidadLblDesde.Font = new System.Drawing.Font("Tahoma", 10F);
            this.ContabilidadLblDesde.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(35)))), ((int)(((byte)(50)))), ((int)(((byte)(61)))));
            this.ContabilidadLblDesde.Location = new System.Drawing.Point(20, 52);
            this.ContabilidadLblDesde.Name = "ContabilidadLblDesde";
            this.ContabilidadLblDesde.Size = new System.Drawing.Size(150, 22);
            this.ContabilidadLblDesde.TabIndex = 0;
            this.ContabilidadLblDesde.Text = "Desde";
            this.ContabilidadDtpDesde.CustomFormat = "dd/MM/yyyy";
            this.ContabilidadDtpDesde.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.ContabilidadDtpDesde.Format = System.Windows.Forms.DateTimePickerFormat.Custom;
            this.ContabilidadDtpDesde.Location = new System.Drawing.Point(20, 76);
            this.ContabilidadDtpDesde.Name = "ContabilidadDtpDesde";
            this.ContabilidadDtpDesde.Size = new System.Drawing.Size(150, 30);
            this.ContabilidadDtpDesde.TabIndex = 1;
            this.ContabilidadLblHasta.BackColor = System.Drawing.Color.White;
            this.ContabilidadLblHasta.Font = new System.Drawing.Font("Tahoma", 10F);
            this.ContabilidadLblHasta.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(35)))), ((int)(((byte)(50)))), ((int)(((byte)(61)))));
            this.ContabilidadLblHasta.Location = new System.Drawing.Point(190, 52);
            this.ContabilidadLblHasta.Name = "ContabilidadLblHasta";
            this.ContabilidadLblHasta.Size = new System.Drawing.Size(150, 22);
            this.ContabilidadLblHasta.TabIndex = 0;
            this.ContabilidadLblHasta.Text = "Hasta";
            this.ContabilidadDtpHasta.CustomFormat = "dd/MM/yyyy";
            this.ContabilidadDtpHasta.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.ContabilidadDtpHasta.Format = System.Windows.Forms.DateTimePickerFormat.Custom;
            this.ContabilidadDtpHasta.Location = new System.Drawing.Point(190, 76);
            this.ContabilidadDtpHasta.Name = "ContabilidadDtpHasta";
            this.ContabilidadDtpHasta.Size = new System.Drawing.Size(150, 30);
            this.ContabilidadDtpHasta.TabIndex = 2;
            this.ContabilidadLblEstado.BackColor = System.Drawing.Color.White;
            this.ContabilidadLblEstado.Font = new System.Drawing.Font("Tahoma", 10F);
            this.ContabilidadLblEstado.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(35)))), ((int)(((byte)(50)))), ((int)(((byte)(61)))));
            this.ContabilidadLblEstado.Location = new System.Drawing.Point(360, 52);
            this.ContabilidadLblEstado.Name = "ContabilidadLblEstado";
            this.ContabilidadLblEstado.Size = new System.Drawing.Size(160, 22);
            this.ContabilidadLblEstado.TabIndex = 0;
            this.ContabilidadLblEstado.Text = "Estado";
            this.ContabilidadCboEstado.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.ContabilidadCboEstado.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.ContabilidadCboEstado.FormattingEnabled = true;
            this.ContabilidadCboEstado.Location = new System.Drawing.Point(360, 76);
            this.ContabilidadCboEstado.Name = "ContabilidadCboEstado";
            this.ContabilidadCboEstado.Size = new System.Drawing.Size(160, 31);
            this.ContabilidadCboEstado.TabIndex = 3;
            this.ContabilidadLblModulo.BackColor = System.Drawing.Color.White;
            this.ContabilidadLblModulo.Font = new System.Drawing.Font("Tahoma", 10F);
            this.ContabilidadLblModulo.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(35)))), ((int)(((byte)(50)))), ((int)(((byte)(61)))));
            this.ContabilidadLblModulo.Location = new System.Drawing.Point(540, 52);
            this.ContabilidadLblModulo.Name = "ContabilidadLblModulo";
            this.ContabilidadLblModulo.Size = new System.Drawing.Size(200, 22);
            this.ContabilidadLblModulo.TabIndex = 0;
            this.ContabilidadLblModulo.Text = "Módulo de origen";
            this.ContabilidadCboModulo.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.ContabilidadCboModulo.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.ContabilidadCboModulo.FormattingEnabled = true;
            this.ContabilidadCboModulo.Location = new System.Drawing.Point(540, 76);
            this.ContabilidadCboModulo.Name = "ContabilidadCboModulo";
            this.ContabilidadCboModulo.Size = new System.Drawing.Size(200, 31);
            this.ContabilidadCboModulo.TabIndex = 4;
            this.ContabilidadLblPais.BackColor = System.Drawing.Color.White;
            this.ContabilidadLblPais.Font = new System.Drawing.Font("Tahoma", 10F);
            this.ContabilidadLblPais.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(35)))), ((int)(((byte)(50)))), ((int)(((byte)(61)))));
            this.ContabilidadLblPais.Location = new System.Drawing.Point(760, 52);
            this.ContabilidadLblPais.Name = "ContabilidadLblPais";
            this.ContabilidadLblPais.Size = new System.Drawing.Size(180, 22);
            this.ContabilidadLblPais.TabIndex = 0;
            this.ContabilidadLblPais.Text = "País";
            this.ContabilidadCboPais.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.ContabilidadCboPais.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.ContabilidadCboPais.FormattingEnabled = true;
            this.ContabilidadCboPais.Location = new System.Drawing.Point(760, 76);
            this.ContabilidadCboPais.Name = "ContabilidadCboPais";
            this.ContabilidadCboPais.Size = new System.Drawing.Size(180, 31);
            this.ContabilidadCboPais.TabIndex = 5;
            this.ContabilidadBtnGenerar.BackColor = System.Drawing.Color.Transparent;
            this.ContabilidadBtnGenerar.BackgroundImage = global::CapaVista_Contabilidad.Properties.Resources.btn_consultar;
            this.ContabilidadBtnGenerar.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.ContabilidadBtnGenerar.Cursor = System.Windows.Forms.Cursors.Hand;
            this.ContabilidadBtnGenerar.FlatAppearance.BorderSize = 0;
            this.ContabilidadBtnGenerar.FlatAppearance.MouseDownBackColor = System.Drawing.Color.Transparent;
            this.ContabilidadBtnGenerar.FlatAppearance.MouseOverBackColor = System.Drawing.Color.Transparent;
            this.ContabilidadBtnGenerar.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.ContabilidadBtnGenerar.Location = new System.Drawing.Point(1052, 50);
            this.ContabilidadBtnGenerar.Name = "ContabilidadBtnGenerar";
            this.ContabilidadBtnGenerar.Size = new System.Drawing.Size(60, 60);
            this.ContabilidadBtnGenerar.TabIndex = 6;
            this.ContabilidadBtnGenerar.UseVisualStyleBackColor = false;
            this.ContabilidadRpvReporte.Location = new System.Drawing.Point(24, 160);
            this.ContabilidadRpvReporte.Name = "ContabilidadRpvReporte";
            this.ContabilidadRpvReporte.ServerReport.BearerToken = null;
            this.ContabilidadRpvReporte.Size = new System.Drawing.Size(1132, 588);
            this.ContabilidadRpvReporte.TabIndex = 7;
            this.AcceptButton = this.ContabilidadBtnGenerar;
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(246)))), ((int)(((byte)(247)))), ((int)(((byte)(248)))));
            this.ClientSize = new System.Drawing.Size(1180, 768);
            this.Controls.Add(this.ContabilidadPnlFiltros);
            this.Controls.Add(this.ContabilidadRpvReporte);
            this.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Icon = global::CapaVista_Contabilidad.Properties.Resources.Icono_Contabilidad;
            this.MaximizeBox = false;
            this.Name = "FrmReportePolizas";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "10001 - ReportePolizas";
            this.ContabilidadPnlFiltros.ResumeLayout(false);
            this.ResumeLayout(false);
        }

        #endregion

        private System.Windows.Forms.ToolTip ContabilidadTtpBotones;
        private Componentes.ClsPanelRedondeadoContabilidad ContabilidadPnlFiltros;
        private System.Windows.Forms.Label ContabilidadLblTitulo;
        private System.Windows.Forms.Label ContabilidadLblResultado;
        private System.Windows.Forms.Label ContabilidadLblDesde;
        private System.Windows.Forms.DateTimePicker ContabilidadDtpDesde;
        private System.Windows.Forms.Label ContabilidadLblHasta;
        private System.Windows.Forms.DateTimePicker ContabilidadDtpHasta;
        private System.Windows.Forms.Label ContabilidadLblEstado;
        private System.Windows.Forms.ComboBox ContabilidadCboEstado;
        private System.Windows.Forms.Label ContabilidadLblModulo;
        private System.Windows.Forms.ComboBox ContabilidadCboModulo;
        private System.Windows.Forms.Label ContabilidadLblPais;
        private System.Windows.Forms.ComboBox ContabilidadCboPais;
        private System.Windows.Forms.Button ContabilidadBtnGenerar;
        private Microsoft.Reporting.WinForms.ReportViewer ContabilidadRpvReporte;
    }
}
