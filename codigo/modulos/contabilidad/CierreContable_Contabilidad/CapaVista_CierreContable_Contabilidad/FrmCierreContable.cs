/* Área: Contabilidad. Autor: Donald Osorio. Carné: 0901-23-17982.
 * Fecha: 14/10/2026. Propósito: revisión y cierre del período contable. Responsable: Donald. */
using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
// Inicio de cambio: Mario Taracena - 0901-23-9335 - 27/10/2026
using Diseno = CapaVista_Contabilidad.Componentes.ClsDisenoContabilidad;
// Fin de cambio: Mario Taracena - 0901-23-9335 - 27/10/2026
using CapaControlador_Contabilidad;
// Inicio de cambio: Mario Taracena - 0901-23-9335 - 27/10/2026
using CapaVista_Contabilidad.Componentes;
// Fin de cambio: Mario Taracena - 0901-23-9335 - 27/10/2026

// Inicio de cambio: Mario Taracena - 0901-23-9335 - 27/10/2026
namespace CapaVista_Contabilidad
// Fin de cambio: Mario Taracena - 0901-23-9335 - 27/10/2026
{
    public partial class FrmCierreContable : Form
    {
        private readonly ClsControladorCierreContabilidad _Controlador = new ClsControladorCierreContabilidad();
        private DataTable _Polizas;
        private readonly ClsPaginacionContabilidad _Paginacion;

        // Inicio de cambio: Mario Taracena - 0901-23-9335 - 27/10/2026
        public FrmCierreContable() : this(ClsModeloPermisosPantallaContabilidad.Total)
        // Fin de cambio: Mario Taracena - 0901-23-9335 - 27/10/2026
        {
        }

        // Inicio de cambio: Mario Taracena - 0901-23-9335 - 27/10/2026
        public FrmCierreContable(ClsModeloPermisosPantallaContabilidad Permisos)
        // Fin de cambio: Mario Taracena - 0901-23-9335 - 27/10/2026
        {
            InitializeComponent();
            ContabilidadMetAplicarPermisos(Permisos);
            ClsPresentacionContabilidad.ContabilidadMetPrepararVentana(this, "Cierre contable", new Size(1180, 688));
            ContabilidadDgvPolizas.RowTemplate.Height = 28;
            ContabilidadDgvPolizas.ColumnHeadersHeight = 34;
            _Paginacion = new ClsPaginacionContabilidad(ContabilidadDgvPolizas);
            Disposed += (Remitente, Argumentos) => _Paginacion.Dispose();
            ContabilidadDtpPeriodo.Value = new DateTime(2026, 10, 1);
            ContabilidadDtpPeriodo.ValueChanged += (Remitente, Argumentos) => ContabilidadMetRevisar();
            ContabilidadBtnRevisar.Click += (Remitente, Argumentos) => ContabilidadMetRevisar();
            // Inicio de cambio: Donald Osorio - 0901-23-17982 - 28/10/2026
            ContabilidadBtnCerrar.Click += (Remitente, Argumentos) =>
            // Fin de cambio: Donald Osorio - 0901-23-17982 - 28/10/2026
            {
                // Inicio de cambio: Donald Osorio - 0901-23-17982 - 28/10/2026
                if (!ClsDialogosContabilidad.ContabilidadFuncConfirmar(this, "¿Desea cerrar el período " + ContabilidadDtpPeriodo.Value.ToString("yyyy-MM")
                        + "? Ya no se podrán registrar ni modificar pólizas en él.", "Sí, cerrar"))
                    return;
                string Mensaje;
                bool Cerrado = _Controlador.ContabilidadFuncCerrarPeriodo(ContabilidadDtpPeriodo.Value, out Mensaje);
                Diseno.ContabilidadMetAviso(this, Cerrado ? string.Empty : Mensaje, Mensaje);
                ContabilidadMetRevisar();
                // Fin de cambio: Donald Osorio - 0901-23-17982 - 28/10/2026
            };
            ContabilidadMetRevisar();
        }

        // Inicio de cambio: Mario Taracena - 0901-23-9335 - 27/10/2026
        private void ContabilidadMetAplicarPermisos(ClsModeloPermisosPantallaContabilidad Permisos)
        // Fin de cambio: Mario Taracena - 0901-23-9335 - 27/10/2026
        {
            // Inicio de cambio: Donald Osorio - 0901-23-17982 - 28/10/2026
            ClsPermisosMenuContabilidad.ContabilidadMetRestringir(ContabilidadBtnCerrar, Permisos.PuedeEditar);
            // Fin de cambio: Donald Osorio - 0901-23-17982 - 28/10/2026
        }

        private void ContabilidadMetRevisar()
        {
            DataTable Anterior = _Polizas;
            // Inicio de cambio: Donald Osorio - 0901-23-17982 - 28/10/2026
            string EstadoPeriodo = string.Empty;
            // Fin de cambio: Donald Osorio - 0901-23-17982 - 28/10/2026
            try
            {
                _Polizas = _Controlador.ContabilidadFuncPolizas(ContabilidadDtpPeriodo.Value);
                // Inicio de cambio: Donald Osorio - 0901-23-17982 - 28/10/2026
                EstadoPeriodo = _Controlador.ContabilidadFuncEstadoPeriodo(ContabilidadDtpPeriodo.Value);
                // Fin de cambio: Donald Osorio - 0901-23-17982 - 28/10/2026
            }
            catch (Exception Excepcion)
            {
                _Polizas = new DataTable();
                ClsDialogosContabilidad.ContabilidadMetError(this, "No se pudieron consultar las pólizas: " + Excepcion.Message);
            }
            _Paginacion.ContabilidadMetCargar(_Polizas);
            if (Anterior != null) Anterior.Dispose();
            // Inicio de cambio: Donald Osorio - 0901-23-17982 - 28/10/2026
            string Error = EstadoPeriodo == "CERRADO" ? "el período ya está cerrado." : _Controlador.ContabilidadFuncValidarCierre(_Polizas);
            ContabilidadLblValidacion.Text = EstadoPeriodo == "CERRADO" ? "Período cerrado. Sus saldos ya pasaron al período siguiente."
                : string.IsNullOrEmpty(Error) ? "Revisión completada. El período está listo para el cierre."
            // Fin de cambio: Donald Osorio - 0901-23-17982 - 28/10/2026
                : "Cierre pendiente: " + Error;
            ContabilidadLblValidacion.ForeColor = string.IsNullOrEmpty(Error) ? Color.FromArgb(70, 104, 125) : Color.FromArgb(197, 143, 62);
            // Inicio de cambio: Donald Osorio - 0901-23-17982 - 28/10/2026
            ContabilidadBtnCerrar.Enabled = string.IsNullOrEmpty(Error);
            // Fin de cambio: Donald Osorio - 0901-23-17982 - 28/10/2026
            // Inicio de cambio: Mario Taracena - 0901-23-9335 - 27/10/2026
            ClsModeloResumenCierreContabilidad Resumen = _Controlador.ContabilidadFuncResumenCierre(_Polizas);
            // Fin de cambio: Mario Taracena - 0901-23-9335 - 27/10/2026
            ContabilidadLblResumen.Text = "Aplicadas: " + Resumen.Aplicadas + "     Borradores: " + Resumen.Borradores
                + "     Debe aplicado: " + Diseno.ContabilidadFuncMoneda(Resumen.Debe) + "     Haber aplicado: " + Diseno.ContabilidadFuncMoneda(Resumen.Haber);
        }
    }
}
