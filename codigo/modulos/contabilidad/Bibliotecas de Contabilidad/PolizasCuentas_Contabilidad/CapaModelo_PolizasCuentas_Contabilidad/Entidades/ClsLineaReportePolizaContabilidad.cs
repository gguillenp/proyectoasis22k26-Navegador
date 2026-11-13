/*
 * ==================================================================
 * Área : Contabilidad
 * Autor : Oskar Cermeño
 * Carné : 0901-23-15379
 * Fecha : 11/11/2026
 * ==================================================================
 * Propósito :
 *  Una fila del reporte de pólizas: datos del encabezado de la póliza
 *  junto con una de sus líneas de detalle.
 * Reglas especificas:
 *  Una póliza sin líneas activas llega con Linea en 0 y sin cuenta.
 * ===================================================================
*/

using System;

namespace CapaModelo_Contabilidad.Entidades
{
    // fila del reporte
    public class ClsLineaReportePolizaContabilidad
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
    }
}
