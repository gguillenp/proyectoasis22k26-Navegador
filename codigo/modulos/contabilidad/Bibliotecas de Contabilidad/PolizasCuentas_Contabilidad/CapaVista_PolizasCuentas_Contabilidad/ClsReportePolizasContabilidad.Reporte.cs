/*
 * ==================================================================
 * Área : Contabilidad
 * Autor : Oskar Cermeño
 * Carné : 0901-23-15379
 * Fecha : 11/11/2026
 * ==================================================================
 * Propósito :
 *  Entrega al MDI la ventana del reporte de pólizas cuando se pulsa
 *  Reporte en el botón Pólizas locales.
 * Reglas especificas:
 *  Implementa el método parcial que declara ClsReportePolizasContabilidad.cs;
 *  ese archivo no se modifica.
 * ===================================================================
*/

using System.Windows.Forms;
using CapaControlador_Contabilidad;

namespace CapaVista_Contabilidad
{
    // conexión del reporte con el MDI
    public static partial class ClsReportePolizasContabilidad
    {
        // crea la ventana del reporte
        static partial void ContabilidadMetCrearReporte(ClsModeloPermisosPantallaContabilidad Permisos, ref Form Formulario)
        {
            Formulario = new FrmReportePolizas(Permisos);
        }
    }
}
