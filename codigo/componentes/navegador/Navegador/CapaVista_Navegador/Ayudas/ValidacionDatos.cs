// Inicio - Roger Yankhel de Jesús Herrera Alcántara 0901-23-2429.
// Ayuda de la vista: ejecuta DataAnnotations sobre cualquier instancia
// y presenta todos los errores de validación en una sola ventana.
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Windows.Forms;

namespace CapaVista_Navegador.Ayudas
{
    // La vista recibe el modelo configurado sin conocer las reglas de la tabla.
    public class ValidacionDatos
    {
        private readonly ValidationContext contexto;
        private readonly List<ValidationResult> resultados;
        private readonly bool valido;

        // Evalúa las propiedades anotadas y conserva los resultados.
        public ValidacionDatos(object instancia)
        {
            contexto = new ValidationContext(instancia);
            resultados = new List<ValidationResult>();
            valido = Validator.TryValidateObject(instancia, contexto, resultados, true);
        }

        // Permite continuar si no hay errores; en caso contrario los muestra.
        public bool Validar()
        {
            if (!valido)
            {
                string mensaje = string.Join("\n", resultados.ConvertAll(item => item.ErrorMessage));
                MessageBox.Show(mensaje, "Validación de datos", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            return valido;
        }
    }
}
// Fin - Roger Yankhel de Jesús Herrera Alcántara 0901-23-2429.
