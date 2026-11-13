/*
 * ==================================================================
 * Área : Contabilidad
 * Autor : Oskar Cermeño
 * Carné : 0901-23-15379
 * Fecha : 11/11/2026
 * ==================================================================
 * Propósito :
 *  Pruebas del reporte de pólizas: filtros y fechas, totales y cuadre,
 *  consulta contra la base, conexión con el MDI y carga y render del
 *  RDLC a PDF con datos de ejemplo.
 * Reglas especificas:
 *  La prueba contra la base inserta dos pólizas de prueba y las borra
 *  al terminar; si no hay conexión queda como no concluyente.
 * ===================================================================
*/

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Odbc;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows.Forms;
using System.Xml.Linq;
using Microsoft.Reporting.WinForms;
using CapaControlador_Contabilidad;
using CapaModelo_Contabilidad.Contratos;
using CapaModelo_Contabilidad.Entidades;
using CapaModelo_Contabilidad.Repositorios;
using CapaVista_Contabilidad;

namespace TestPolizasCuentas_Contabilidad
{
    // pruebas del reporte de pólizas
    [TestClass]
    public class TestReportePolizas
    {
        // repositorio falso para pruebas
        private sealed class ClsRepositorioFalsoReporte : IRepositorioReportePolizasContabilidad
        {
            // líneas que devuelve
            public List<ClsLineaReportePolizaContabilidad> Lineas = new List<ClsLineaReportePolizaContabilidad>();
            // error que lanza
            public Exception Error;
            // veces consultado
            public int Consultas;
            // último estado recibido
            public string Estado;
            // último módulo recibido
            public int IdModulo;
            // último país recibido
            public int IdPais;

            // devuelve las líneas
            public List<ClsLineaReportePolizaContabilidad> ContabilidadFuncObtenerLineas(DateTime Desde, DateTime Hasta, string Estado, int IdModulo, int IdPais)
            {
                Consultas++;
                this.Estado = Estado;
                this.IdModulo = IdModulo;
                this.IdPais = IdPais;
                if (Error != null) throw Error;
                return Lineas;
            }

            // módulos de prueba
            public Dictionary<int, string> ContabilidadFuncObtenerModulos()
            {
                return new Dictionary<int, string> { { 1, "Contabilidad" }, { 5, "Ventas" } };
            }

            // países de prueba
            public Dictionary<int, string> ContabilidadFuncObtenerPaises()
            {
                return new Dictionary<int, string> { { 1, "Guatemala" }, { 2, "El Salvador" } };
            }
        }

        // fecha válida de prueba
        private static readonly DateTime _Dia = new DateTime(2026, 11, 3);

        // controlador con repositorio falso
        private static ClsControladorReportePolizasContabilidad ContabilidadFuncControlador(ClsRepositorioFalsoReporte Repositorio = null)
        {
            return new ClsControladorReportePolizasContabilidad(Repositorio ?? new ClsRepositorioFalsoReporte());
        }

        // una línea de ejemplo
        private static ClsLineaReportePolizaContabilidad ContabilidadFuncLinea(int IdPoliza, string Codigo, string Estado, string Moneda, int Linea,
            string Cuenta, string Nombre, decimal Debe, decimal Haber)
        {
            return new ClsLineaReportePolizaContabilidad
            {
                IdPoliza = IdPoliza, CodigoPoliza = Codigo, Fecha = _Dia, Periodo = "2026-11", Estado = Estado, Modulo = "Contabilidad",
                Pais = Moneda == "USD" ? "El Salvador" : "Guatemala", Moneda = Moneda, Descripcion = "Póliza de prueba " + Codigo,
                DocumentoOrigen = IdPoliza == 2 ? "FAC-0001" : string.Empty, Linea = Linea, CodigoCuenta = Cuenta, NombreCuenta = Nombre,
                DescripcionLinea = "Línea " + Linea, Debe = Debe, Haber = Haber
            };
        }

        // pólizas de ejemplo
        private static List<ClsLineaReportePolizaContabilidad> ContabilidadFuncLineasEjemplo()
        {
            return new List<ClsLineaReportePolizaContabilidad>
            {
                ContabilidadFuncLinea(1, "POL-2026-11-0001", "APLICADA", "GTQ", 1, "1.01.02.001", "Banco Industrial (Moneda Nacional)", 1500m, 0m),
                ContabilidadFuncLinea(1, "POL-2026-11-0001", "APLICADA", "GTQ", 2, "1.01.01.001", "Caja General", 0m, 1500m),
                ContabilidadFuncLinea(2, "POL-2026-11-0002", "BORRADOR", "GTQ", 1, "1.01.04.001", "Clientes Nacionales", 800m, 0m),
                ContabilidadFuncLinea(2, "POL-2026-11-0002", "BORRADOR", "GTQ", 2, "4.01.01.001", "Ventas", 0m, 750.50m),
                ContabilidadFuncLinea(3, "POL-2026-11-0003", "VALIDADA", "USD", 1, "1.01.01.001", "Caja General", 200m, 0m),
                ContabilidadFuncLinea(3, "POL-2026-11-0003", "VALIDADA", "USD", 2, "1.01.02.004", "Banco Industrial (Moneda Extranjera)", 0m, 200m),
                ContabilidadFuncLinea(4, "POL-2026-11-0004", "ANULADA", "GTQ", 1, "1.01.01.002", "Caja Chica", 99m, 0m),
                ContabilidadFuncLinea(4, "POL-2026-11-0004", "ANULADA", "GTQ", 2, "1.01.01.001", "Caja General", 0m, 99m)
            };
        }

        // ===================== Filtros y fechas =====================

        // estados del filtro
        [TestMethod]
        public void Test_Filtros_EstadosIncluyenTodosYAnulada()
        {
            // Arrange
            ClsControladorReportePolizasContabilidad Controlador = ContabilidadFuncControlador();

            // Act
            string[] Estados = Controlador.ContabilidadFuncObtenerEstados();

            // Assert
            CollectionAssert.AreEqual(new[] { "Todos", "BORRADOR", "VALIDADA", "APLICADA", "ANULADA" }, Estados);
        }

        // catálogos con opción todos
        [TestMethod]
        public void Test_Filtros_CatalogosEmpiezanConTodos()
        {
            // Arrange
            ClsControladorReportePolizasContabilidad Controlador = ContabilidadFuncControlador();

            // Act
            List<KeyValuePair<int, string>> Modulos = Controlador.ContabilidadFuncObtenerModulos();
            List<KeyValuePair<int, string>> Paises = Controlador.ContabilidadFuncObtenerPaises();

            // Assert
            Assert.AreEqual(0, Modulos[0].Key);
            Assert.AreEqual("Todos", Modulos[0].Value);
            Assert.AreEqual(3, Modulos.Count);
            Assert.AreEqual("Todos", Paises[0].Value);
            Assert.AreEqual("El Salvador", Paises[2].Value);
        }

        // filtros correctos
        [TestMethod]
        public void Test_Filtros_ValidosNoDanError()
        {
            // Arrange
            ClsControladorReportePolizasContabilidad Controlador = ContabilidadFuncControlador();

            // Act
            string Error = Controlador.ContabilidadFuncValidarFiltros(new DateTime(2026, 11, 1), new DateTime(2026, 11, 30), "Todos", 0, 0);

            // Assert
            Assert.AreEqual(string.Empty, Error);
        }

        // rango de fechas al revés
        [TestMethod]
        public void Test_Filtros_RangoInvertidoDaError()
        {
            // Arrange
            ClsControladorReportePolizasContabilidad Controlador = ContabilidadFuncControlador();

            // Act
            string Error = Controlador.ContabilidadFuncValidarFiltros(new DateTime(2026, 11, 30), new DateTime(2026, 11, 1), "Todos", 0, 0);

            // Assert
            Assert.AreEqual("La fecha final no puede ser anterior a la inicial.", Error);
        }

        // fecha antes del sistema
        [TestMethod]
        public void Test_Filtros_FechaAntesDelInicioDelSistema()
        {
            // Arrange
            ClsControladorReportePolizasContabilidad Controlador = ContabilidadFuncControlador();

            // Act
            string Error = Controlador.ContabilidadFuncValidarFiltros(new DateTime(2026, 9, 30), new DateTime(2026, 11, 1), "Todos", 0, 0);

            // Assert
            StringAssert.Contains(Error, "octubre de 2026");
        }

        // filtros fuera de catálogo
        [TestMethod]
        public void Test_Filtros_EstadoModuloYPaisInvalidos()
        {
            // Arrange
            ClsControladorReportePolizasContabilidad Controlador = ContabilidadFuncControlador();

            // Act
            string ErrorEstado = Controlador.ContabilidadFuncValidarFiltros(_Dia, _Dia, "CERRADA", 0, 0);
            string ErrorModulo = Controlador.ContabilidadFuncValidarFiltros(_Dia, _Dia, "Todos", -1, 0);
            string ErrorPais = Controlador.ContabilidadFuncValidarFiltros(_Dia, _Dia, "Todos", 0, -1);

            // Assert
            StringAssert.Contains(ErrorEstado, "CERRADA");
            StringAssert.Contains(ErrorModulo, "módulo");
            StringAssert.Contains(ErrorPais, "país");
        }

        // estado que va a la consulta
        [TestMethod]
        public void Test_Filtros_EstadoTodosNoFiltraLaConsulta()
        {
            // Arrange
            ClsControladorReportePolizasContabilidad Controlador = ContabilidadFuncControlador();

            // Act
            string Todos = Controlador.ContabilidadFuncEstadoConsulta("Todos");
            string Vacio = Controlador.ContabilidadFuncEstadoConsulta(null);
            string Validada = Controlador.ContabilidadFuncEstadoConsulta(" validada ");

            // Assert
            Assert.AreEqual(string.Empty, Todos);
            Assert.AreEqual(string.Empty, Vacio);
            Assert.AreEqual("VALIDADA", Validada);
        }

        // fechas por defecto
        [TestMethod]
        public void Test_Filtros_FechasPorDefecto()
        {
            // Arrange
            ClsControladorReportePolizasContabilidad Controlador = ContabilidadFuncControlador();

            // Act
            DateTime Inicio = Controlador.ContabilidadFuncInicioMes(new DateTime(2026, 11, 18));
            DateTime Maxima = Controlador.ContabilidadFuncFechaMaxima();

            // Assert
            Assert.AreEqual(new DateTime(2026, 11, 1), Inicio);
            Assert.AreEqual(new DateTime(2026, 10, 1), Controlador.ContabilidadFuncFechaMinima());
            Assert.AreEqual(1, Maxima.AddDays(1).Day);
            Assert.IsTrue(Maxima >= DateTime.Today);
        }

        // texto de filtros
        [TestMethod]
        public void Test_Filtros_TextoUsaLosNombres()
        {
            // Arrange
            ClsControladorReportePolizasContabilidad Controlador = ContabilidadFuncControlador();

            // Act
            string Texto = Controlador.ContabilidadFuncTextoFiltros(new DateTime(2026, 11, 1), new DateTime(2026, 11, 30), "APLICADA", 5, 2);

            // Assert
            StringAssert.Contains(Texto, "Del 01/11/2026 al 30/11/2026");
            StringAssert.Contains(Texto, "Estado: APLICADA");
            StringAssert.Contains(Texto, "Módulo: Ventas");
            StringAssert.Contains(Texto, "País: El Salvador");
        }

        // ===================== Totales y cuadre =====================

        // totales de una póliza
        [TestMethod]
        public void Test_Totales_PorPolizaYCuadre()
        {
            // Arrange
            ClsControladorReportePolizasContabilidad Controlador = ContabilidadFuncControlador();

            // Act
            ClsModeloReportePolizasContabilidad Reporte = Controlador.ContabilidadFuncArmar(ContabilidadFuncLineasEjemplo());
            ClsModeloLineaReportePolizaContabilidad Primera = Reporte.Lineas.First(Linea => Linea.IdPoliza == 1);

            // Assert
            Assert.AreEqual(8, Reporte.Lineas.Count);
            Assert.AreEqual(4, Reporte.CantidadPolizas);
            Assert.AreEqual(1500m, Primera.TotalDebePoliza);
            Assert.AreEqual(1500m, Primera.TotalHaberPoliza);
            Assert.IsTrue(Primera.Cuadra);
            Assert.AreEqual(0m, Primera.Diferencia);
        }

        // póliza que no cuadra
        [TestMethod]
        public void Test_Totales_PolizaDescuadradaMarcaDiferencia()
        {
            // Arrange
            ClsControladorReportePolizasContabilidad Controlador = ContabilidadFuncControlador();

            // Act
            ClsModeloReportePolizasContabilidad Reporte = Controlador.ContabilidadFuncArmar(ContabilidadFuncLineasEjemplo());
            List<ClsModeloLineaReportePolizaContabilidad> Segunda = Reporte.Lineas.Where(Linea => Linea.IdPoliza == 2).ToList();

            // Assert
            Assert.IsTrue(Segunda.All(Linea => !Linea.Cuadra));
            Assert.IsTrue(Segunda.All(Linea => Linea.Diferencia == 49.50m));
            Assert.AreEqual(800m, Segunda[0].TotalDebePoliza);
            Assert.AreEqual(750.50m, Segunda[0].TotalHaberPoliza);
        }

        // regla de cuadre
        [TestMethod]
        public void Test_Totales_PolizaEnCeroNoCuadra()
        {
            // Arrange
            ClsControladorReportePolizasContabilidad Controlador = ContabilidadFuncControlador();

            // Act
            bool Cero = Controlador.ContabilidadFuncEstaCuadrada(0m, 0m);
            bool Igual = Controlador.ContabilidadFuncEstaCuadrada(10.25m, 10.25m);
            bool Distinto = Controlador.ContabilidadFuncEstaCuadrada(10.25m, 10.24m);

            // Assert
            Assert.IsFalse(Cero);
            Assert.IsTrue(Igual);
            Assert.IsFalse(Distinto);
        }

        // totales por moneda
        [TestMethod]
        public void Test_Totales_SeparanMonedasYExcluyenAnuladas()
        {
            // Arrange
            ClsControladorReportePolizasContabilidad Controlador = ContabilidadFuncControlador();

            // Act
            ClsModeloReportePolizasContabilidad Reporte = Controlador.ContabilidadFuncArmar(ContabilidadFuncLineasEjemplo());
            ClsModeloTotalReportePolizaContabilidad Quetzales = Reporte.Totales.Single(Total => Total.Moneda == "GTQ");
            ClsModeloTotalReportePolizaContabilidad Dolares = Reporte.Totales.Single(Total => Total.Moneda == "USD");

            // Assert
            Assert.AreEqual(2, Reporte.Totales.Count);
            Assert.AreEqual(2, Quetzales.Polizas);
            Assert.AreEqual(2300m, Quetzales.Debe);
            Assert.AreEqual(2250.50m, Quetzales.Haber);
            Assert.AreEqual(1, Quetzales.Descuadradas);
            Assert.AreEqual(1, Quetzales.Anuladas);
            Assert.AreEqual(1, Dolares.Polizas);
            Assert.AreEqual(200m, Dolares.Debe);
            Assert.AreEqual(0, Dolares.Descuadradas);
        }

        // reporte sin líneas
        [TestMethod]
        public void Test_Totales_SinLineasDaReporteVacio()
        {
            // Arrange
            ClsControladorReportePolizasContabilidad Controlador = ContabilidadFuncControlador();

            // Act
            ClsModeloReportePolizasContabilidad Vacio = Controlador.ContabilidadFuncArmar(new List<ClsLineaReportePolizaContabilidad>());
            ClsModeloReportePolizasContabilidad Nulo = Controlador.ContabilidadFuncArmar(null);

            // Assert
            Assert.AreEqual(0, Vacio.CantidadPolizas);
            Assert.AreEqual(0, Vacio.Totales.Count);
            Assert.AreEqual(0, Nulo.Lineas.Count);
        }

        // ===================== Generar con el controlador =====================

        // filtros hacia el repositorio
        [TestMethod]
        public void Test_Generar_PasaLosFiltrosAlRepositorio()
        {
            // Arrange
            ClsRepositorioFalsoReporte Repositorio = new ClsRepositorioFalsoReporte { Lineas = ContabilidadFuncLineasEjemplo() };
            ClsControladorReportePolizasContabilidad Controlador = ContabilidadFuncControlador(Repositorio);
            string Mensaje;

            // Act
            ClsModeloReportePolizasContabilidad Reporte = Controlador.ContabilidadFuncGenerar(new DateTime(2026, 11, 1), new DateTime(2026, 11, 30), "Todos", 1, 2, out Mensaje);

            // Assert
            Assert.IsNotNull(Reporte);
            Assert.AreEqual(1, Repositorio.Consultas);
            Assert.AreEqual(string.Empty, Repositorio.Estado);
            Assert.AreEqual(1, Repositorio.IdModulo);
            Assert.AreEqual(2, Repositorio.IdPais);
            Assert.AreEqual("4 pólizas en el reporte.", Mensaje);
            StringAssert.Contains(Reporte.Filtros, "País: El Salvador");
        }

        // no consulta con error
        [TestMethod]
        public void Test_Generar_FiltrosInvalidosNoConsultan()
        {
            // Arrange
            ClsRepositorioFalsoReporte Repositorio = new ClsRepositorioFalsoReporte();
            ClsControladorReportePolizasContabilidad Controlador = ContabilidadFuncControlador(Repositorio);
            string Mensaje;

            // Act
            ClsModeloReportePolizasContabilidad Reporte = Controlador.ContabilidadFuncGenerar(new DateTime(2026, 11, 30), new DateTime(2026, 11, 1), "Todos", 0, 0, out Mensaje);

            // Assert
            Assert.IsNull(Reporte);
            Assert.AreEqual(0, Repositorio.Consultas);
            Assert.AreEqual("La fecha final no puede ser anterior a la inicial.", Mensaje);
        }

        // aviso sin pólizas
        [TestMethod]
        public void Test_Generar_SinPolizasAvisa()
        {
            // Arrange
            ClsControladorReportePolizasContabilidad Controlador = ContabilidadFuncControlador();
            string Mensaje;

            // Act
            ClsModeloReportePolizasContabilidad Reporte = Controlador.ContabilidadFuncGenerar(_Dia, _Dia, "VALIDADA", 0, 0, out Mensaje);

            // Assert
            Assert.IsNotNull(Reporte);
            Assert.AreEqual(0, Reporte.CantidadPolizas);
            Assert.AreEqual("No hay pólizas con los filtros elegidos.", Mensaje);
        }

        // error de la base
        [TestMethod]
        public void Test_Generar_ErrorDeLaBaseDevuelveMensaje()
        {
            // Arrange
            ClsRepositorioFalsoReporte Repositorio = new ClsRepositorioFalsoReporte { Error = new InvalidOperationException("sin conexión") };
            ClsControladorReportePolizasContabilidad Controlador = ContabilidadFuncControlador(Repositorio);
            string Mensaje;

            // Act
            ClsModeloReportePolizasContabilidad Reporte = Controlador.ContabilidadFuncGenerar(_Dia, _Dia, "Todos", 0, 0, out Mensaje);

            // Assert
            Assert.IsNull(Reporte);
            StringAssert.Contains(Mensaje, "sin conexión");
        }

        // ===================== Consulta del repositorio =====================

        // consulta sin filtros
        [TestMethod]
        public void Test_Consulta_SinFiltrosSoloUsaFechas()
        {
            // Arrange
            List<OdbcParameter> Parametros = new List<OdbcParameter>();

            // Act
            string Consulta = ClsRepositorioReportePolizasContabilidad.ContabilidadFuncConsulta(_Dia, _Dia, string.Empty, 0, 0, Parametros);

            // Assert
            Assert.AreEqual(2, Parametros.Count);
            Assert.AreEqual(2, Consulta.Count(Caracter => Caracter == '?'));
            StringAssert.Contains(Consulta, "estadoPolizaEncabezado = 'ANULADA'");
            Assert.IsFalse(Consulta.Contains("pe.idPais = ?"));
        }

        // consulta con filtros
        [TestMethod]
        public void Test_Consulta_ConFiltrosAgregaParametrosEnOrden()
        {
            // Arrange
            List<OdbcParameter> Parametros = new List<OdbcParameter>();

            // Act
            string Consulta = ClsRepositorioReportePolizasContabilidad.ContabilidadFuncConsulta(_Dia, _Dia, "APLICADA", 3, 2, Parametros);

            // Assert
            Assert.AreEqual(5, Parametros.Count);
            Assert.AreEqual(5, Consulta.Count(Caracter => Caracter == '?'));
            CollectionAssert.AreEqual(new[] { "Desde", "Hasta", "Estado", "Modulo", "Pais" }, Parametros.Select(Parametro => Parametro.ParameterName).ToArray());
            Assert.AreEqual("APLICADA", Parametros[2].Value);
            Assert.IsTrue(Consulta.TrimEnd().EndsWith("pd.lineaPolizaDetalle"));
        }

        // póliza sin líneas
        [TestMethod]
        public void Test_Consulta_MapeaPolizaSinLineas()
        {
            // Arrange
            DataTable Tabla = new DataTable();
            foreach (string Columna in new[] { "idPolizaEncabezado", "codigoPolizaEncabezado", "fechaPolizaEncabezado", "periodoContablePolizaEncabezado",
                "estadoPolizaEncabezado", "nombreModuloContable", "nombrePais", "codigoMonedaPais", "documentoOrigenPolizaEncabezado",
                "descripcionPolizaEncabezado", "lineaPolizaDetalle", "codigoCuenta", "nombreCuenta", "descripcionPolizaDetalle",
                "debePolizaDetalle", "haberPolizaDetalle" })
                Tabla.Columns.Add(Columna, typeof(object));
            Tabla.Rows.Add(7, "POL-2026-11-0007", _Dia, "2026-11", "BORRADOR", "Contabilidad", "Guatemala", DBNull.Value, DBNull.Value,
                DBNull.Value, DBNull.Value, DBNull.Value, DBNull.Value, DBNull.Value, DBNull.Value, DBNull.Value);

            // Act
            List<ClsLineaReportePolizaContabilidad> Lineas = ClsRepositorioReportePolizasContabilidad.ContabilidadFuncMapear(Tabla);

            // Assert
            Assert.AreEqual(1, Lineas.Count);
            Assert.AreEqual(0, Lineas[0].Linea);
            Assert.AreEqual(0m, Lineas[0].Debe);
            Assert.AreEqual("GTQ", Lineas[0].Moneda);
            Assert.AreEqual(string.Empty, Lineas[0].Descripcion);
        }

        // consulta contra la base
        [TestMethod]
        public void Test_Consulta_ContraLaBaseIncluyeAnuladasYSoloLineasActivas()
        {
            // Arrange
            string Sufijo = Guid.NewGuid().ToString("N").Substring(0, 8).ToUpperInvariant();
            DateTime Fecha = new DateTime(2026, 10, 20);
            List<int> Polizas = new List<int>();
            OdbcConnection Conexion = new OdbcConnection("Dsn=EmbutidosS.A");
            try
            {
                Conexion.Open();
            }
            catch (Exception Excepcion)
            {
                Conexion.Dispose();
                Assert.Inconclusive("No hay conexión con la base: " + Excepcion.Message);
            }
            try
            {
                int Activa = ContabilidadFuncInsertarPoliza(Conexion, "TST-RPT-A-" + Sufijo, Fecha, "BORRADOR", true);
                Polizas.Add(Activa);
                ContabilidadMetInsertarLinea(Conexion, Activa, 5, 1, 150.25m, 0m, true);
                ContabilidadMetInsertarLinea(Conexion, Activa, 8, 2, 0m, 150.25m, true);
                ContabilidadMetInsertarLinea(Conexion, Activa, 6, 3, 999m, 0m, false);
                int Anulada = ContabilidadFuncInsertarPoliza(Conexion, "TST-RPT-N-" + Sufijo, Fecha, "ANULADA", false);
                Polizas.Add(Anulada);
                ContabilidadMetInsertarLinea(Conexion, Anulada, 5, 1, 10m, 0m, true);
                ContabilidadMetInsertarLinea(Conexion, Anulada, 8, 2, 0m, 10m, true);
                ClsRepositorioReportePolizasContabilidad Repositorio = new ClsRepositorioReportePolizasContabilidad();

                // Act
                List<ClsLineaReportePolizaContabilidad> Todas = Repositorio.ContabilidadFuncObtenerLineas(Fecha, Fecha, string.Empty, 1, 1)
                    .Where(Linea => Polizas.Contains(Linea.IdPoliza)).ToList();
                List<ClsLineaReportePolizaContabilidad> Borradores = Repositorio.ContabilidadFuncObtenerLineas(Fecha, Fecha, "BORRADOR", 0, 0)
                    .Where(Linea => Polizas.Contains(Linea.IdPoliza)).ToList();
                ClsModeloReportePolizasContabilidad Reporte = new ClsControladorReportePolizasContabilidad(Repositorio).ContabilidadFuncArmar(Todas);

                // Assert
                Assert.AreEqual(4, Todas.Count);
                Assert.AreEqual(2, Todas.Count(Linea => Linea.Estado == "ANULADA"));
                Assert.IsFalse(Todas.Any(Linea => Linea.Debe == 999m));
                Assert.AreEqual(2, Borradores.Count);
                Assert.IsTrue(Borradores.All(Linea => Linea.IdPoliza == Activa));
                Assert.AreEqual("GTQ", Todas[0].Moneda);
                Assert.AreEqual("Guatemala", Todas[0].Pais);
                Assert.AreEqual("1.01.01.001", Todas.First(Linea => Linea.IdPoliza == Activa && Linea.Linea == 1).CodigoCuenta);
                Assert.IsTrue(Reporte.Lineas.Where(Linea => Linea.IdPoliza == Activa).All(Linea => Linea.Cuadra && Linea.TotalDebePoliza == 150.25m));
                Assert.AreEqual(150.25m, Reporte.Totales.Single().Debe);
                Assert.AreEqual(1, Reporte.Totales.Single().Anuladas);
            }
            finally
            {
                ContabilidadMetBorrarPolizas(Conexion, Polizas);
                Conexion.Dispose();
            }
        }

        // inserta un encabezado de prueba
        private static int ContabilidadFuncInsertarPoliza(OdbcConnection Conexion, string Codigo, DateTime Fecha, string Estado, bool Activa)
        {
            using (OdbcCommand Comando = new OdbcCommand("INSERT INTO tblPolizaEncabezado (idModuloContable, idPais, codigoPolizaEncabezado, " +
                "fechaPolizaEncabezado, periodoContablePolizaEncabezado, estadoPolizaEncabezado, descripcionPolizaEncabezado, is_active) " +
                "VALUES (1, 1, ?, ?, ?, ?, 'Prueba del reporte de pólizas', ?)", Conexion))
            {
                Comando.Parameters.AddWithValue("Codigo", Codigo);
                Comando.Parameters.Add(new OdbcParameter("Fecha", OdbcType.Date) { Value = Fecha });
                Comando.Parameters.AddWithValue("Periodo", Fecha.ToString("yyyy-MM"));
                Comando.Parameters.AddWithValue("Estado", Estado);
                Comando.Parameters.AddWithValue("Activa", Activa ? 1 : 0);
                Comando.ExecuteNonQuery();
            }
            using (OdbcCommand Comando = new OdbcCommand("SELECT LAST_INSERT_ID()", Conexion))
                return Convert.ToInt32(Comando.ExecuteScalar());
        }

        // inserta una línea de prueba
        private static void ContabilidadMetInsertarLinea(OdbcConnection Conexion, int IdPoliza, int IdCuenta, int Linea, decimal Debe, decimal Haber, bool Activa)
        {
            using (OdbcCommand Comando = new OdbcCommand("INSERT INTO tblPolizaDetalle (idPolizaEncabezado, idCuenta, lineaPolizaDetalle, " +
                "debePolizaDetalle, haberPolizaDetalle, is_active) VALUES (?, ?, ?, ?, ?, ?)", Conexion))
            {
                Comando.Parameters.AddWithValue("Poliza", IdPoliza);
                Comando.Parameters.AddWithValue("Cuenta", IdCuenta);
                Comando.Parameters.AddWithValue("Linea", Linea);
                Comando.Parameters.Add(new OdbcParameter("Debe", OdbcType.Decimal) { Precision = 12, Scale = 4, Value = Debe });
                Comando.Parameters.Add(new OdbcParameter("Haber", OdbcType.Decimal) { Precision = 12, Scale = 4, Value = Haber });
                Comando.Parameters.AddWithValue("Activa", Activa ? 1 : 0);
                Comando.ExecuteNonQuery();
            }
        }

        // borra las pólizas de prueba
        private static void ContabilidadMetBorrarPolizas(OdbcConnection Conexion, List<int> Polizas)
        {
            if (Conexion.State != ConnectionState.Open) return;
            foreach (int IdPoliza in Polizas)
                foreach (string Sentencia in new[] { "DELETE FROM tblPolizaDetalle WHERE idPolizaEncabezado = ?", "DELETE FROM tblPolizaEncabezado WHERE idPolizaEncabezado = ?" })
                    using (OdbcCommand Comando = new OdbcCommand(Sentencia, Conexion))
                    {
                        Comando.Parameters.AddWithValue("Poliza", IdPoliza);
                        Comando.ExecuteNonQuery();
                    }
        }

        // ===================== Conexión con el MDI y vista =====================

        // reporte desde el MDI
        [TestMethod]
        public void Test_Mdi_CrearReporteYaNoDevuelveNull()
        {
            // Arrange
            ClsModeloPermisosPantallaContabilidad Permisos = ClsModeloPermisosPantallaContabilidad.Total;

            // Act
            using (Form Formulario = ClsReportePolizasContabilidad.ContabilidadFuncCrearReporte(Permisos))
            {
                // Assert
                Assert.IsNotNull(Formulario);
                Assert.IsInstanceOfType(Formulario, typeof(FrmReportePolizas));
                Assert.AreEqual("10001 - ReportePolizas", Formulario.Text);
            }
        }

        // botones según permiso
        [TestMethod]
        public void Test_Vista_ImprimirYExportarSegunPermiso()
        {
            // Arrange
            ClsModeloPermisosPantallaContabilidad SinImprimir = new ClsModeloPermisosPantallaContabilidad { TieneAcceso = true };
            ClsModeloPermisosPantallaContabilidad ConImprimir = ClsModeloPermisosPantallaContabilidad.Total;
            FieldInfo Campo = typeof(FrmReportePolizas).GetField("ContabilidadRpvReporte", BindingFlags.Instance | BindingFlags.NonPublic);

            // Act
            using (FrmReportePolizas Restringido = new FrmReportePolizas(SinImprimir, ContabilidadFuncControlador()))
            using (FrmReportePolizas Completo = new FrmReportePolizas(ConImprimir, ContabilidadFuncControlador()))
            {
                ReportViewer VisorRestringido = (ReportViewer)Campo.GetValue(Restringido);
                ReportViewer VisorCompleto = (ReportViewer)Campo.GetValue(Completo);

                // Assert
                Assert.IsFalse(VisorRestringido.ShowPrintButton);
                Assert.IsFalse(VisorRestringido.ShowExportButton);
                Assert.IsTrue(VisorCompleto.ShowPrintButton);
                Assert.IsTrue(VisorCompleto.ShowExportButton);
            }
        }

        // ===================== RDLC =====================

        // lee el RDLC embebido
        private static XDocument ContabilidadFuncLeerRdlc()
        {
            using (Stream Flujo = typeof(FrmReportePolizas).Assembly.GetManifestResourceStream(FrmReportePolizas.RecursoReporte))
            {
                Assert.IsNotNull(Flujo, "El RDLC no está embebido con el nombre " + FrmReportePolizas.RecursoReporte);
                return XDocument.Load(Flujo);
            }
        }

        // campos del RDLC
        [TestMethod]
        public void Test_Rdlc_EmbebidoConCamposDeLasTablas()
        {
            // Arrange
            ClsControladorReportePolizasContabilidad Controlador = ContabilidadFuncControlador();
            XDocument Rdlc = ContabilidadFuncLeerRdlc();
            XNamespace Ns = Rdlc.Root.Name.Namespace;

            // Act
            Dictionary<string, string[]> Campos = Rdlc.Descendants(Ns + "DataSet").ToDictionary(
                DataSet => (string)DataSet.Attribute("Name"),
                DataSet => DataSet.Descendants(Ns + "Field").Select(Campo => (string)Campo.Attribute("Name")).ToArray());
            string[] Lineas = Controlador.ContabilidadFuncTablaLineas(null).Columns.Cast<DataColumn>().Select(Columna => Columna.ColumnName).ToArray();
            string[] Totales = Controlador.ContabilidadFuncTablaTotales(null).Columns.Cast<DataColumn>().Select(Columna => Columna.ColumnName).ToArray();

            // Assert
            CollectionAssert.AreEquivalent(Lineas, Campos["DsPolizas"]);
            CollectionAssert.AreEquivalent(Totales, Campos["DsTotalesPolizas"]);
        }

        // render a PDF
        [TestMethod]
        public void Test_Rdlc_RenderizaPdfConDatosDeEjemplo()
        {
            // Arrange
            ClsControladorReportePolizasContabilidad Controlador = ContabilidadFuncControlador();
            ClsModeloReportePolizasContabilidad Datos = Controlador.ContabilidadFuncArmar(ContabilidadFuncLineasEjemplo());
            Datos.Filtros = Controlador.ContabilidadFuncTextoFiltros(new DateTime(2026, 11, 1), new DateTime(2026, 11, 30), "Todos", 0, 0);
            LocalReport Reporte = new LocalReport();

            // Act
            FrmReportePolizas.ContabilidadMetPrepararReporte(Reporte, Controlador.ContabilidadFuncTablaLineas(Datos),
                Controlador.ContabilidadFuncTablaTotales(Datos), FrmReportePolizas.ContabilidadFuncParametros(Datos.Filtros, "prueba"));
            byte[] Pdf = Reporte.Render("PDF");

            // Assert
            Assert.IsTrue(Pdf.Length > 1000);
            Assert.AreEqual("%PDF", Encoding.ASCII.GetString(Pdf, 0, 4));
            Reporte.Dispose();
        }

        // render sin datos
        [TestMethod]
        public void Test_Rdlc_RenderizaSinPolizas()
        {
            // Arrange
            ClsControladorReportePolizasContabilidad Controlador = ContabilidadFuncControlador();
            ClsModeloReportePolizasContabilidad Vacio = Controlador.ContabilidadFuncArmar(new List<ClsLineaReportePolizaContabilidad>());
            LocalReport Reporte = new LocalReport();

            // Act
            FrmReportePolizas.ContabilidadMetPrepararReporte(Reporte, Controlador.ContabilidadFuncTablaLineas(Vacio),
                Controlador.ContabilidadFuncTablaTotales(Vacio), FrmReportePolizas.ContabilidadFuncParametros("Sin datos", string.Empty));
            byte[] Pdf = Reporte.Render("PDF");

            // Assert
            Assert.AreEqual("%PDF", Encoding.ASCII.GetString(Pdf, 0, 4));
            Reporte.Dispose();
        }
    }
}
