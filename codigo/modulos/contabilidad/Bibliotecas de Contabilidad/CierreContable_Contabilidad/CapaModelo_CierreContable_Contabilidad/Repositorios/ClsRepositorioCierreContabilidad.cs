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
 *  Solo cierra si no quedan pólizas en BORRADOR o VALIDADA y si los
 *  períodos anteriores ya están cerrados. Si el período tiene pólizas
 *  aplicadas pero aún no tiene saldos, los arma desde esas pólizas.
 *  Todo ocurre en una sola transacción.
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

        // Inicio de cambio: Gabriel André Guillén Pocón - 0901-23-1998 - 10/11/2026
        // Deja tblCuenta lista para el período siguiente (columnas de acumulado del script de main)
        private const string _ReiniciarCuentas =
            "UPDATE tblCuenta SET saldoInicialCuenta = saldoActualCuenta, cargoPeriodoCuenta = 0, abonoPeriodoCuenta = 0, " +
            "cargoActualCuenta = IF(? = '01', 0, cargoActualCuenta), abonoActualCuenta = IF(? = '01', 0, abonoActualCuenta), " +
            "periodoSaldoCuenta = ? WHERE periodoSaldoCuenta <= ?";
        // Fin de cambio: Gabriel André Guillén Pocón - 0901-23-1998 - 10/11/2026

        // Inicio de cambio: Gabriel André Guillén Pocón - 0901-23-1998 - 10/11/2026
        // Arma los saldos del período desde sus pólizas aplicadas, subiendo a las cuentas padre
        private const string _GenerarSaldos =
            "INSERT INTO tblSaldoCuentaPeriodo (idCuenta, periodoContableSaldoCuentaPeriodo, saldoInicialSaldoCuentaPeriodo, " +
            "cargoSaldoCuentaPeriodo, abonoSaldoCuentaPeriodo, saldoFinalSaldoCuentaPeriodo, estadoSaldoCuentaPeriodo) " +
            "SELECT m.idCuenta, m.periodo, m.inicial, m.debe, m.haber, 0, 'ABIERTO' FROM (" +
            "WITH RECURSIVE movimiento AS (" +
            "SELECT pd.idCuenta, SUM(pd.debePolizaDetalle) AS debe, SUM(pd.haberPolizaDetalle) AS haber " +
            "FROM tblPolizaDetalle pd INNER JOIN tblPolizaEncabezado pe ON pe.idPolizaEncabezado = pd.idPolizaEncabezado " +
            "WHERE pe.periodoContablePolizaEncabezado = ? AND pe.estadoPolizaEncabezado = 'APLICADA' AND pe.is_active = TRUE " +
            "AND pd.is_active = TRUE GROUP BY pd.idCuenta" +
            "), ascenso AS (" +
            "SELECT idCuenta, debe, haber FROM movimiento UNION ALL " +
            "SELECT c.idCuentaPadre, a.debe, a.haber FROM ascenso a INNER JOIN tblCuenta c ON c.idCuenta = a.idCuenta WHERE c.idCuentaPadre IS NOT NULL" +
            ") SELECT a.idCuenta, ? AS periodo, SUM(a.debe) AS debe, SUM(a.haber) AS haber, " +
            "COALESCE((SELECT s.saldoFinalSaldoCuentaPeriodo FROM tblSaldoCuentaPeriodo s WHERE s.idCuenta = a.idCuenta " +
            "AND s.periodoContableSaldoCuentaPeriodo < ? ORDER BY s.periodoContableSaldoCuentaPeriodo DESC LIMIT 1), 0) AS inicial " +
            "FROM ascenso a INNER JOIN tblCuenta c ON c.idCuenta = a.idCuenta WHERE c.codigoCuenta <> '0' GROUP BY a.idCuenta" +
            ") AS m";

        // Calcula el saldo final según la naturaleza de la cuenta
        private const string _CalcularSaldoFinal =
            "UPDATE tblSaldoCuentaPeriodo s INNER JOIN tblCuenta c ON c.idCuenta = s.idCuenta SET s.saldoFinalSaldoCuentaPeriodo = " +
            "CASE WHEN c.tipoCuenta = 'DEUDORA' THEN s.saldoInicialSaldoCuentaPeriodo + s.cargoSaldoCuentaPeriodo - s.abonoSaldoCuentaPeriodo " +
            "ELSE s.saldoInicialSaldoCuentaPeriodo + s.abonoSaldoCuentaPeriodo - s.cargoSaldoCuentaPeriodo END " +
            "WHERE s.periodoContableSaldoCuentaPeriodo = ?";

        // Copia los saldos armados al catálogo de cuentas
        private const string _ActualizarCuentas =
            "UPDATE tblCuenta c INNER JOIN tblSaldoCuentaPeriodo s ON s.idCuenta = c.idCuenta " +
            "SET c.saldoInicialCuenta = s.saldoInicialSaldoCuentaPeriodo, c.cargoPeriodoCuenta = s.cargoSaldoCuentaPeriodo, " +
            "c.abonoPeriodoCuenta = s.abonoSaldoCuentaPeriodo, c.saldoActualCuenta = s.saldoFinalSaldoCuentaPeriodo, " +
            "c.periodoSaldoCuenta = s.periodoContableSaldoCuentaPeriodo WHERE s.periodoContableSaldoCuentaPeriodo = ?";

        // Pólizas aplicadas del período
        private const string _Aplicadas =
            "SELECT COUNT(*) FROM tblPolizaEncabezado WHERE periodoContablePolizaEncabezado = ? AND is_active = TRUE " +
            "AND estadoPolizaEncabezado = 'APLICADA'";
        // Fin de cambio: Gabriel André Guillén Pocón - 0901-23-1998 - 10/11/2026

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
                        // Inicio de cambio: Gabriel André Guillén Pocón - 0901-23-1998 - 10/11/2026
                        if (!Abierto)
                        {
                            if (Convert.ToInt32(ContabilidadFuncEscalar(Conexion, Transaccion, _Aplicadas, Periodo)) == 0)
                                throw new InvalidOperationException("El período " + Periodo + " no tiene pólizas aplicadas.");
                            ContabilidadFuncEjecutar(Conexion, Transaccion, _GenerarSaldos, Periodo, Periodo, Periodo);
                            ContabilidadFuncEjecutar(Conexion, Transaccion, _CalcularSaldoFinal, Periodo);
                            ContabilidadFuncEjecutar(Conexion, Transaccion, _ActualizarCuentas, Periodo);
                        }
                        // Fin de cambio: Gabriel André Guillén Pocón - 0901-23-1998 - 10/11/2026
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
