/*
 * ==================================================================
 * Área : Contabilidad
 * Autor : Dylan Hernández
 * Carné : 0901-23-519
 * Fecha : 17/10/2026
 * ==================================================================
 * Propósito :
 *  Reglas de activos fijos: catálogos, depreciación por línea recta,
 *  validación y grabación según el estado. Responsable de la
 *  pantalla: Dylan.
 * Reglas especificas:
 *  La validación vive en ClsModeloActivoFijoContabilidad. La
 *  depreciación mensual se redondea a dos decimales.
 * ===================================================================
*/

using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using CapaControlador_Contabilidad;
using CapaModelo_ActivosFijos_Contabilidad.Contratos;
using CapaModelo_ActivosFijos_Contabilidad.Entidades;
using CapaModelo_ActivosFijos_Contabilidad.Repositorios;
using CapaModelo_Contabilidad.Contratos;
using CapaModelo_Contabilidad.Repositorios;

namespace CapaControlador_ActivosFijos_Contabilidad
{
    public class ClsControladorActivosFijosContabilidad
    {
        private readonly IRepositorioActivosFijosContabilidad _Repositorio = new ClsRepositorioActivosFijosContabilidad();

        public string[] ContabilidadFuncModulos()
        {
            return _Repositorio.ContabilidadFuncObtenerModulos();
        }

        public DataTable ContabilidadFuncVidaUtil()
        {
            DataTable Tabla = new DataTable();
            Tabla.Columns.Add("Tipo", typeof(string));
            Tabla.Columns.Add("Nombre", typeof(string));
            Tabla.Columns.Add("Meses", typeof(int));
            foreach (ClsVidaUtilContabilidad Vida in _Repositorio.ContabilidadFuncObtenerVidaUtil())
                Tabla.Rows.Add(Vida.Tipo, Vida.Nombre, Vida.Meses);
            return Tabla;
        }

        private ClsModeloActivoFijoContabilidad ContabilidadFuncCrearModelo(string Descripcion, string Centro, DateTime Adquisicion,
            DateTime Inicio, decimal Compra, decimal Residual, int Meses)
        {
            return new ClsModeloActivoFijoContabilidad
            {
                Descripcion = Descripcion, Centro = Centro, Adquisicion = Adquisicion, Inicio = Inicio,
                Compra = Compra, Residual = Residual, Meses = Meses
            };
        }

        public Dictionary<string, string> ContabilidadFuncErroresActivo(string Descripcion, string Centro, DateTime Adquisicion,
            DateTime Inicio, decimal Compra, decimal Residual, int Meses)
        {
            return ClsValidacionDatosContabilidad.ContabilidadFuncErrores(
                ContabilidadFuncCrearModelo(Descripcion, Centro, Adquisicion, Inicio, Compra, Residual, Meses));
        }

        public string ContabilidadFuncValidarActivo(string Descripcion, string Centro, DateTime Adquisicion,
            DateTime Inicio, decimal Compra, decimal Residual, int Meses)
        {
            return ContabilidadFuncErroresActivo(Descripcion, Centro, Adquisicion, Inicio, Compra, Residual, Meses).Values.FirstOrDefault() ?? string.Empty;
        }

        public decimal ContabilidadFuncBaseDepreciable(decimal Compra, decimal Residual)
        {
            return Compra - Residual;
        }

        public decimal ContabilidadFuncDepreciacion(decimal Compra, decimal Residual, int Meses)
        {
            if (Compra < 0m || Residual < 0m || Residual > Compra || Meses <= 0) return 0m;
            return decimal.Round((Compra - Residual) / Meses, 2, MidpointRounding.AwayFromZero);
        }

        public bool ContabilidadFuncGrabarCambios(EstadoEntidadContabilidad Estado, string Descripcion, string Centro, string Modulo,
            string Tipo, DateTime Adquisicion, DateTime Inicio, decimal Compra, decimal Residual, int Meses, out string Mensaje)
        {
            ClsModeloActivoFijoContabilidad Modelo = ContabilidadFuncCrearModelo(Descripcion, Centro, Adquisicion, Inicio, Compra, Residual, Meses);
            Modelo.Modulo = Modulo;
            Modelo.Tipo = Tipo;
            Mensaje = ClsValidacionDatosContabilidad.ContabilidadFuncPrimerError(Modelo);
            if (Mensaje != string.Empty) return false;

            ClsActivoFijoContabilidad Entidad = new ClsActivoFijoContabilidad
            {
                IdActivo = Modelo.IdActivo, Descripcion = Modelo.Descripcion.Trim(), Centro = Modelo.Centro.Trim(), Modulo = Modelo.Modulo,
                Tipo = Modelo.Tipo, Adquisicion = Modelo.Adquisicion, Inicio = Modelo.Inicio, Compra = Modelo.Compra,
                Residual = Modelo.Residual, Meses = Modelo.Meses
            };
            try
            {
                switch (Estado)
                {
                    case EstadoEntidadContabilidad.Agregar:
                        _Repositorio.ContabilidadFuncAgregar(Entidad);
                        Mensaje = "Activo validado correctamente.";
                        break;
                    case EstadoEntidadContabilidad.Modificar:
                        _Repositorio.ContabilidadFuncEditar(Entidad);
                        Mensaje = "Activo actualizado correctamente.";
                        break;
                    case EstadoEntidadContabilidad.Borrar:
                        _Repositorio.ContabilidadFuncEliminar(Entidad);
                        Mensaje = "Activo eliminado correctamente.";
                        break;
                }
                return true;
            }
            catch (Exception Excepcion)
            {
                Mensaje = Excepcion.Message;
                return false;
            }
        }
    }
}
