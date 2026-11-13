/*
 * ==================================================================
 * Área : Contabilidad
 * Autor : Oskar Cermeño
 * Carné : 0901-23-15379
 * Fecha : 11/11/2026
 * ==================================================================
 * Propósito :
 *  Reglas del reporte de pólizas (libro diario): catálogos de los
 *  filtros, validación de fechas y filtros, totales por póliza, cuadre
 *  y totales generales por moneda, y las tablas que recibe el RDLC.
 * Reglas especificas:
 *  La Vista solo habla con esta clase. Una póliza cuadra si el debe es
 *  igual al haber y mayor que cero. Las anuladas se listan pero no
 *  suman en los totales generales. Monedas distintas no se mezclan.
 * ===================================================================
*/

using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using CapaModelo_Contabilidad.Contratos;
using CapaModelo_Contabilidad.Entidades;
using CapaModelo_Contabilidad.Repositorios;

namespace CapaControlador_Contabilidad
{
    // controlador del reporte de pólizas
    public class ClsControladorReportePolizasContabilidad
    {
        // opción sin filtro
        public const string Todos = "Todos";
        // estado anulado
        private const string _Anulada = "ANULADA";
        // formato de fechas
        private const string _FormatoFecha = "dd/MM/yyyy";
        // estados de la póliza
        private static readonly string[] _Estados = { Todos, "BORRADOR", "VALIDADA", "APLICADA", _Anulada };

        // acceso a datos
        private readonly IRepositorioReportePolizasContabilidad _Repositorio;
        // módulos ya leídos
        private Dictionary<int, string> _Modulos;
        // países ya leídos
        private Dictionary<int, string> _Paises;

        // usa el repositorio real
        public ClsControladorReportePolizasContabilidad() : this(new ClsRepositorioReportePolizasContabilidad())
        {
        }

        // recibe el repositorio
        public ClsControladorReportePolizasContabilidad(IRepositorioReportePolizasContabilidad Repositorio)
        {
            _Repositorio = Repositorio ?? throw new ArgumentNullException(nameof(Repositorio));
        }

        // estados para el filtro
        public string[] ContabilidadFuncObtenerEstados()
        {
            return (string[])_Estados.Clone();
        }

        // módulos para el filtro
        public List<KeyValuePair<int, string>> ContabilidadFuncObtenerModulos()
        {
            if (_Modulos == null) _Modulos = _Repositorio.ContabilidadFuncObtenerModulos();
            return ContabilidadFuncConTodos(_Modulos);
        }

        // países para el filtro
        public List<KeyValuePair<int, string>> ContabilidadFuncObtenerPaises()
        {
            if (_Paises == null) _Paises = _Repositorio.ContabilidadFuncObtenerPaises();
            return ContabilidadFuncConTodos(_Paises);
        }

        // agrega la opción todos
        private static List<KeyValuePair<int, string>> ContabilidadFuncConTodos(Dictionary<int, string> Catalogo)
        {
            List<KeyValuePair<int, string>> Lista = new List<KeyValuePair<int, string>> { new KeyValuePair<int, string>(0, Todos) };
            Lista.AddRange(Catalogo.OrderBy(Elemento => Elemento.Key));
            return Lista;
        }

        // primer día permitido
        public DateTime ContabilidadFuncFechaMinima()
        {
            return ClsControladorPolizasContabilidad.InicioSistema;
        }

        // último día permitido
        public DateTime ContabilidadFuncFechaMaxima()
        {
            DateTime Mes = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            return Mes.AddMonths(1).AddDays(-1);
        }

        // primer día del mes
        public DateTime ContabilidadFuncInicioMes(DateTime Fecha)
        {
            return new DateTime(Fecha.Year, Fecha.Month, 1);
        }

        // revisa los filtros
        public string ContabilidadFuncValidarFiltros(DateTime Desde, DateTime Hasta, string Estado, int IdModulo, int IdPais)
        {
            if (Desde.Date < ContabilidadFuncFechaMinima() || Hasta.Date < ContabilidadFuncFechaMinima())
                return "La fecha no es correcta: el sistema inició en octubre de 2026.";
            string Error = ClsValidacionDatosContabilidad.ContabilidadFuncErrorRangoFechas(Desde, Hasta);
            if (Error != string.Empty) return Error;
            if (!string.IsNullOrWhiteSpace(Estado) && !_Estados.Contains(Estado.Trim().ToUpperInvariant()) && Estado.Trim() != Todos)
                return "El estado '" + Estado + "' no es válido.";
            if (IdModulo < 0) return "El módulo de origen no es válido.";
            if (IdPais < 0) return "El país no es válido.";
            return string.Empty;
        }

        // estado que va a la consulta
        public string ContabilidadFuncEstadoConsulta(string Estado)
        {
            return string.IsNullOrWhiteSpace(Estado) || Estado.Trim() == Todos ? string.Empty : Estado.Trim().ToUpperInvariant();
        }

        // si debe y haber cuadran
        public bool ContabilidadFuncEstaCuadrada(decimal Debe, decimal Haber)
        {
            return Debe > 0m && Debe == Haber;
        }

        // consulta y arma el reporte
        public ClsModeloReportePolizasContabilidad ContabilidadFuncGenerar(DateTime Desde, DateTime Hasta, string Estado, int IdModulo, int IdPais, out string Mensaje)
        {
            Mensaje = ContabilidadFuncValidarFiltros(Desde, Hasta, Estado, IdModulo, IdPais);
            if (Mensaje != string.Empty) return null;
            try
            {
                List<ClsLineaReportePolizaContabilidad> Lineas = _Repositorio.ContabilidadFuncObtenerLineas(
                    Desde.Date, Hasta.Date, ContabilidadFuncEstadoConsulta(Estado), IdModulo, IdPais);
                ClsModeloReportePolizasContabilidad Reporte = ContabilidadFuncArmar(Lineas);
                Reporte.Filtros = ContabilidadFuncTextoFiltros(Desde, Hasta, Estado, IdModulo, IdPais);
                Reporte.Usuario = ContabilidadFuncUsuario();
                Mensaje = Reporte.CantidadPolizas == 0
                    ? "No hay pólizas con los filtros elegidos."
                    : Reporte.CantidadPolizas + (Reporte.CantidadPolizas == 1 ? " póliza en el reporte." : " pólizas en el reporte.");
                return Reporte;
            }
            catch (Exception Excepcion)
            {
                Mensaje = "No se pudo generar el reporte de pólizas: " + Excepcion.Message;
                return null;
            }
        }

        // totales, cuadre y totales generales
        public ClsModeloReportePolizasContabilidad ContabilidadFuncArmar(IEnumerable<ClsLineaReportePolizaContabilidad> Lineas)
        {
            ClsModeloReportePolizasContabilidad Reporte = new ClsModeloReportePolizasContabilidad();
            if (Lineas == null) return Reporte;
            foreach (IGrouping<int, ClsLineaReportePolizaContabilidad> Poliza in Lineas.GroupBy(Linea => Linea.IdPoliza))
            {
                decimal Debe = Poliza.Sum(Linea => Linea.Debe);
                decimal Haber = Poliza.Sum(Linea => Linea.Haber);
                bool Cuadra = ContabilidadFuncEstaCuadrada(Debe, Haber);
                foreach (ClsLineaReportePolizaContabilidad Linea in Poliza)
                    Reporte.Lineas.Add(new ClsModeloLineaReportePolizaContabilidad
                    {
                        IdPoliza = Linea.IdPoliza, CodigoPoliza = Linea.CodigoPoliza, Fecha = Linea.Fecha, Periodo = Linea.Periodo,
                        Estado = Linea.Estado, Modulo = Linea.Modulo, Pais = Linea.Pais,
                        Moneda = string.IsNullOrWhiteSpace(Linea.Moneda) ? "GTQ" : Linea.Moneda,
                        DocumentoOrigen = Linea.DocumentoOrigen ?? string.Empty, Descripcion = Linea.Descripcion ?? string.Empty,
                        Linea = Linea.Linea, CodigoCuenta = Linea.CodigoCuenta ?? string.Empty, NombreCuenta = Linea.NombreCuenta ?? string.Empty,
                        DescripcionLinea = Linea.DescripcionLinea ?? string.Empty, Debe = Linea.Debe, Haber = Linea.Haber,
                        TotalDebePoliza = Debe, TotalHaberPoliza = Haber, Cuadra = Cuadra, Diferencia = Debe - Haber
                    });
            }
            Reporte.Totales = ContabilidadFuncTotales(Reporte.Lineas);
            return Reporte;
        }

        // totales generales por moneda
        public List<ClsModeloTotalReportePolizaContabilidad> ContabilidadFuncTotales(IEnumerable<ClsModeloLineaReportePolizaContabilidad> Lineas)
        {
            return Lineas
                .GroupBy(Linea => Linea.IdPoliza)
                .Select(Poliza => Poliza.First())
                .GroupBy(Poliza => Poliza.Moneda)
                .OrderBy(Moneda => Moneda.Key)
                .Select(Moneda => new ClsModeloTotalReportePolizaContabilidad
                {
                    Moneda = Moneda.Key,
                    Polizas = Moneda.Count(Poliza => Poliza.Estado != _Anulada),
                    Debe = Moneda.Where(Poliza => Poliza.Estado != _Anulada).Sum(Poliza => Poliza.TotalDebePoliza),
                    Haber = Moneda.Where(Poliza => Poliza.Estado != _Anulada).Sum(Poliza => Poliza.TotalHaberPoliza),
                    Descuadradas = Moneda.Count(Poliza => Poliza.Estado != _Anulada && !Poliza.Cuadra),
                    Anuladas = Moneda.Count(Poliza => Poliza.Estado == _Anulada)
                })
                .ToList();
        }

        // texto de los filtros
        public string ContabilidadFuncTextoFiltros(DateTime Desde, DateTime Hasta, string Estado, int IdModulo, int IdPais)
        {
            string Modulo = Todos;
            string Pais = Todos;
            if (IdModulo > 0)
                Modulo = ContabilidadFuncObtenerModulos().Where(Elemento => Elemento.Key == IdModulo).Select(Elemento => Elemento.Value).FirstOrDefault() ?? IdModulo.ToString();
            if (IdPais > 0)
                Pais = ContabilidadFuncObtenerPaises().Where(Elemento => Elemento.Key == IdPais).Select(Elemento => Elemento.Value).FirstOrDefault() ?? IdPais.ToString();
            string TextoEstado = ContabilidadFuncEstadoConsulta(Estado);
            return "Del " + Desde.ToString(_FormatoFecha, CultureInfo.InvariantCulture) + " al " + Hasta.ToString(_FormatoFecha, CultureInfo.InvariantCulture) +
                "   |   Estado: " + (TextoEstado == string.Empty ? Todos : TextoEstado) + "   |   Módulo: " + Modulo + "   |   País: " + Pais;
        }

        // usuario de la sesión
        private static string ContabilidadFuncUsuario()
        {
            try
            {
                return ClsControladorSeguridadContabilidad.ContabilidadFuncUsuarioSesion();
            }
            catch
            {
                return string.Empty;
            }
        }

        // tabla de filas para el RDLC
        public DataTable ContabilidadFuncTablaLineas(ClsModeloReportePolizasContabilidad Reporte)
        {
            DataTable Tabla = new DataTable("DsPolizas");
            Tabla.Columns.Add("IdPoliza", typeof(int));
            Tabla.Columns.Add("CodigoPoliza", typeof(string));
            Tabla.Columns.Add("Fecha", typeof(DateTime));
            Tabla.Columns.Add("Periodo", typeof(string));
            Tabla.Columns.Add("Estado", typeof(string));
            Tabla.Columns.Add("Modulo", typeof(string));
            Tabla.Columns.Add("Pais", typeof(string));
            Tabla.Columns.Add("Moneda", typeof(string));
            Tabla.Columns.Add("DocumentoOrigen", typeof(string));
            Tabla.Columns.Add("Descripcion", typeof(string));
            Tabla.Columns.Add("Linea", typeof(int));
            Tabla.Columns.Add("CodigoCuenta", typeof(string));
            Tabla.Columns.Add("NombreCuenta", typeof(string));
            Tabla.Columns.Add("DescripcionLinea", typeof(string));
            Tabla.Columns.Add("Debe", typeof(decimal));
            Tabla.Columns.Add("Haber", typeof(decimal));
            Tabla.Columns.Add("TotalDebePoliza", typeof(decimal));
            Tabla.Columns.Add("TotalHaberPoliza", typeof(decimal));
            Tabla.Columns.Add("Cuadra", typeof(bool));
            Tabla.Columns.Add("Diferencia", typeof(decimal));
            if (Reporte == null) return Tabla;
            foreach (ClsModeloLineaReportePolizaContabilidad Linea in Reporte.Lineas)
                Tabla.Rows.Add(Linea.IdPoliza, Linea.CodigoPoliza, Linea.Fecha, Linea.Periodo, Linea.Estado, Linea.Modulo, Linea.Pais, Linea.Moneda,
                    Linea.DocumentoOrigen, Linea.Descripcion, Linea.Linea, Linea.CodigoCuenta, Linea.NombreCuenta, Linea.DescripcionLinea,
                    Linea.Debe, Linea.Haber, Linea.TotalDebePoliza, Linea.TotalHaberPoliza, Linea.Cuadra, Linea.Diferencia);
            return Tabla;
        }

        // tabla de totales para el RDLC
        public DataTable ContabilidadFuncTablaTotales(ClsModeloReportePolizasContabilidad Reporte)
        {
            DataTable Tabla = new DataTable("DsTotalesPolizas");
            Tabla.Columns.Add("Moneda", typeof(string));
            Tabla.Columns.Add("Polizas", typeof(int));
            Tabla.Columns.Add("Debe", typeof(decimal));
            Tabla.Columns.Add("Haber", typeof(decimal));
            Tabla.Columns.Add("Descuadradas", typeof(int));
            Tabla.Columns.Add("Anuladas", typeof(int));
            if (Reporte == null) return Tabla;
            foreach (ClsModeloTotalReportePolizaContabilidad Total in Reporte.Totales)
                Tabla.Rows.Add(Total.Moneda, Total.Polizas, Total.Debe, Total.Haber, Total.Descuadradas, Total.Anuladas);
            return Tabla;
        }
    }
}
