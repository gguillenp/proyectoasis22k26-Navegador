// Inicio - Roger Yankhel de Jesús Herrera Alcántara 0901-23-2429.
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using CapaModelo_Navegador;

namespace CapaControlador_Navegador
{
    public class ModeloCampo
    {
        public ClsColumnaInfo Columna { get; set; }

        [ValidacionColumna]
        public string Valor { get; set; }
    }

    // Las columnas cambian según la tabla: no se fijan propiedades de empleados.
    public class ModeloRegistro : IValidatableObject
    {
        private readonly Dictionary<string, string> datos;
        private readonly List<ClsColumnaInfo> columnas;

        public ModeloRegistro(Dictionary<string, string> datos, List<ClsColumnaInfo> columnas)
        {
            this.datos = datos;
            this.columnas = columnas;
        }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (columnas == null || columnas.Count == 0)
            {
                yield return new ValidationResult("No se encontró el esquema de la tabla.");
                yield break;
            }
            if (datos == null || datos.Count == 0)
            {
                yield return new ValidationResult("La colección de datos está vacía.");
                yield break;
            }
            // Validar solo los campos enviados permite actualizar parcialmente y eliminar por PK.
            foreach (var dato in datos)
            {
                var columna = columnas.Find(item =>
                    string.Equals(item.Nombre, dato.Key, StringComparison.OrdinalIgnoreCase));
                if (columna == null)
                {
                    yield return new ValidationResult("El atributo '" + dato.Key + "' no existe en la tabla.", new[] { dato.Key });
                    continue;
                }
                var campo = new ModeloCampo { Columna = columna, Valor = dato.Value };
                var resultados = new List<ValidationResult>();
                // TryValidateObject no recorre objetos hijos automáticamente.
                Validator.TryValidateObject(campo, new ValidationContext(campo), resultados, true);
                foreach (var resultado in resultados)
                    yield return resultado;
            }
        }
    }
}
// Fin - Roger Yankhel de Jesús Herrera Alcántara 0901-23-2429.
