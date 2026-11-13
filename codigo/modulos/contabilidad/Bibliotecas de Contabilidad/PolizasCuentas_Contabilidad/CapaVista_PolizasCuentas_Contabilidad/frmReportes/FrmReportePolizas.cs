/*
 * ==================================================================
 * Área : Contabilidad
 * Autor : Oskar Cermeño
 * Carné : 0901-23-15379
 * Fecha : 11/11/2026
 * ==================================================================
 * Propósito :
 *  Reporte de pólizas (libro diario) del botón Reporte de Pólizas
 *  locales: filtros de fechas, estado, módulo de origen y país, y un
 *  ReportViewer con cada póliza, sus líneas, su cuadre y los totales.
 * Reglas especificas:
 *  La ventana solo habla con ClsControladorReportePolizasContabilidad.
 *  El reporte RpReportePolizas.rdlc va embebido en este ensamblado.
 *  Imprimir y exportar solo aparecen con el permiso de imprimir.
 * ===================================================================
*/

using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using Microsoft.Reporting.WinForms;
using CapaControlador_Contabilidad;
using CapaVista_Contabilidad.Componentes;

namespace CapaVista_Contabilidad
{
    // ventana del reporte de pólizas
    public partial class FrmReportePolizas : Form
    {
        // nombre del RDLC embebido
        public const string RecursoReporte = "CapaVista_Contabilidad.Reportes.RpReportePolizas.rdlc";

        // permisos de la pantalla
        private readonly ClsModeloPermisosPantallaContabilidad _Permisos;
        // reglas del reporte
        private readonly ClsControladorReportePolizasContabilidad _Controlador;
        // marcas de error
        private readonly ClsValidacionContabilidad _Validacion = new ClsValidacionContabilidad();
        // filas mostradas
        private DataTable _Lineas;
        // totales mostrados
        private DataTable _Totales;

        // abre con todos los permisos
        public FrmReportePolizas() : this(ClsModeloPermisosPantallaContabilidad.Total)
        {
        }

        // abre con los permisos del MDI
        public FrmReportePolizas(ClsModeloPermisosPantallaContabilidad Permisos) : this(Permisos, new ClsControladorReportePolizasContabilidad())
        {
        }

        // abre con un controlador dado
        public FrmReportePolizas(ClsModeloPermisosPantallaContabilidad Permisos, ClsControladorReportePolizasContabilidad Controlador)
        {
            _Permisos = Permisos ?? ClsModeloPermisosPantallaContabilidad.Ninguno;
            _Controlador = Controlador ?? throw new ArgumentNullException(nameof(Controlador));
            InitializeComponent();
            ClsPresentacionContabilidad.ContabilidadMetPrepararVentana(this, "Reporte de pólizas", new Size(1180, 768));
            ContabilidadMetConfigurarVisor();

            ContabilidadDtpDesde.MinDate = _Controlador.ContabilidadFuncFechaMinima();
            ContabilidadDtpHasta.MinDate = _Controlador.ContabilidadFuncFechaMinima();
            ContabilidadDtpDesde.MaxDate = _Controlador.ContabilidadFuncFechaMaxima();
            ContabilidadDtpHasta.MaxDate = _Controlador.ContabilidadFuncFechaMaxima();
            DateTime Inicio = _Controlador.ContabilidadFuncInicioMes(DateTime.Today);
            ContabilidadDtpDesde.Value = Inicio < ContabilidadDtpDesde.MinDate ? ContabilidadDtpDesde.MinDate : Inicio;
            ContabilidadDtpHasta.Value = DateTime.Today < ContabilidadDtpHasta.MinDate ? ContabilidadDtpHasta.MinDate : DateTime.Today;
            ContabilidadCboEstado.DataSource = _Controlador.ContabilidadFuncObtenerEstados();

            ContabilidadTtpBotones.SetToolTip(ContabilidadBtnGenerar, "Generar reporte");
            ContabilidadBtnGenerar.Click += (Remitente, Argumentos) => ContabilidadMetGenerar();
            Load += ContabilidadMetCargar;
            Disposed += (Remitente, Argumentos) => { ContabilidadMetSoltarTablas(); _Validacion.Dispose(); };
        }

        // botones del visor según permisos
        private void ContabilidadMetConfigurarVisor()
        {
            ContabilidadRpvReporte.ShowPrintButton = _Permisos.PuedeImprimir;
            ContabilidadRpvReporte.ShowExportButton = _Permisos.PuedeImprimir;
            ContabilidadRpvReporte.ShowBackButton = false;
            ContabilidadRpvReporte.ShowRefreshButton = false;
            ContabilidadRpvReporte.ShowStopButton = false;
        }

        // parámetros del RDLC
        public static ReportParameter[] ContabilidadFuncParametros(string Filtros, string Usuario)
        {
            return new[]
            {
                new ReportParameter("Filtros", Filtros ?? string.Empty), new ReportParameter("Usuario", Usuario ?? string.Empty)
            };
        }

        // carga el RDLC con sus datos
        public static void ContabilidadMetPrepararReporte(LocalReport Reporte, DataTable Lineas, DataTable Totales, ReportParameter[] Parametros)
        {
            using (Stream Definicion = typeof(FrmReportePolizas).Assembly.GetManifestResourceStream(RecursoReporte))
                Reporte.LoadReportDefinition(Definicion);
            Reporte.DataSources.Clear();
            Reporte.DataSources.Add(new ReportDataSource("DsPolizas", Lineas));
            Reporte.DataSources.Add(new ReportDataSource("DsTotalesPolizas", Totales));
            Reporte.SetParameters(Parametros);
        }

        // catálogos y primer reporte
        private void ContabilidadMetCargar(object Remitente, EventArgs Argumentos)
        {
            ContabilidadMetCargarCatalogo(ContabilidadCboModulo, _Controlador.ContabilidadFuncObtenerModulos);
            ContabilidadMetCargarCatalogo(ContabilidadCboPais, _Controlador.ContabilidadFuncObtenerPaises);
            ContabilidadMetGenerar();
        }

        // llena un combo id y nombre
        private void ContabilidadMetCargarCatalogo(ComboBox Combo, Func<List<KeyValuePair<int, string>>> Obtener)
        {
            List<KeyValuePair<int, string>> Lista;
            try
            {
                Lista = Obtener();
            }
            catch (Exception Excepcion)
            {
                Lista = new List<KeyValuePair<int, string>> { new KeyValuePair<int, string>(0, ClsControladorReportePolizasContabilidad.Todos) };
                ContabilidadLblResultado.Text = "No se pudieron leer los catálogos: " + Excepcion.Message;
            }
            Combo.DisplayMember = "Value";
            Combo.ValueMember = "Key";
            Combo.DataSource = Lista;
        }

        // id elegido en un combo
        private static int ContabilidadFuncIdElegido(ComboBox Combo)
        {
            return Combo.SelectedValue is int ? (int)Combo.SelectedValue : 0;
        }

        // pide el reporte y lo muestra
        private void ContabilidadMetGenerar()
        {
            _Validacion.ContabilidadMetLimpiar();
            DateTime Desde = ContabilidadDtpDesde.Value.Date;
            DateTime Hasta = ContabilidadDtpHasta.Value.Date;
            string Estado = Convert.ToString(ContabilidadCboEstado.SelectedItem);
            int IdModulo = ContabilidadFuncIdElegido(ContabilidadCboModulo);
            int IdPais = ContabilidadFuncIdElegido(ContabilidadCboPais);

            string Error = _Controlador.ContabilidadFuncValidarFiltros(Desde, Hasta, Estado, IdModulo, IdPais);
            if (Error != string.Empty)
            {
                _Validacion.ContabilidadFuncValidar(ContabilidadDtpHasta, Error);
                ContabilidadLblResultado.Text = string.Empty;
                return;
            }

            string Mensaje;
            ClsModeloReportePolizasContabilidad Reporte;
            Cursor = Cursors.WaitCursor;
            try
            {
                Reporte = _Controlador.ContabilidadFuncGenerar(Desde, Hasta, Estado, IdModulo, IdPais, out Mensaje);
            }
            finally
            {
                Cursor = Cursors.Default;
            }
            if (Reporte == null)
            {
                ContabilidadLblResultado.Text = string.Empty;
                ClsDialogosContabilidad.ContabilidadMetError(this, Mensaje);
                return;
            }

            try
            {
                DataTable Lineas = _Controlador.ContabilidadFuncTablaLineas(Reporte);
                DataTable Totales = _Controlador.ContabilidadFuncTablaTotales(Reporte);
                ContabilidadMetPrepararReporte(ContabilidadRpvReporte.LocalReport, Lineas, Totales, ContabilidadFuncParametros(Reporte.Filtros, Reporte.Usuario));
                ContabilidadMetSoltarTablas();
                _Lineas = Lineas;
                _Totales = Totales;
                ContabilidadRpvReporte.SetDisplayMode(DisplayMode.PrintLayout);
                ContabilidadRpvReporte.ZoomMode = ZoomMode.PageWidth;
                ContabilidadRpvReporte.RefreshReport();
                ContabilidadLblResultado.Text = Mensaje;
            }
            catch (Exception Excepcion)
            {
                ClsDialogosContabilidad.ContabilidadMetError(this, "No se pudo cargar el reporte: " + Excepcion.Message);
            }
        }

        // suelta las tablas del reporte
        private void ContabilidadMetSoltarTablas()
        {
            _Lineas?.Dispose();
            _Totales?.Dispose();
            _Lineas = null;
            _Totales = null;
        }
    }
}
