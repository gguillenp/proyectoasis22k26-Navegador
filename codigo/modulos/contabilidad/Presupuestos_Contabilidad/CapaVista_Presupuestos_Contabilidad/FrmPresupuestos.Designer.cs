/* Área: Contabilidad. Autor: Mario Taracena. Carné: 0901-23-9335.
 * Fecha: 14/10/2026. Propósito: presentación de la consulta de presupuestos y su ejecución por módulo. */
using System.Windows.Forms;
using CapaVista_Contabilidad.Componentes;
using CapaVista_Contabilidad.Properties;

namespace CapaVista_Contabilidad
{
    partial class FrmPresupuestos
    {
        private System.ComponentModel.IContainer components = null;
        protected override void Dispose(bool Disposing)
        {
            if (Disposing && components != null) components.Dispose();
            base.Dispose(Disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            this.ContabilidadLblTitulo = new System.Windows.Forms.Label();
            this.ContabilidadLblPresupuesto = new System.Windows.Forms.Label();
            this.ContabilidadCboPresupuesto = new System.Windows.Forms.ComboBox();
            this.ContabilidadPnlEncabezado = new Componentes.ClsPanelRedondeadoContabilidad();
            this.ContabilidadLblVigencia = new System.Windows.Forms.Label();
            this.ContabilidadPnlDetalle = new Componentes.ClsPanelRedondeadoContabilidad();
            this.ContabilidadLblEjecucion = new System.Windows.Forms.Label();
            this.ContabilidadDgvEjecucion = new Componentes.ClsTablaContabilidad();
            this.ContabilidadColEjeModulo = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.ContabilidadColEjeAsignado = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.ContabilidadColEjeEjecutado = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.ContabilidadColEjePorcentaje = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.ContabilidadColEjeRestante = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.ContabilidadLblResumen = new System.Windows.Forms.Label();
            this.ContabilidadBtnNueva = new Componentes.ClsBotonContabilidad();
            this.ContabilidadBtnModificar = new Componentes.ClsBotonContabilidad();
            this.ContabilidadBtnEliminar = new Componentes.ClsBotonContabilidad();
            ((System.ComponentModel.ISupportInitialize)(this.ContabilidadDgvEjecucion)).BeginInit();
            this.ContabilidadPnlEncabezado.SuspendLayout();
            this.ContabilidadPnlDetalle.SuspendLayout();
            this.SuspendLayout();
            this.ContabilidadLblTitulo.Name = "ContabilidadLblTitulo";
            this.Controls.Add(this.ContabilidadLblTitulo);
            this.ContabilidadLblTitulo.Text = "Presupuestos";
            this.ContabilidadLblTitulo.Location = new System.Drawing.Point(24, 22);
            this.ContabilidadLblTitulo.Size = new System.Drawing.Size(560, 38);
            this.ContabilidadLblTitulo.ForeColor = System.Drawing.Color.FromArgb(35, 50, 61);
            this.ContabilidadLblTitulo.BackColor = System.Drawing.Color.Transparent;
            this.ContabilidadLblTitulo.Font = new System.Drawing.Font("Lucida Fax", 20F, System.Drawing.FontStyle.Bold | System.Drawing.FontStyle.Italic);
            this.ContabilidadPnlEncabezado.Name = "ContabilidadPnlEncabezado";
            this.Controls.Add(this.ContabilidadPnlEncabezado);
            this.ContabilidadPnlEncabezado.Location = new System.Drawing.Point(24, 76);
            this.ContabilidadPnlEncabezado.Size = new System.Drawing.Size(1132, 92);
            this.ContabilidadPnlEncabezado.BackColor = System.Drawing.Color.Transparent;
            this.ContabilidadPnlEncabezado.ColorRelleno = System.Drawing.Color.White;
            this.ContabilidadPnlEncabezado.ColorBorde = System.Drawing.Color.FromArgb(212, 222, 228);
            this.ContabilidadPnlEncabezado.Radio = 18;
            this.ContabilidadLblPresupuesto.Name = "ContabilidadLblPresupuesto";
            this.ContabilidadPnlEncabezado.Controls.Add(this.ContabilidadLblPresupuesto);
            this.ContabilidadLblPresupuesto.Text = "Presupuesto";
            this.ContabilidadLblPresupuesto.Location = new System.Drawing.Point(24, 18);
            this.ContabilidadLblPresupuesto.Size = new System.Drawing.Size(420, 24);
            this.ContabilidadLblPresupuesto.ForeColor = System.Drawing.Color.FromArgb(35, 50, 61);
            this.ContabilidadLblPresupuesto.BackColor = System.Drawing.Color.Transparent;
            this.ContabilidadLblPresupuesto.Font = new System.Drawing.Font("Tahoma", 10F);
            this.ContabilidadCboPresupuesto.Name = "ContabilidadCboPresupuesto";
            this.ContabilidadPnlEncabezado.Controls.Add(this.ContabilidadCboPresupuesto);
            this.ContabilidadCboPresupuesto.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.ContabilidadCboPresupuesto.Location = new System.Drawing.Point(24, 44);
            this.ContabilidadCboPresupuesto.Size = new System.Drawing.Size(420, 31);
            this.ContabilidadCboPresupuesto.Font = new System.Drawing.Font("Segoe UI", 10F);
            this.ContabilidadLblVigencia.Name = "ContabilidadLblVigencia";
            this.ContabilidadPnlEncabezado.Controls.Add(this.ContabilidadLblVigencia);
            this.ContabilidadLblVigencia.Text = "";
            this.ContabilidadLblVigencia.Location = new System.Drawing.Point(474, 44);
            this.ContabilidadLblVigencia.Size = new System.Drawing.Size(634, 30);
            this.ContabilidadLblVigencia.ForeColor = System.Drawing.Color.FromArgb(35, 50, 61);
            this.ContabilidadLblVigencia.BackColor = System.Drawing.Color.Transparent;
            this.ContabilidadLblVigencia.Font = new System.Drawing.Font("Tahoma", 10F);
            this.ContabilidadLblVigencia.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.ContabilidadPnlDetalle.Name = "ContabilidadPnlDetalle";
            this.Controls.Add(this.ContabilidadPnlDetalle);
            this.ContabilidadPnlDetalle.Location = new System.Drawing.Point(24, 184);
            this.ContabilidadPnlDetalle.Size = new System.Drawing.Size(1132, 382);
            this.ContabilidadPnlDetalle.BackColor = System.Drawing.Color.Transparent;
            this.ContabilidadPnlDetalle.ColorRelleno = System.Drawing.Color.White;
            this.ContabilidadPnlDetalle.ColorBorde = System.Drawing.Color.FromArgb(212, 222, 228);
            this.ContabilidadPnlDetalle.Radio = 18;
            this.ContabilidadLblEjecucion.Name = "ContabilidadLblEjecucion";
            this.ContabilidadPnlDetalle.Controls.Add(this.ContabilidadLblEjecucion);
            this.ContabilidadLblEjecucion.Text = "Ejecución por módulo";
            this.ContabilidadLblEjecucion.Location = new System.Drawing.Point(20, 14);
            this.ContabilidadLblEjecucion.Size = new System.Drawing.Size(1092, 24);
            this.ContabilidadLblEjecucion.ForeColor = System.Drawing.Color.FromArgb(35, 50, 61);
            this.ContabilidadLblEjecucion.BackColor = System.Drawing.Color.Transparent;
            this.ContabilidadLblEjecucion.Font = new System.Drawing.Font("Tahoma", 10F, System.Drawing.FontStyle.Bold);
            this.ContabilidadDgvEjecucion.Name = "ContabilidadDgvEjecucion";
            this.ContabilidadPnlDetalle.Controls.Add(this.ContabilidadDgvEjecucion);
            this.ContabilidadDgvEjecucion.Location = new System.Drawing.Point(20, 46);
            this.ContabilidadDgvEjecucion.Size = new System.Drawing.Size(1092, 316);
            this.ContabilidadDgvEjecucion.AutoGenerateColumns = false;
            this.ContabilidadDgvEjecucion.ReadOnly = true;
            this.ContabilidadColEjeModulo.Name = "ContabilidadColEjeModulo";
            this.ContabilidadColEjeModulo.DataPropertyName = "Modulo";
            this.ContabilidadColEjeModulo.HeaderText = "Módulo";
            this.ContabilidadColEjeModulo.MinimumWidth = 90;
            this.ContabilidadColEjeAsignado.Name = "ContabilidadColEjeAsignado";
            this.ContabilidadColEjeAsignado.DataPropertyName = "Asignado";
            this.ContabilidadColEjeAsignado.HeaderText = "Asignado (Q)";
            this.ContabilidadColEjeAsignado.MinimumWidth = 90;
            this.ContabilidadColEjeAsignado.DefaultCellStyle.Format = "N2";
            this.ContabilidadColEjeAsignado.DefaultCellStyle.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
            this.ContabilidadColEjeEjecutado.Name = "ContabilidadColEjeEjecutado";
            this.ContabilidadColEjeEjecutado.DataPropertyName = "Ejecutado";
            this.ContabilidadColEjeEjecutado.HeaderText = "Ejecutado (Q)";
            this.ContabilidadColEjeEjecutado.MinimumWidth = 90;
            this.ContabilidadColEjeEjecutado.DefaultCellStyle.Format = "N2";
            this.ContabilidadColEjeEjecutado.DefaultCellStyle.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
            this.ContabilidadColEjePorcentaje.Name = "ContabilidadColEjePorcentaje";
            this.ContabilidadColEjePorcentaje.DataPropertyName = "Porcentaje";
            this.ContabilidadColEjePorcentaje.HeaderText = "% ejecutado";
            this.ContabilidadColEjePorcentaje.MinimumWidth = 90;
            this.ContabilidadColEjePorcentaje.DefaultCellStyle.Format = "P1";
            this.ContabilidadColEjePorcentaje.DefaultCellStyle.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
            this.ContabilidadColEjeRestante.Name = "ContabilidadColEjeRestante";
            this.ContabilidadColEjeRestante.DataPropertyName = "Restante";
            this.ContabilidadColEjeRestante.HeaderText = "Restante (Q)";
            this.ContabilidadColEjeRestante.MinimumWidth = 90;
            this.ContabilidadColEjeRestante.DefaultCellStyle.Format = "N2";
            this.ContabilidadColEjeRestante.DefaultCellStyle.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight;
            this.ContabilidadDgvEjecucion.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] { this.ContabilidadColEjeModulo, this.ContabilidadColEjeAsignado, this.ContabilidadColEjeEjecutado, this.ContabilidadColEjePorcentaje, this.ContabilidadColEjeRestante });
            this.ContabilidadLblResumen.Name = "ContabilidadLblResumen";
            this.Controls.Add(this.ContabilidadLblResumen);
            this.ContabilidadLblResumen.Text = "";
            this.ContabilidadLblResumen.Location = new System.Drawing.Point(26, 580);
            this.ContabilidadLblResumen.Size = new System.Drawing.Size(1128, 24);
            this.ContabilidadLblResumen.ForeColor = System.Drawing.Color.FromArgb(35, 50, 61);
            this.ContabilidadLblResumen.BackColor = System.Drawing.Color.Transparent;
            this.ContabilidadLblResumen.Font = new System.Drawing.Font("Tahoma", 10F);
            this.ContabilidadBtnNueva.Name = "ContabilidadBtnNueva";
            this.Controls.Add(this.ContabilidadBtnNueva);
            this.ContabilidadBtnNueva.Text = "Nuevo presupuesto";
            this.ContabilidadBtnNueva.Location = new System.Drawing.Point(24, 626);
            this.ContabilidadBtnNueva.Size = new System.Drawing.Size(200, 42);
            this.ContabilidadBtnNueva.Estilo = Componentes.EstiloBotonContabilidad.Secundario;
            this.ContabilidadBtnNueva.Icono = global::CapaVista_Contabilidad.Properties.Resources.ico_mas;
            this.ContabilidadBtnEliminar.Name = "ContabilidadBtnEliminar";
            this.Controls.Add(this.ContabilidadBtnEliminar);
            this.ContabilidadBtnEliminar.Text = "Eliminar presupuesto";
            this.ContabilidadBtnEliminar.Location = new System.Drawing.Point(240, 626);
            this.ContabilidadBtnEliminar.Size = new System.Drawing.Size(210, 42);
            this.ContabilidadBtnEliminar.Estilo = Componentes.EstiloBotonContabilidad.Acento;
            this.ContabilidadBtnModificar.Name = "ContabilidadBtnModificar";
            this.Controls.Add(this.ContabilidadBtnModificar);
            this.ContabilidadBtnModificar.Text = "Modificar presupuesto";
            this.ContabilidadBtnModificar.Location = new System.Drawing.Point(936, 626);
            this.ContabilidadBtnModificar.Size = new System.Drawing.Size(220, 42);
            this.ContabilidadBtnModificar.Estilo = Componentes.EstiloBotonContabilidad.Principal;
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Icon = global::CapaVista_Contabilidad.Properties.Resources.Icono_Contabilidad;
            this.MaximizeBox = false;
            this.Name = "FrmPresupuestos";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "10002 - Presupuestos";
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.ClientSize = new System.Drawing.Size(1180, 688);
            this.BackColor = System.Drawing.Color.FromArgb(246, 247, 248);
            this.Font = new System.Drawing.Font("Segoe UI", 10F);
            ((System.ComponentModel.ISupportInitialize)(this.ContabilidadDgvEjecucion)).EndInit();
            this.ContabilidadPnlEncabezado.ResumeLayout(false);
            this.ContabilidadPnlEncabezado.PerformLayout();
            this.ContabilidadPnlDetalle.ResumeLayout(false);
            this.ContabilidadPnlDetalle.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion

        private System.Windows.Forms.Label ContabilidadLblTitulo;
        private System.Windows.Forms.Label ContabilidadLblPresupuesto;
        private System.Windows.Forms.ComboBox ContabilidadCboPresupuesto;
        private Componentes.ClsPanelRedondeadoContabilidad ContabilidadPnlEncabezado;
        private System.Windows.Forms.Label ContabilidadLblVigencia;
        private Componentes.ClsPanelRedondeadoContabilidad ContabilidadPnlDetalle;
        private System.Windows.Forms.Label ContabilidadLblEjecucion;
        private Componentes.ClsTablaContabilidad ContabilidadDgvEjecucion;
        private System.Windows.Forms.DataGridViewTextBoxColumn ContabilidadColEjeModulo;
        private System.Windows.Forms.DataGridViewTextBoxColumn ContabilidadColEjeAsignado;
        private System.Windows.Forms.DataGridViewTextBoxColumn ContabilidadColEjeEjecutado;
        private System.Windows.Forms.DataGridViewTextBoxColumn ContabilidadColEjePorcentaje;
        private System.Windows.Forms.DataGridViewTextBoxColumn ContabilidadColEjeRestante;
        private System.Windows.Forms.Label ContabilidadLblResumen;
        private Componentes.ClsBotonContabilidad ContabilidadBtnNueva;
        private Componentes.ClsBotonContabilidad ContabilidadBtnModificar;
        private Componentes.ClsBotonContabilidad ContabilidadBtnEliminar;
    }
}
