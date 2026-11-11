/*
 * ==================================================================
 * Área : Contabilidad
 * Autor : Donald Osorio
 * Carné : 0901-23-17982
 * Fecha : 17/10/2026
 * ==================================================================
 * Propósito :
 *  Reglas del cierre contable: pólizas de un período, resumen y
 *  diagnóstico que permite o bloquea el cierre. Responsable de la
 *  pantalla: Donald.
 * Reglas especificas:
 *  Solo se cierra un período sin borradores ni pólizas validadas
 *  pendientes de aplicar. El cierre lo ejecuta el repositorio de cierre.
 * ===================================================================
*/

using System;
using System.Data;
using System.Linq;
using CapaModelo_Contabilidad.Contratos;
using CapaModelo_Contabilidad.Repositorios;
// Inicio de cambio: Mario Taracena - 0901-23-9335 - 27/10/2026
using CapaModelo_Contabilidad.Entidades;
// Fin de cambio: Mario Taracena - 0901-23-9335 - 27/10/2026

// Inicio de cambio: Mario Taracena - 0901-23-9335 - 27/10/2026
namespace CapaControlador_Contabilidad
// Fin de cambio: Mario Taracena - 0901-23-9335 - 27/10/2026
{
    public class ClsControladorCierreContabilidad
    {
        private readonly IRepositorioPolizasContabilidad _Repositorio = new ClsRepositorioPolizasContabilidad();
        // Inicio de cambio: Donald Osorio - 0901-23-17982 - 28/10/2026
        private readonly ClsRepositorioCierreContabilidad _Cierre = new ClsRepositorioCierreContabilidad();

        public string ContabilidadFuncEstadoPeriodo(DateTime Periodo)
        {
            return _Cierre.ContabilidadFuncEstadoPeriodo(Periodo.ToString("yyyy-MM"));
        }

        public bool ContabilidadFuncCerrarPeriodo(DateTime Periodo, out string Mensaje)
        {
            try
            {
                string Siguiente = _Cierre.ContabilidadFuncCerrarPeriodo(Periodo.ToString("yyyy-MM"));
                Mensaje = "Período " + Periodo.ToString("yyyy-MM") + " cerrado. Los saldos finales pasaron como saldos iniciales de " + Siguiente + ".";
                return true;
            }
            catch (Exception Excepcion)
            {
                Mensaje = Excepcion.Message;
                return false;
            }
        }
        // Fin de cambio: Donald Osorio - 0901-23-17982 - 28/10/2026

        public DataTable ContabilidadFuncPolizas(DateTime Periodo)
        {
            DataTable Resultado = new DataTable();
            Resultado.Columns.Add("Poliza", typeof(int));
            Resultado.Columns.Add("Fecha", typeof(DateTime));
            Resultado.Columns.Add("Estado", typeof(string));
            Resultado.Columns.Add("Debe", typeof(decimal));
            Resultado.Columns.Add("Haber", typeof(decimal));
            Resultado.Columns.Add("Diferencia", typeof(decimal));
            string Mes = Periodo.ToString("yyyy-MM");
            foreach (ClsPolizaContabilidad Poliza in _Repositorio.ContabilidadFuncObtenerTodos().Where(Elemento => Elemento.PeriodoContable == Mes))
            {
                decimal Debe = Poliza.Detalle.Sum(Linea => Linea.Debe);
                decimal Haber = Poliza.Detalle.Sum(Linea => Linea.Haber);
                Resultado.Rows.Add(Poliza.IdPoliza, Poliza.Fecha, Poliza.Estado, Debe, Haber, Debe - Haber);
            }
            return Resultado;
        }

        public string ContabilidadFuncValidarCierre(DataTable Polizas)
        {
            if (Polizas.Rows.Count == 0) return "El período no tiene pólizas para revisar.";
            foreach (DataRow Fila in Polizas.Rows)
            {
                if ((string)Fila["Estado"] == "BORRADOR") return "Hay pólizas en borrador. Revise y valide o anule antes del cierre.";
                // Inicio de cambio: Donald Osorio - 0901-23-17982 - 28/10/2026
                if ((string)Fila["Estado"] == "VALIDADA") return "Hay pólizas validadas sin aplicar. Ejecute la actualización de saldos antes del cierre.";
                if (ContabilidadFuncEsFirme(Fila) && (decimal)Fila["Diferencia"] != 0m) return "Hay pólizas aplicadas descuadradas.";
                // Fin de cambio: Donald Osorio - 0901-23-17982 - 28/10/2026
            }
            // Inicio de cambio: Donald Osorio - 0901-23-17982 - 28/10/2026
            if (!Polizas.Rows.Cast<DataRow>().Any(ContabilidadFuncEsFirme)) return "El período no tiene pólizas aplicadas.";
            // Fin de cambio: Donald Osorio - 0901-23-17982 - 28/10/2026
            return string.Empty;
        }

        // Inicio de cambio: Mario Taracena - 0901-23-9335 - 27/10/2026
        public ClsModeloResumenCierreContabilidad ContabilidadFuncResumenCierre(DataTable Polizas)
        // Fin de cambio: Mario Taracena - 0901-23-9335 - 27/10/2026
        {
            var Filas = Polizas.Rows.Cast<DataRow>();
            var Aplicadas = Filas.Where(ContabilidadFuncEsFirme).ToList();
            // Inicio de cambio: Mario Taracena - 0901-23-9335 - 27/10/2026
            return new ClsModeloResumenCierreContabilidad
            // Fin de cambio: Mario Taracena - 0901-23-9335 - 27/10/2026
            {
                Aplicadas = Aplicadas.Count, Borradores = Filas.Count(Fila => (string)Fila["Estado"] == "BORRADOR"),
                Debe = Aplicadas.Sum(Fila => (decimal)Fila["Debe"]), Haber = Aplicadas.Sum(Fila => (decimal)Fila["Haber"])
            };
        }

        private static bool ContabilidadFuncEsFirme(DataRow Fila)
        {
            // Inicio de cambio: Donald Osorio - 0901-23-17982 - 28/10/2026
            return (string)Fila["Estado"] == "APLICADA";
            // Fin de cambio: Donald Osorio - 0901-23-17982 - 28/10/2026
        }
    }
}
