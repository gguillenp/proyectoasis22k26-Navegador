/*
 * ==================================================================
 * Área : Contabilidad
 * Autor : Oskar Cermeño
 * Carné : 0901-23-15379
 * Fecha : 11/11/2026
 * ==================================================================
 * Propósito :
 *  Consulta del reporte de pólizas (libro diario): encabezados de
 *  tblPolizaEncabezado con sus líneas de tblPolizaDetalle, la cuenta,
 *  el módulo de origen y el país con su moneda.
 * Reglas especificas:
 *  Solo lectura. Las pólizas anuladas quedan con is_active en FALSE,
 *  por eso se incluyen aparte. De las líneas solo cuentan las activas.
 *  Estado vacío, módulo 0 y país 0 no filtran.
 * ===================================================================
*/

using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Odbc;
using CapaModelo_Contabilidad.Contratos;
using CapaModelo_Contabilidad.Entidades;

namespace CapaModelo_Contabilidad.Repositorios
{
    // repositorio del reporte
    public class ClsRepositorioReportePolizasContabilidad : ClsRepositorioMaestroContabilidad, IRepositorioReportePolizasContabilidad
    {
        // consulta base
        private const string _SelectLineas =
            "SELECT pe.idPolizaEncabezado, pe.codigoPolizaEncabezado, pe.fechaPolizaEncabezado, pe.periodoContablePolizaEncabezado, " +
            "pe.estadoPolizaEncabezado, m.nombreModuloContable, p.nombrePais, p.codigoMonedaPais, pe.documentoOrigenPolizaEncabezado, " +
            "pe.descripcionPolizaEncabezado, pd.lineaPolizaDetalle, c.codigoCuenta, c.nombreCuenta, pd.descripcionPolizaDetalle, " +
            "pd.debePolizaDetalle, pd.haberPolizaDetalle " +
            "FROM tblPolizaEncabezado pe " +
            "INNER JOIN tblModuloContable m ON m.idModuloContable = pe.idModuloContable " +
            "LEFT JOIN tblPais p ON p.idPais = pe.idPais " +
            "LEFT JOIN tblPolizaDetalle pd ON pd.idPolizaEncabezado = pe.idPolizaEncabezado AND pd.is_active = TRUE " +
            "LEFT JOIN tblCuenta c ON c.idCuenta = pd.idCuenta " +
            "WHERE (pe.is_active = TRUE OR pe.estadoPolizaEncabezado = 'ANULADA') " +
            "AND pe.fechaPolizaEncabezado BETWEEN ? AND ?";

        // orden del libro diario
        private const string _Orden =
            " ORDER BY pe.fechaPolizaEncabezado, pe.codigoPolizaEncabezado, pe.idPolizaEncabezado, pd.lineaPolizaDetalle";

        // arma la consulta con sus filtros
        public static string ContabilidadFuncConsulta(DateTime Desde, DateTime Hasta, string Estado, int IdModulo, int IdPais, List<OdbcParameter> Parametros)
        {
            string Consulta = _SelectLineas;
            Parametros.Add(new OdbcParameter("Desde", OdbcType.Date) { Value = Desde.Date });
            Parametros.Add(new OdbcParameter("Hasta", OdbcType.Date) { Value = Hasta.Date });
            if (!string.IsNullOrWhiteSpace(Estado))
            {
                Consulta += " AND pe.estadoPolizaEncabezado = ?";
                Parametros.Add(new OdbcParameter("Estado", Estado));
            }
            if (IdModulo > 0)
            {
                Consulta += " AND pe.idModuloContable = ?";
                Parametros.Add(new OdbcParameter("Modulo", IdModulo));
            }
            if (IdPais > 0)
            {
                Consulta += " AND pe.idPais = ?";
                Parametros.Add(new OdbcParameter("Pais", IdPais));
            }
            return Consulta + _Orden;
        }

        // líneas filtradas
        public List<ClsLineaReportePolizaContabilidad> ContabilidadFuncObtenerLineas(DateTime Desde, DateTime Hasta, string Estado, int IdModulo, int IdPais)
        {
            List<OdbcParameter> Parametros = new List<OdbcParameter>();
            string Consulta = ContabilidadFuncConsulta(Desde, Hasta, Estado, IdModulo, IdPais, Parametros);
            using (DataTable Tabla = ContabilidadFuncEjecutarConsulta(Consulta, CommandType.Text, Parametros))
                return ContabilidadFuncMapear(Tabla);
        }

        // pasa filas a entidades
        public static List<ClsLineaReportePolizaContabilidad> ContabilidadFuncMapear(DataTable Tabla)
        {
            List<ClsLineaReportePolizaContabilidad> Lineas = new List<ClsLineaReportePolizaContabilidad>();
            foreach (DataRow Fila in Tabla.Rows)
            {
                bool SinLinea = Fila["lineaPolizaDetalle"] == DBNull.Value;
                Lineas.Add(new ClsLineaReportePolizaContabilidad
                {
                    IdPoliza = Convert.ToInt32(Fila["idPolizaEncabezado"]),
                    CodigoPoliza = Convert.ToString(Fila["codigoPolizaEncabezado"]),
                    Fecha = Convert.ToDateTime(Fila["fechaPolizaEncabezado"]),
                    Periodo = Convert.ToString(Fila["periodoContablePolizaEncabezado"]),
                    Estado = Convert.ToString(Fila["estadoPolizaEncabezado"]),
                    Modulo = Convert.ToString(Fila["nombreModuloContable"]),
                    Pais = Convert.ToString(Fila["nombrePais"]),
                    Moneda = Fila["codigoMonedaPais"] == DBNull.Value ? "GTQ" : Convert.ToString(Fila["codigoMonedaPais"]),
                    DocumentoOrigen = Convert.ToString(Fila["documentoOrigenPolizaEncabezado"]),
                    Descripcion = Convert.ToString(Fila["descripcionPolizaEncabezado"]),
                    Linea = SinLinea ? 0 : Convert.ToInt32(Fila["lineaPolizaDetalle"]),
                    CodigoCuenta = Convert.ToString(Fila["codigoCuenta"]),
                    NombreCuenta = Convert.ToString(Fila["nombreCuenta"]),
                    DescripcionLinea = Convert.ToString(Fila["descripcionPolizaDetalle"]),
                    Debe = SinLinea ? 0m : Convert.ToDecimal(Fila["debePolizaDetalle"]),
                    Haber = SinLinea ? 0m : Convert.ToDecimal(Fila["haberPolizaDetalle"])
                });
            }
            return Lineas;
        }

        // módulos contables
        public Dictionary<int, string> ContabilidadFuncObtenerModulos()
        {
            return ContabilidadFuncCatalogo("SELECT idModuloContable, nombreModuloContable FROM tblModuloContable WHERE is_active = TRUE ORDER BY idModuloContable");
        }

        // países
        public Dictionary<int, string> ContabilidadFuncObtenerPaises()
        {
            return ContabilidadFuncCatalogo("SELECT idPais, nombrePais FROM tblPais WHERE is_active = TRUE ORDER BY idPais");
        }

        // lee un catálogo id y nombre
        private Dictionary<int, string> ContabilidadFuncCatalogo(string Consulta)
        {
            Dictionary<int, string> Catalogo = new Dictionary<int, string>();
            using (DataTable Tabla = ContabilidadFuncEjecutarConsulta(Consulta, CommandType.Text))
                foreach (DataRow Fila in Tabla.Rows) Catalogo[Convert.ToInt32(Fila[0])] = Convert.ToString(Fila[1]);
            return Catalogo;
        }
    }
}
