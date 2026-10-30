/*
 * ==================================================================
 * Área : Contabilidad
 * Autor : Gabriel André Guillén Pocón
 * Carné : 0901-23-1998
 * Fecha : 30/10/2026
 * ==================================================================
 * Propósito :
 *  Impuesto que Contabilidad entrega a los demás módulos: un tramo con
 *  su tasa en porcentaje y las cuentas donde se registra.
 * ===================================================================
*/

using System;

namespace CapaControlador_Contabilidad
{
    public class ClsModeloImpuestoContabilidad
    {
        public int IdPais { get; set; }
        public string Pais { get; set; }
        public string Codigo { get; set; }
        public string Nombre { get; set; }
        public string Tipo { get; set; }
        public string Operacion { get; set; }
        public decimal Tasa { get; set; }
        public decimal MontoDesde { get; set; }
        public decimal? MontoHasta { get; set; }
        public decimal ImporteFijo { get; set; }
        public bool SobreExcedente { get; set; }
        public string CuentaAcreditable { get; set; }
        public string CuentaPorPagar { get; set; }
        public string BaseLegal { get; set; }
        public DateTime VigenteDesde { get; set; }
        public DateTime? VigenteHasta { get; set; }
    }
}
