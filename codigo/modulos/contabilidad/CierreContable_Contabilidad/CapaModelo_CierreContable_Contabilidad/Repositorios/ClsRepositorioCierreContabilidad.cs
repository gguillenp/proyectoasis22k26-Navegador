/*
 * ==================================================================
 * Área : Contabilidad
 * Autor : Donald Osorio
 * Carné : 0901-23-17982
 * Fecha : 28/10/2026
 * ==================================================================
 * Propósito :
 *  Cierre de un período contable sobre tblSaldoCuentaPeriodo: marca el
 *  período como CERRADO, abre el siguiente con los saldos finales como
 *  saldos iniciales y deja tblCuenta lista para el nuevo período.
 * Reglas especificas:
 *  Solo cierra si no quedan pólizas en BORRADOR o VALIDADA, si los
 *  períodos anteriores ya están cerrados y si el período tiene saldos
 *  actualizados. Todo ocurre en una sola transacción.
 * ===================================================================
*/

using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Odbc;
using System.Globalization;

namespace CapaModelo_Contabilidad.Repositorios
{
    public class ClsRepositorioCierreContabilidad : ClsRepositorioMaestroContabilidad
    {
        private const string _Pendientes =
            "SELECT COUNT(*) FROM tblPolizaEncabezado WHERE periodoContablePolizaEncabezado = ? AND is_active = TRUE " +
            "AND estadoPolizaEncabezado IN ('BORRADOR','VALIDADA')";

        private const string _EstadoPeriodo =
            "SELECT estadoSaldoCuentaPeriodo, COUNT(*) FROM tblSaldoCuentaPeriodo WHERE periodoContableSaldoCuentaPeriodo = ? " +
            "GROUP BY estadoSaldoCuentaPeriodo";

        private const string _AnteriorAbierto =
            "SELECT MIN(periodoContableSaldoCuentaPeriodo) FROM tblSaldoCuentaPeriodo WHERE estadoSaldoCuentaPeriodo = 'ABIERTO' " +
            "AND periodoContableSaldoCuentaPeriodo < ?";

        private const string _Cerrar =
            "UPDATE tblSaldoCuentaPeriodo SET estadoSaldoCuentaPeriodo = 'CERRADO' WHERE periodoContableSaldoCuentaPeriodo = ?";

        private const string _AbrirSiguiente =
            "INSERT INTO tblSaldoCuentaPeriodo (idCuenta, periodoContableSaldoCuentaPeriodo, saldoInicialSaldoCuentaPeriodo, " +
            "cargoSaldoCuentaPeriodo, abonoSaldoCuentaPeriodo, saldoFinalSaldoCuentaPeriodo, estadoSaldoCuentaPeriodo) " +
            "SELECT s.idCuenta, ?, s.saldoFinalSaldoCuentaPeriodo, 0, 0, s.saldoFinalSaldoCuentaPeriodo, 'ABIERTO' " +
            "FROM tblSaldoCuentaPeriodo s WHERE s.periodoContableSaldoCuentaPeriodo = (SELECT MAX(u.periodoContableSaldoCuentaPeriodo) " +
            "FROM tblSaldoCuentaPeriodo u WHERE u.idCuenta = s.idCuenta AND u.periodoContableSaldoCuentaPeriodo <= ?) " +
            "ON DUPLICATE KEY UPDATE tblSaldoCuentaPeriodo.idCuenta = tblSaldoCuentaPeriodo.idCuenta";

        private const string _ReiniciarCuentas =
            "UPDATE tblCuenta SET saldoInicialCuenta = saldoActualCuenta, cargoPeriodoCuenta = 0, abonoPeriodoCuenta = 0, " +
            "cargoAcumuladoCuenta = IF(? = '01', 0, cargoAcumuladoCuenta), abonoAcumuladoCuenta = IF(? = '01', 0, abonoAcumuladoCuenta), " +
            "periodoSaldoCuenta = ? WHERE periodoSaldoCuenta <= ?";

        public string ContabilidadFuncEstadoPeriodo(string Periodo)
        {
            using (DataTable Tabla = ContabilidadFuncEjecutarConsulta(_EstadoPeriodo, CommandType.Text,
                new List<OdbcParameter> { new OdbcParameter("Periodo", Periodo) }))
            {
                if (Tabla.Rows.Count == 0) return string.Empty;
                foreach (DataRow Fila in Tabla.Rows)
                    if (Convert.ToString(Fila[0]) == "ABIERTO") return "ABIERTO";
                return "CERRADO";
            }
        }

        public string ContabilidadFuncCerrarPeriodo(string Periodo)
        {
            string Siguiente = DateTime.ParseExact(Periodo + "-01", "yyyy-MM-dd", CultureInfo.InvariantCulture).AddMonths(1).ToString("yyyy-MM");
            using (OdbcConnection Conexion = ContabilidadFuncObtenerConexion())
            {
                Conexion.Open();
                using (OdbcTransaction Transaccion = Conexion.BeginTransaction())
                {
                    try
                    {
                        if (Convert.ToInt32(ContabilidadFuncEscalar(Conexion, Transaccion, _Pendientes, Periodo)) > 0)
                            throw new InvalidOperationException("Hay pólizas en borrador o validadas sin aplicar en " + Periodo + ". Valídelas, aplique el batch o anúlelas antes del cierre.");
                        bool Abierto = false, Cerrado = false;
                        using (OdbcCommand Comando = new OdbcCommand(_EstadoPeriodo, Conexion, Transaccion))
                        {
                            Comando.Parameters.AddWithValue("Periodo", Periodo);
                            using (OdbcDataReader Lector = Comando.ExecuteReader())
                                while (Lector.Read())
                                {
                                    if (Lector.GetString(0) == "ABIERTO") Abierto = true;
                                    else Cerrado = true;
                                }
                        }
                        if (!Abierto && Cerrado) throw new InvalidOperationException("El período " + Periodo + " ya está cerrado.");
                        if (!Abierto) throw new InvalidOperationException("El período " + Periodo + " no tiene saldos actualizados. Aplique sus pólizas antes del cierre.");
                        object Anterior = ContabilidadFuncEscalar(Conexion, Transaccion, _AnteriorAbierto, Periodo);
                        if (Anterior != null && Anterior != DBNull.Value)
                            throw new InvalidOperationException("Primero debe cerrar el período " + Convert.ToString(Anterior) + ".");

                        ContabilidadFuncEjecutar(Conexion, Transaccion, _Cerrar, Periodo);
                        ContabilidadFuncEjecutar(Conexion, Transaccion, _AbrirSiguiente, Siguiente, Periodo);
                        string MesSiguiente = Siguiente.Substring(5, 2);
                        ContabilidadFuncEjecutar(Conexion, Transaccion, _ReiniciarCuentas, MesSiguiente, MesSiguiente, Siguiente, Periodo);
                        Transaccion.Commit();
                        return Siguiente;
                    }
                    catch
                    {
                        Transaccion.Rollback();
                        throw;
                    }
                }
            }
        }

        private static object ContabilidadFuncEscalar(OdbcConnection Conexion, OdbcTransaction Transaccion, string Sentencia, string Periodo)
        {
            using (OdbcCommand Comando = new OdbcCommand(Sentencia, Conexion, Transaccion))
            {
                Comando.Parameters.AddWithValue("Periodo", Periodo);
                return Comando.ExecuteScalar();
            }
        }

        private static int ContabilidadFuncEjecutar(OdbcConnection Conexion, OdbcTransaction Transaccion, string Sentencia, params string[] Valores)
        {
            using (OdbcCommand Comando = new OdbcCommand(Sentencia, Conexion, Transaccion))
            {
                foreach (string Valor in Valores) Comando.Parameters.AddWithValue("Valor", Valor);
                return Comando.ExecuteNonQuery();
            }
        }
    }
}
