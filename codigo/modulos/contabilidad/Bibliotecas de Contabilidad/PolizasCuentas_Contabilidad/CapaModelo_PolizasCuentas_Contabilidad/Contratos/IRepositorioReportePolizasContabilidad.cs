/*
 * ==================================================================
 * Área : Contabilidad
 * Autor : Oskar Cermeño
 * Carné : 0901-23-15379
 * Fecha : 11/11/2026
 * ==================================================================
 * Propósito :
 *  Contrato del repositorio del reporte de pólizas: las líneas que
 *  cumplen los filtros y los catálogos de módulos y países.
 * Reglas especificas:
 *  Estado vacío, módulo 0 y país 0 significan "todos".
 * ===================================================================
*/

using System;
using System.Collections.Generic;
using CapaModelo_Contabilidad.Entidades;

namespace CapaModelo_Contabilidad.Contratos
{
    // contrato del reporte
    public interface IRepositorioReportePolizasContabilidad
    {
        // líneas filtradas
        List<ClsLineaReportePolizaContabilidad> ContabilidadFuncObtenerLineas(DateTime Desde, DateTime Hasta, string Estado, int IdModulo, int IdPais);

        // módulos contables
        Dictionary<int, string> ContabilidadFuncObtenerModulos();

        // países
        Dictionary<int, string> ContabilidadFuncObtenerPaises();
    }
}
