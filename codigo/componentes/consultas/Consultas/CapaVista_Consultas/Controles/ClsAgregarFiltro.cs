using CapaControlador_Consultas;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Windows.Forms;

namespace CapaVista_Consultas.Controles
{
    // Inicio de código de "José Pablo Cano Cóbar" - carné: "0901-23-1727" - Fecha: "15/09/26"
    public partial class ClsAgregarFiltro : Componentes.ClsControlUsuarioConsultas
    {
        private readonly ClsControladorFiltroSimple _Controlador =
            new ClsControladorFiltroSimple();

        private string _TablaActual;

        public event EventHandler<ClsArgumentosFiltro> ConsultasEvtBuscarSolicitado;

        public event EventHandler ConsultasEvtRefrescarSolicitado;

        public ClsAgregarFiltro()
        {
            InitializeComponent();

            ConsultasBtnBuscar.Click += ConsultasMetBtnBuscarClick;
            ConsultasBtnRefrescar.Click += ConsultasMetBtnRefrescarClick;

            ConsultasTxtValor.KeyDown += ConsultasMetTxtValorKeyDown;
            ConsultasCboCampo.SelectedIndexChanged += ConsultasMetCboCampoSelectedIndexChanged;
        }
        public void ConsultasProcActualizarTabla(string Tabla)
        {
            if (string.IsNullOrWhiteSpace(Tabla))
            {
                ConsultasCboCampo.Items.Clear();
                _TablaActual = null;
                return;
            }

            _TablaActual = Tabla;

            ConsultasMetCargarCampos();
        }

        //Inicio del código de Miguel David Contreras Jacinto 0901-21-3878 el 21/09/2026
        private void ConsultasMetCboCampoSelectedIndexChanged(object Sender, EventArgs Evento)
        {
            ConsultasMetCargarOperadoresPorTipo();
        }

        //Fin del código de Miguel David Contreras Jacinto 0901-21-3878 el 21/09/2026
        public void ConsultasProcLimpiar()
        {
            ConsultasCboCampo.SelectedIndex = -1;
            ConsultasCboOperador.SelectedIndex = -1;
            ConsultasTxtValor.Clear();
        }

        private void ConsultasMetCargarCampos()
        {
            ConsultasCboCampo.Items.Clear();

            try
            {
                List<string> Campos =
                    _Controlador.ConsultasFuncObtenerCampos(_TablaActual);

                foreach (string Campo in Campos)
                {
                    ConsultasCboCampo.Items.Add(Campo);
                }
            }
            catch (Exception Excepcion)
            {
                MessageBox.Show(
                    "No se pudieron cargar los campos de la tabla '" +
                    _TablaActual + "'.\n\nDetalle: " + Excepcion.Message,
                    "Consultas",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
        }
        private void ConsultasMetBtnBuscarClick(object Sender, EventArgs e)
        {
            string Campo = ConsultasCboCampo.SelectedItem == null
                ? string.Empty
                : ConsultasCboCampo.SelectedItem.ToString();

            string Operador = ConsultasCboOperador.SelectedItem == null
                ? string.Empty
                : ConsultasCboOperador.SelectedItem.ToString();

            string Valor = ConsultasTxtValor.Text;

            string Mensaje = _Controlador.ConsultasFuncValidarFiltro(
                Campo,
                Operador,
                Valor);

            if (Mensaje != null)
            {
                MessageBox.Show(
                    Mensaje,
                    "Consultas",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);

                return;
            }

            if (ConsultasEvtBuscarSolicitado != null)
            {
                ConsultasEvtBuscarSolicitado(
                    this,
                    new ClsArgumentosFiltro(Campo, Operador, Valor));
            }
        }

        //Inicio del código de Miguel David Contreras Jacinto 0901-21-3878 el 21/09/2026
        private void ConsultasMetCargarOperadoresPorTipo()
        {
            // Guardar el operador seleccionado actualmente
            string OperadorSeleccionado =
                ConsultasCboOperador.SelectedItem == null
                ? string.Empty
                : ConsultasCboOperador.SelectedItem.ToString();

            ConsultasCboOperador.Items.Clear();

            if (ConsultasCboCampo.SelectedItem == null)
                return;

            if (string.IsNullOrWhiteSpace(_TablaActual))
                return;

            string Campo = ConsultasCboCampo.SelectedItem.ToString();

            Type TipoCampo = _Controlador.ConsultasFuncObtenerTipoCampo(
                _TablaActual,
                Campo);

            if (TipoCampo == null)
                return;

            // Campos numéricos y fechas
            if (TipoCampo == typeof(decimal) ||
                TipoCampo == typeof(int) ||
                TipoCampo == typeof(long) ||
                TipoCampo == typeof(double) ||
                TipoCampo == typeof(float) ||
                TipoCampo == typeof(DateTime))
            {
                ConsultasCboOperador.Items.Add("=");
                ConsultasCboOperador.Items.Add("<>");
                ConsultasCboOperador.Items.Add(">");
                ConsultasCboOperador.Items.Add("<");
                ConsultasCboOperador.Items.Add(">=");
                ConsultasCboOperador.Items.Add("<=");
            }
            // Booleanos
            else if (TipoCampo == typeof(bool))
            {
                ConsultasCboOperador.Items.Add("=");
                ConsultasCboOperador.Items.Add("<>");
            }
            // Texto
            else
            {
                ConsultasCboOperador.Items.Add("=");
                ConsultasCboOperador.Items.Add("<>");
                ConsultasCboOperador.Items.Add("Contiene");
                ConsultasCboOperador.Items.Add("Comienza con");
                ConsultasCboOperador.Items.Add("Termina con");
            }

            // Volver a seleccionar el operador anterior si todavía existe
            if (!string.IsNullOrWhiteSpace(OperadorSeleccionado) &&
                ConsultasCboOperador.Items.Contains(OperadorSeleccionado))
            {
                ConsultasCboOperador.SelectedItem = OperadorSeleccionado;
            }
            else
            {
                ConsultasCboOperador.SelectedIndex = -1;
            }
        }

        //Fin del código de Miguel David Contreras Jacinto 0901-21-3878 el 21/09/2026

        private void ConsultasMetBtnRefrescarClick(object Sender, EventArgs e)
        {
            ConsultasProcLimpiar();

            if (ConsultasEvtRefrescarSolicitado != null)
            {
                ConsultasEvtRefrescarSolicitado(this, EventArgs.Empty);
            }
        }

        private void ConsultasMetTxtValorKeyDown(object Sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                ConsultasMetBtnBuscarClick(Sender, EventArgs.Empty);
            }
        }

        //Inicio de código de Diego Fernando Santizo Samayoa 0901-22-15950 22/09/2026
        private void ConsultasMetBtnAyudaClick(object sender, EventArgs e)
        {
            DirectoryInfo Directorio = new DirectoryInfo(Application.StartupPath);

            while (Directorio != null)
            {
                string Ruta = Path.Combine(Directorio.FullName,"ayuda","componentes","consultas","Ayuda_Consultas.chm");

                if (File.Exists(Ruta))
                {
                    Help.ShowHelp(this,Ruta,"ConsultaSimple.html");
                    return;
                }
                Directorio = Directorio.Parent;
            }

            MessageBox.Show("No se encontró el archivo de ayuda.");
        }
        //Fin de código de Diego Fernando Santizo Samayoa 0901-22-15950 22/09/2026

        public class ClsArgumentosFiltro : EventArgs
        {
            public string Campo { get; private set; }
            public string Operador { get; private set; }
            public string Valor { get; private set; }

            public ClsArgumentosFiltro(string Campo, string Operador, string Valor)
            {
                this.Campo = Campo;
                this.Operador = Operador;
                this.Valor = Valor;
            }
        }
        // Fin del código de "José Pablo Cano Cóbar" - Carné: "0901-23-1727" - Fecha: "15/09/26"
    }
}