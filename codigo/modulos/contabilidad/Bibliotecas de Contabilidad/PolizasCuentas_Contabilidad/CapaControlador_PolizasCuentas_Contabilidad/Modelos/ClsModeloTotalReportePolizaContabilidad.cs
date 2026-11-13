/*
 * ==================================================================
 * Área : Contabilidad
 * Autor : Oskar Cermeño
 * Carné : 0901-23-15379
 * Fecha : 11/11/2026
 * ==================================================================
 * Propósito :
 *  Totales generales del reporte de pólizas para una moneda.
 * Reglas especificas:
 *  Monedas distintas no se suman entre sí. Las pólizas anuladas solo
 *  se cuentan; sus importes no entran en los totales.
 * ===================================================================
*/

namespace CapaControlador_Contabilidad
{
    // totales por moneda
    public class ClsModeloTotalReportePolizaContabilidad
    {
        // código de moneda
        public string Moneda { get; set; }
        // pólizas que suman
        public int Polizas { get; set; }
        // total al debe
        public decimal Debe { get; set; }
        // total al haber
        public decimal Haber { get; set; }
        // pólizas que no cuadran
        public int Descuadradas { get; set; }
        // pólizas anuladas
        public int Anuladas { get; set; }
    }
}
