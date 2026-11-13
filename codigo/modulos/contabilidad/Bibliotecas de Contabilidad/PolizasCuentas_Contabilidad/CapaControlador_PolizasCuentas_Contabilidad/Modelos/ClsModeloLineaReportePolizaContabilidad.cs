/*
 * ==================================================================
 * Área : Contabilidad
 * Autor : Oskar Cermeño
 * Carné : 0901-23-15379
 * Fecha : 11/11/2026
 * ==================================================================
 * Propósito :
 *  Fila del reporte de pólizas tal como la recibe el RDLC: la línea
 *  de detalle con los datos y los totales de su póliza.
 * Reglas especificas:
 *  Los importes son decimal. Cuadra y Diferencia los calcula el
 *  controlador, no el reporte.
 * ===================================================================
*/

using System;

namespace CapaControlador_Contabilidad
{
    // fila del reporte para la vista
    public class ClsModeloLineaReportePolizaContabilidad
    {
        // id de la póliza
        public int IdPoliza { get; set; }
        // código de la póliza
        public string CodigoPoliza { get; set; }
        // fecha de la póliza
        public DateTime Fecha { get; set; }
        // período contable
        public string Periodo { get; set; }
        // estado de la póliza
        public string Estado { get; set; }
        // módulo de origen
        public string Modulo { get; set; }
        // país de la póliza
        public string Pais { get; set; }
        // moneda del país
        public string Moneda { get; set; }
        // documento de origen
        public string DocumentoOrigen { get; set; }
        // descripción de la póliza
        public string Descripcion { get; set; }
        // número de línea
        public int Linea { get; set; }
        // código de la cuenta
        public string CodigoCuenta { get; set; }
        // nombre de la cuenta
        public string NombreCuenta { get; set; }
        // descripción de la línea
        public string DescripcionLinea { get; set; }
        // monto al debe
        public decimal Debe { get; set; }
        // monto al haber
        public decimal Haber { get; set; }
        // total debe de la póliza
        public decimal TotalDebePoliza { get; set; }
        // total haber de la póliza
        public decimal TotalHaberPoliza { get; set; }
        // si la póliza cuadra
        public bool Cuadra { get; set; }
        // debe menos haber
        public decimal Diferencia { get; set; }
    }
}
