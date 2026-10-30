/*
 * ==================================================================
 * Área : Contabilidad
 * Autor : Gabriel André Guillén Pocón
 * Carné : 0901-23-1998
 * Fecha : 30/10/2026
 * ==================================================================
 * Propósito :
 *  Consulta de impuestos para los demás módulos: la lista vigente de
 *  un país, la tasa de un impuesto y el cálculo sobre un monto.
 * Reglas especificas:
 *  La operación COMPRA o VENTA también trae los impuestos de AMBAS. El
 *  cálculo toma el tramo del monto: importe fijo más la tasa sobre el
 *  monto completo o solo sobre el excedente del tramo, según el tramo.
 *  Se redondea a dos decimales.
 * ===================================================================
*/

using System;
using System.Collections.Generic;
using System.Linq;
using CapaModelo_Contabilidad.Entidades;
using CapaModelo_Contabilidad.Repositorios;

namespace CapaControlador_Contabilidad
{
    public class ClsControladorImpuestosContabilidad
    {
        private readonly ClsRepositorioImpuestosContabilidad _Repositorio = new ClsRepositorioImpuestosContabilidad();

        public List<ClsModeloImpuestoContabilidad> ContabilidadFuncImpuestos(int IdPais)
        {
            return ContabilidadFuncVigentes(IdPais, DateTime.Today).Select(ContabilidadFuncModelo).ToList();
        }

        public List<ClsModeloImpuestoContabilidad> ContabilidadFuncImpuestos(int IdPais, string Operacion)
        {
            string Buscada = (Operacion ?? string.Empty).Trim().ToUpperInvariant();
            return ContabilidadFuncVigentes(IdPais, DateTime.Today)
                .Where(Impuesto => Impuesto.Operacion == Buscada || (Impuesto.Operacion == "AMBAS" && (Buscada == "COMPRA" || Buscada == "VENTA")))
                .Select(ContabilidadFuncModelo).ToList();
        }

        public decimal ContabilidadFuncTasa(int IdPais, string Codigo)
        {
            return ContabilidadFuncTramos(IdPais, Codigo, DateTime.Today).First().Tasa;
        }

        public decimal ContabilidadFuncCalcular(int IdPais, string Codigo, decimal Monto)
        {
            return ContabilidadFuncCalcular(IdPais, Codigo, Monto, DateTime.Today);
        }

        public decimal ContabilidadFuncCalcular(int IdPais, string Codigo, decimal Monto, DateTime Fecha)
        {
            List<ClsImpuestoContabilidad> Tramos = ContabilidadFuncTramos(IdPais, Codigo, Fecha);
            if (Monto <= 0m) return 0m;
            ClsImpuestoContabilidad Tramo = Tramos.Last(Fila => Fila.MontoDesde == 0m || Monto > Fila.MontoDesde);
            decimal Base = Tramo.SobreExcedente ? Monto - Tramo.MontoDesde : Monto;
            return Math.Round(Tramo.ImporteFijo + Base * Tramo.Tasa / 100m, 2, MidpointRounding.AwayFromZero);
        }

        private List<ClsImpuestoContabilidad> ContabilidadFuncVigentes(int IdPais, DateTime Fecha)
        {
            if (IdPais < 0) throw new ArgumentException("El país no es válido.");
            return _Repositorio.ContabilidadFuncObtenerVigentes(IdPais, Fecha);
        }

        private List<ClsImpuestoContabilidad> ContabilidadFuncTramos(int IdPais, string Codigo, DateTime Fecha)
        {
            if (IdPais <= 0) throw new ArgumentException("Indique el país del impuesto.");
            string Buscado = (Codigo ?? string.Empty).Trim().ToUpperInvariant();
            List<ClsImpuestoContabilidad> Tramos = ContabilidadFuncVigentes(IdPais, Fecha)
                .Where(Impuesto => Impuesto.Codigo == Buscado).OrderBy(Impuesto => Impuesto.MontoDesde).ToList();
            if (Tramos.Count == 0)
                throw new InvalidOperationException("No hay un impuesto " + Buscado + " vigente al " + Fecha.ToString("dd/MM/yyyy") + " para el país " + IdPais + ".");
            return Tramos;
        }

        private static ClsModeloImpuestoContabilidad ContabilidadFuncModelo(ClsImpuestoContabilidad Impuesto)
        {
            return new ClsModeloImpuestoContabilidad
            {
                IdPais = Impuesto.IdPais, Pais = Impuesto.Pais, Codigo = Impuesto.Codigo, Nombre = Impuesto.Nombre, Tipo = Impuesto.Tipo,
                Operacion = Impuesto.Operacion, Tasa = Impuesto.Tasa, MontoDesde = Impuesto.MontoDesde, MontoHasta = Impuesto.MontoHasta,
                ImporteFijo = Impuesto.ImporteFijo, SobreExcedente = Impuesto.SobreExcedente, CuentaAcreditable = Impuesto.CuentaAcreditable,
                CuentaPorPagar = Impuesto.CuentaPorPagar, BaseLegal = Impuesto.BaseLegal, VigenteDesde = Impuesto.VigenteDesde, VigenteHasta = Impuesto.VigenteHasta
            };
        }
    }
}
