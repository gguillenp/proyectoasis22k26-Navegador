/*
 * ==================================================================
 * Área : Contabilidad
 * Autor : Oskar Cermeño
 * Carné : 0901-23-15379
 * Fecha : 11/11/2026
 * ==================================================================
 * Propósito :
 *  Resultado del reporte de pólizas: filas, totales por moneda y el
 *  texto de los filtros con que se generó.
 * Reglas especificas:
 *  Lo arma ClsControladorReportePolizasContabilidad.
 * ===================================================================
*/

using System.Collections.Generic;
using System.Linq;

namespace CapaControlador_Contabilidad
{
    // datos del reporte
    public class ClsModeloReportePolizasContabilidad
    {
        // filas del reporte
        public List<ClsModeloLineaReportePolizaContabilidad> Lineas { get; set; } = new List<ClsModeloLineaReportePolizaContabilidad>();
        // totales por moneda
        public List<ClsModeloTotalReportePolizaContabilidad> Totales { get; set; } = new List<ClsModeloTotalReportePolizaContabilidad>();
        // texto de filtros
        public string Filtros { get; set; } = string.Empty;
        // usuario que genera
        public string Usuario { get; set; } = string.Empty;

        // pólizas distintas
        public int CantidadPolizas
        {
            get { return Lineas.Select(Linea => Linea.IdPoliza).Distinct().Count(); }
        }
    }
}
