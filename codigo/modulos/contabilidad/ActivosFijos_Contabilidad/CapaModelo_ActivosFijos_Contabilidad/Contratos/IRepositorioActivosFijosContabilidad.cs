/*
 * ==================================================================
 * Área : Contabilidad
 * Autor : Dylan Hernández
 * Carné : 0901-23-519
 * Fecha : 17/10/2026
 * ==================================================================
 * Propósito :
 *  Contrato del repositorio de activos fijos: el mantenimiento
 *  genérico más módulos y maestro de vida útil.
 * ===================================================================
*/

using System.Collections.Generic;
using CapaModelo_ActivosFijos_Contabilidad.Entidades;
using CapaModelo_ActivosFijos_Contabilidad.Repositorios;
using CapaModelo_Contabilidad.Contratos;
using CapaModelo_Contabilidad.Repositorios;

namespace CapaModelo_ActivosFijos_Contabilidad.Contratos
{
    public interface IRepositorioActivosFijosContabilidad : IRepositorioGenericoContabilidad<ClsActivoFijoContabilidad>
    {
        string[] ContabilidadFuncObtenerModulos();

        List<ClsVidaUtilContabilidad> ContabilidadFuncObtenerVidaUtil();
    }
}
