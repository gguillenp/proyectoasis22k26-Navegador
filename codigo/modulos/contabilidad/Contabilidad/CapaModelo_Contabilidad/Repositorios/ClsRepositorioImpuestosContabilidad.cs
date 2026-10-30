/*
 * ==================================================================
 * Área : Contabilidad
 * Autor : Gabriel André Guillén Pocón
 * Carné : 0901-23-1998
 * Fecha : 30/10/2026
 * ==================================================================
 * Propósito :
 *  Lee de tblImpuesto los impuestos vigentes a una fecha, de un país o
 *  de todos, con el código de sus cuentas contables.
 * Reglas especificas:
 *  País 0 trae todos los países. Cada fila es un tramo; los impuestos
 *  de tasa única tienen un solo tramo.
 * ===================================================================
*/

using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Odbc;
using CapaModelo_Contabilidad.Entidades;

namespace CapaModelo_Contabilidad.Repositorios
{
    public class ClsRepositorioImpuestosContabilidad : ClsRepositorioMaestroContabilidad
    {
        private const string _SelectVigentes =
            "SELECT i.idImpuesto, i.idPais, p.nombrePais, i.codigoImpuesto, i.nombreImpuesto, i.tipoImpuesto, i.operacionImpuesto, " +
            "i.tasaImpuesto, i.montoDesdeImpuesto, i.montoHastaImpuesto, i.importeFijoImpuesto, i.sobreExcedenteImpuesto, " +
            "ca.codigoCuenta AS cuentaAcreditable, cp.codigoCuenta AS cuentaPorPagar, i.baseLegalImpuesto, " +
            "i.fechaInicioVigenciaImpuesto, i.fechaFinVigenciaImpuesto FROM tblImpuesto i " +
            "INNER JOIN tblPais p ON p.idPais = i.idPais " +
            "LEFT JOIN tblCuenta ca ON ca.idCuenta = i.idCuentaAcreditableImpuesto " +
            "LEFT JOIN tblCuenta cp ON cp.idCuenta = i.idCuentaPorPagarImpuesto " +
            "WHERE i.is_active = TRUE AND (? = 0 OR i.idPais = ?) AND i.fechaInicioVigenciaImpuesto <= ? " +
            "AND (i.fechaFinVigenciaImpuesto IS NULL OR i.fechaFinVigenciaImpuesto >= ?) " +
            "ORDER BY i.idPais, i.codigoImpuesto, i.montoDesdeImpuesto";

        public List<ClsImpuestoContabilidad> ContabilidadFuncObtenerVigentes(int IdPais, DateTime Fecha)
        {
            List<ClsImpuestoContabilidad> Impuestos = new List<ClsImpuestoContabilidad>();
            List<OdbcParameter> Parametros = new List<OdbcParameter>
            {
                new OdbcParameter("Pais", IdPais), new OdbcParameter("PaisFiltro", IdPais),
                new OdbcParameter("Desde", OdbcType.Date) { Value = Fecha.Date }, new OdbcParameter("Hasta", OdbcType.Date) { Value = Fecha.Date }
            };
            using (DataTable Tabla = ContabilidadFuncEjecutarConsulta(_SelectVigentes, CommandType.Text, Parametros))
                foreach (DataRow Fila in Tabla.Rows)
                    Impuestos.Add(new ClsImpuestoContabilidad
                    {
                        IdImpuesto = Convert.ToInt32(Fila["idImpuesto"]),
                        IdPais = Convert.ToInt32(Fila["idPais"]),
                        Pais = Convert.ToString(Fila["nombrePais"]),
                        Codigo = Convert.ToString(Fila["codigoImpuesto"]),
                        Nombre = Convert.ToString(Fila["nombreImpuesto"]),
                        Tipo = Convert.ToString(Fila["tipoImpuesto"]),
                        Operacion = Convert.ToString(Fila["operacionImpuesto"]),
                        Tasa = Convert.ToDecimal(Fila["tasaImpuesto"]),
                        MontoDesde = Convert.ToDecimal(Fila["montoDesdeImpuesto"]),
                        MontoHasta = Fila["montoHastaImpuesto"] == DBNull.Value ? (decimal?)null : Convert.ToDecimal(Fila["montoHastaImpuesto"]),
                        ImporteFijo = Convert.ToDecimal(Fila["importeFijoImpuesto"]),
                        SobreExcedente = Convert.ToBoolean(Fila["sobreExcedenteImpuesto"]),
                        CuentaAcreditable = Fila["cuentaAcreditable"] == DBNull.Value ? null : Convert.ToString(Fila["cuentaAcreditable"]),
                        CuentaPorPagar = Fila["cuentaPorPagar"] == DBNull.Value ? null : Convert.ToString(Fila["cuentaPorPagar"]),
                        BaseLegal = Fila["baseLegalImpuesto"] == DBNull.Value ? null : Convert.ToString(Fila["baseLegalImpuesto"]),
                        VigenteDesde = Convert.ToDateTime(Fila["fechaInicioVigenciaImpuesto"]),
                        VigenteHasta = Fila["fechaFinVigenciaImpuesto"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(Fila["fechaFinVigenciaImpuesto"])
                    });
            return Impuestos;
        }
    }
}
