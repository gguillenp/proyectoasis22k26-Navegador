using System;
using System.Data;
using System.Windows.Forms;
using CapaControlador_Consultas;

namespace CapaVista_Consultas.Controles
{
    // Inicio del código de Carlos Andres Arriaza Lara 0901-23-13862 el 16/09/2026
    public partial class ClsSeleccioneUnaConsulta : Componentes.ClsControlUsuarioConsultas
    {
        public event Action<string, string> ConsultaSeleccionada;
        public string Tabla { get; set; }
        public string Query { get; set; }

        private readonly ClsConsultaSeleccionada _Consultas = new ClsConsultaSeleccionada();

        public ClsSeleccioneUnaConsulta()
        {
            InitializeComponent();
            ConsultasProcActualizarConsultas();
        }

        private void ConsultasProcActualizarConsultas()
        {
            try
            {
                ConsultasDgvConsultasReutilizables.Columns.Clear();

                DataTable Consultas = _Consultas.ConsultasFuncCargarConsultas();

                ConsultasDgvConsultasReutilizables.DataSource = Consultas;

                if (ConsultasDgvConsultasReutilizables.Columns["Query"] != null)
                {
                    ConsultasDgvConsultasReutilizables.Columns["Query"].Visible = false;
                }

                if (ConsultasDgvConsultasReutilizables.Columns["Tabla"] != null)
                {
                    ConsultasDgvConsultasReutilizables.Columns["Tabla"].Visible = false;
                }
            }
            catch (InvalidOperationException Excepcion)
            {
                ConsultasDgvConsultasReutilizables.DataSource = null;

                MessageBox.Show(
                    Excepcion.Message,
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
            catch (Exception Excepcion)
            {
                ConsultasDgvConsultasReutilizables.DataSource = null;

                MessageBox.Show(
                    "Ocurrió un error inesperado al cargar " +
                    "las consultas.\n\n" +
                    Excepcion.Message,
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        public void ConsultasProcRefrescarConsultas()
        {
            if (string.IsNullOrWhiteSpace(Tabla))
            {
                ConsultasProcActualizarConsultas();
                return;
            }

            ConsultasProcActualizarConsultasPorTabla(Tabla);
        }

        private void ConsultasMetBtnIngresarClick(object Sender, EventArgs Evento)
        {
            FrmMantenimientoConsultas FormularioMantenimientoConsultas = new FrmMantenimientoConsultas(Tabla);
            FormularioMantenimientoConsultas.ShowDialog();
            ConsultasProcRefrescarConsultas();
        }

        private void ConsultasMetBtnConsultarClick(object Sender, EventArgs Evento)
        {
            if (ConsultasDgvConsultasReutilizables.CurrentRow == null)
            {
                MessageBox.Show(
                    "Seleccione una fila para ejecutar la consulta.",
                    "Consulta",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);

                return;
            }

            Query =
                ConsultasDgvConsultasReutilizables
                    .CurrentRow
                    .Cells["Query"]
                    .Value?
                    .ToString();

            Tabla =
                ConsultasDgvConsultasReutilizables
                    .CurrentRow
                    .Cells["Tabla"]
                    .Value?
                    .ToString();

            if (string.IsNullOrWhiteSpace(Query))
            {
                MessageBox.Show(
                    "La consulta seleccionada no contiene " +
                    "una sentencia válida.",
                    "Consulta",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);

                return;
            }

            ConsultaSeleccionada?.Invoke(Query, Tabla);
        }

        public void ConsultasProcActualizarConsultasPorTabla(string NombreTabla)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(NombreTabla))
                {
                    throw new ArgumentException("El nombre de la tabla no puede estar vacío.");
                }

                Tabla = NombreTabla;

                ConsultasDgvConsultasReutilizables.Columns.Clear();

                DataTable Consultas = _Consultas.ConsultasFuncCargarConsultasPorTabla(NombreTabla);

                ConsultasDgvConsultasReutilizables.DataSource = Consultas;

                if (ConsultasDgvConsultasReutilizables.Columns["Id"] != null)
                {
                    ConsultasDgvConsultasReutilizables.Columns["Id"].Visible = false;
                }

                if (ConsultasDgvConsultasReutilizables.Columns["Query"] != null)
                {
                    ConsultasDgvConsultasReutilizables.Columns["Query"].Visible = false;
                }

                if (ConsultasDgvConsultasReutilizables.Columns["Tabla"] != null)
                {
                    ConsultasDgvConsultasReutilizables.Columns["Tabla"].Visible = false;
                }

            }
            catch (ArgumentException Excepcion)
            {
                ConsultasDgvConsultasReutilizables.DataSource = null;

                MessageBox.Show(
                    Excepcion.Message,
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
            catch (InvalidOperationException Excepcion)
            {
                ConsultasDgvConsultasReutilizables
                    .DataSource = null;

                MessageBox.Show(
                    Excepcion.Message,
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
            catch (Exception Excepcion)
            {
                ConsultasDgvConsultasReutilizables
                    .DataSource = null;

                MessageBox.Show(
                    "Ocurrió un error inesperado al cargar " +
                    "las consultas.\n\n" +
                    Excepcion.Message,
                    "Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        // Fin del código de Carlos Andres Arriaza Lara 0901-23-13862 el 16/09/2026
    }
}