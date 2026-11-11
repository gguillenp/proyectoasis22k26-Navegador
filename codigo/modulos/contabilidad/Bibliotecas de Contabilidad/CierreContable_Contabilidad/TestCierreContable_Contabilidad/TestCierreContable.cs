/*
 * ==================================================================
 * Área : Contabilidad
 * Autor : Donald Osorio
 * Carné : 0901-23-17982
 * Fecha : 03/11/2026
 * ==================================================================
 * Propósito :
 *  Pruebas unitarias (MSTest) de la lógica del cierre contable que
 *  no depende de la base de datos: ContabilidadFuncValidarCierre y
 *  ContabilidadFuncResumenCierre de ClsControladorCierreContabilidad.
 *  Siguen el patrón Arrange / Act / Assert del ejemplo TestCalculadora.
 * ===================================================================
*/

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Data;
using CapaControlador_Contabilidad;
using CapaModelo_Contabilidad.Entidades;

namespace TestCierreContable_Contabilidad
{
    [TestClass]
    public class TestCierreContable
    {
        // Metodo auxiliar: arma la tabla con las mismas columnas que devuelve ContabilidadFuncPolizas
        private static DataTable CrearTablaPolizas()
        {
            DataTable Tabla = new DataTable();
            Tabla.Columns.Add("Poliza", typeof(int));
            Tabla.Columns.Add("Fecha", typeof(DateTime));
            Tabla.Columns.Add("Estado", typeof(string));
            Tabla.Columns.Add("Debe", typeof(decimal));
            Tabla.Columns.Add("Haber", typeof(decimal));
            Tabla.Columns.Add("Diferencia", typeof(decimal));
            return Tabla;
        }

        private static void AgregarPoliza(DataTable Tabla, int Id, string Estado, decimal Debe, decimal Haber)
        {
            Tabla.Rows.Add(Id, new DateTime(2026, 10, 15), Estado, Debe, Haber, Debe - Haber);
        }

        // ---------------- ContabilidadFuncValidarCierre ----------------

        [TestMethod]
        public void Test_ValidarCierre_PeriodoSinPolizas()
        {
            //Arrange: inicializar las variables
            ClsControladorCierreContabilidad Controlador = new ClsControladorCierreContabilidad();
            DataTable Polizas = CrearTablaPolizas();
            //Act: ejecucion del metodo a probar
            string resultado = Controlador.ContabilidadFuncValidarCierre(Polizas);
            //Assert: comprobacion de los resultados
            Assert.AreEqual("El período no tiene pólizas para revisar.", resultado);
        }

        [TestMethod]
        public void Test_ValidarCierre_ConPolizaBorrador()
        {
            //Arrange: inicializar las variables
            ClsControladorCierreContabilidad Controlador = new ClsControladorCierreContabilidad();
            DataTable Polizas = CrearTablaPolizas();
            AgregarPoliza(Polizas, 1, "APLICADA", 500m, 500m);
            AgregarPoliza(Polizas, 2, "BORRADOR", 100m, 100m);
            //Act: ejecucion del metodo a probar
            string resultado = Controlador.ContabilidadFuncValidarCierre(Polizas);
            //Assert: comprobacion de los resultados
            Assert.AreEqual("Hay pólizas en borrador. Revise y valide o anule antes del cierre.", resultado);
        }

        [TestMethod]
        public void Test_ValidarCierre_ConPolizaValidadaSinAplicar()
        {
            //Arrange: inicializar las variables
            ClsControladorCierreContabilidad Controlador = new ClsControladorCierreContabilidad();
            DataTable Polizas = CrearTablaPolizas();
            AgregarPoliza(Polizas, 1, "APLICADA", 500m, 500m);
            AgregarPoliza(Polizas, 2, "VALIDADA", 200m, 200m);
            //Act: ejecucion del metodo a probar
            string resultado = Controlador.ContabilidadFuncValidarCierre(Polizas);
            //Assert: comprobacion de los resultados
            Assert.AreEqual("Hay pólizas validadas sin aplicar. Ejecute la actualización de saldos antes del cierre.", resultado);
        }

        [TestMethod]
        public void Test_ValidarCierre_PolizaAplicadaDescuadrada()
        {
            //Arrange: inicializar las variables
            ClsControladorCierreContabilidad Controlador = new ClsControladorCierreContabilidad();
            DataTable Polizas = CrearTablaPolizas();
            AgregarPoliza(Polizas, 1, "APLICADA", 500m, 450m);
            //Act: ejecucion del metodo a probar
            string resultado = Controlador.ContabilidadFuncValidarCierre(Polizas);
            //Assert: comprobacion de los resultados
            Assert.AreEqual("Hay pólizas aplicadas descuadradas.", resultado);
        }

        [TestMethod]
        public void Test_ValidarCierre_SinPolizasAplicadas()
        {
            //Arrange: inicializar las variables (solo hay una poliza anulada)
            ClsControladorCierreContabilidad Controlador = new ClsControladorCierreContabilidad();
            DataTable Polizas = CrearTablaPolizas();
            AgregarPoliza(Polizas, 1, "ANULADA", 300m, 300m);
            //Act: ejecucion del metodo a probar
            string resultado = Controlador.ContabilidadFuncValidarCierre(Polizas);
            //Assert: comprobacion de los resultados
            Assert.AreEqual("El período no tiene pólizas aplicadas.", resultado);
        }

        [TestMethod]
        public void Test_ValidarCierre_PeriodoListoParaCerrar()
        {
            //Arrange: inicializar las variables
            ClsControladorCierreContabilidad Controlador = new ClsControladorCierreContabilidad();
            DataTable Polizas = CrearTablaPolizas();
            AgregarPoliza(Polizas, 1, "APLICADA", 500m, 500m);
            AgregarPoliza(Polizas, 2, "APLICADA", 1250.75m, 1250.75m);
            //Act: ejecucion del metodo a probar
            string resultado = Controlador.ContabilidadFuncValidarCierre(Polizas);
            //Assert: comprobacion de los resultados (cadena vacia = se puede cerrar)
            Assert.AreEqual(string.Empty, resultado);
        }

        [TestMethod]
        public void Test_ValidarCierre_AnuladaNoBloqueaElCierre()
        {
            //Arrange: inicializar las variables
            ClsControladorCierreContabilidad Controlador = new ClsControladorCierreContabilidad();
            DataTable Polizas = CrearTablaPolizas();
            AgregarPoliza(Polizas, 1, "APLICADA", 800m, 800m);
            AgregarPoliza(Polizas, 2, "ANULADA", 999m, 0m);
            //Act: ejecucion del metodo a probar
            string resultado = Controlador.ContabilidadFuncValidarCierre(Polizas);
            //Assert: comprobacion de los resultados
            Assert.AreEqual(string.Empty, resultado);
        }

        // ---------------- ContabilidadFuncResumenCierre ----------------

        [TestMethod]
        public void Test_ResumenCierre_SumaSoloPolizasAplicadas()
        {
            //Arrange: inicializar las variables
            ClsControladorCierreContabilidad Controlador = new ClsControladorCierreContabilidad();
            DataTable Polizas = CrearTablaPolizas();
            AgregarPoliza(Polizas, 1, "APLICADA", 500m, 500m);
            AgregarPoliza(Polizas, 2, "APLICADA", 250m, 250m);
            AgregarPoliza(Polizas, 3, "BORRADOR", 900m, 900m);
            AgregarPoliza(Polizas, 4, "ANULADA", 100m, 100m);
            //Act: ejecucion del metodo a probar
            ClsModeloResumenCierreContabilidad resumen = Controlador.ContabilidadFuncResumenCierre(Polizas);
            //Assert: comprobacion de los resultados
            Assert.AreEqual(2, resumen.Aplicadas);
            Assert.AreEqual(1, resumen.Borradores);
            Assert.AreEqual(750m, resumen.Debe);
            Assert.AreEqual(750m, resumen.Haber);
        }

        [TestMethod]
        public void Test_ResumenCierre_TablaVacia()
        {
            //Arrange: inicializar las variables
            ClsControladorCierreContabilidad Controlador = new ClsControladorCierreContabilidad();
            DataTable Polizas = CrearTablaPolizas();
            //Act: ejecucion del metodo a probar
            ClsModeloResumenCierreContabilidad resumen = Controlador.ContabilidadFuncResumenCierre(Polizas);
            //Assert: comprobacion de los resultados
            Assert.AreEqual(0, resumen.Aplicadas);
            Assert.AreEqual(0, resumen.Borradores);
            Assert.AreEqual(0m, resumen.Debe);
            Assert.AreEqual(0m, resumen.Haber);
        }

        [TestMethod]
        public void Test_ResumenCierre_ValidadasNoCuentanComoAplicadas()
        {
            //Arrange: inicializar las variables
            ClsControladorCierreContabilidad Controlador = new ClsControladorCierreContabilidad();
            DataTable Polizas = CrearTablaPolizas();
            AgregarPoliza(Polizas, 1, "VALIDADA", 400m, 400m);
            //Act: ejecucion del metodo a probar
            ClsModeloResumenCierreContabilidad resumen = Controlador.ContabilidadFuncResumenCierre(Polizas);
            //Assert: comprobacion de los resultados
            Assert.AreEqual(0, resumen.Aplicadas);
            Assert.AreEqual(0m, resumen.Debe);
        }
    }
}
