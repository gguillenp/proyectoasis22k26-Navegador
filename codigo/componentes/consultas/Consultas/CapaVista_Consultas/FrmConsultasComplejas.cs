using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;

namespace CapaVista_Consultas
{
    // Inicio de código de "Diego Fernando Santizo Samayoa" - carné: "0901-22-15950" - Fecha: "19/09/26"
    public partial class FrmConsultasComplejas :
        Componentes.ClsBaseTerminus
    {
        public string TablaActual { get; private set; }
        List<Form> _formularioCerrar = new List<Form>();
        public string CampoSeleccionado { get; private set; }
        public bool SeleccionRealizada { get; private set; }

        public FrmConsultasComplejas()
        {
            InitializeComponent();
            ConsultasMetConfigurarFormulario();
        }

        public FrmConsultasComplejas(string Tabla, string CampoId): this()
        {
            TablaActual = Tabla;
            ConsultasUcConsultasReutilizables.Tabla = TablaActual;
            ConsultasUcConsultasReutilizables.ConsultasProcRefrescarConsultas();

            ConsultasUcTabla.ConsultasMetConfigurarSeleccion(CampoId);

            ConsultasUcTabla.ConsultasProcCambiarRegistrosPorPagina(30,TablaActual);
        }

        private void ConsultasMetConfigurarFormulario()
        {
            ConsultasUcTabla.ConsultasMetAjustarAlturaFilas(30);
            ConsultasUcConsultasReutilizables.ConsultaSeleccionada += ConsultasMetEjecutarConsultaSeleccionada;

            ConsultasUcTabla.ConsultasEvtFilaSeleccionada += ConsultasMetUcTablaFilaSeleccionada;
        }

        private void ConsultasMetUcTablaFilaSeleccionada(object Sender, EventArgs Evento)
        {
            CampoSeleccionado =
                ConsultasUcTabla.CampoSeleccionado;

            SeleccionRealizada =
                ConsultasUcTabla.SeleccionRealizada;

            if (!SeleccionRealizada)
            {
                return;
            }

            DialogResult =
                DialogResult.OK;

            Close();
        }

        // Fin de código de "Diego Fernando Santizo Samayoa" - carné: "0901-22-15950" - Fecha: "19/09/26"

        // Inicio de código de "José Pablo Cano Cóbar" - carné: "0901-23-1727" - Fecha: "16/09/26"

        private void ConsultasMetEjecutarConsultaSeleccionada(string Query, string Tabla)
        {
            TablaActual = Tabla;

            ConsultasUcTabla.ConsultasProcCargarConsultaDesdeQuery(Query,Tabla);
        }

        private void ConsultasMetBtnSeleccionarClick(object Sender,EventArgs Evento)
        {
            bool ResultadoSeleccion = ConsultasUcTabla.ConsultasFuncSeleccionarRegistro();

            if (!ResultadoSeleccion)
            {
                MessageBox.Show(
                    "Seleccione un registro.",
                    "Consultas",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);

                return;
            }
            CampoSeleccionado = ConsultasUcTabla.CampoSeleccionado;
            SeleccionRealizada = ConsultasUcTabla.SeleccionRealizada;
            DialogResult = DialogResult.OK;
            Close();
        }

        private void ConsultasMetBtnSalirClick(object Sender, EventArgs Evento)
        {
            DialogResult Respuesta =
                MessageBox.Show(
                    "¿Desea salir del componente de Consultas?",
                    "Consultas",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

            if (Respuesta == DialogResult.Yes)
            {

                foreach (Form frm in Application.OpenForms)
                {
                    if (frm.Name == "FrmConsultasSimples")
                    {
                        _formularioCerrar.Add(frm);
                    }
                }
                foreach (Form frm in _formularioCerrar) {
                    frm.Close();
                }
                this.Dispose();
            }
        }

        private void ConsultasMetBtnInicioClick(object Sender, EventArgs Evento)
        {
            Close();
        }

        private void ConsultasMetBtnRefrescarClick(object Sender, EventArgs Evento)
        {
            ConsultasUcTabla.ConsultasProcActualizarTabla(TablaActual);
            ConsultasUcConsultasReutilizables.ConsultasProcRefrescarConsultas();
        }
        // Fin de código de "José Pablo Cano Cóbar" - carné: "0901-23-1727" - Fecha: "16/09/26"

        //Inicio de código de Diego Fernando Santizo Samayoa 0901-22-15950 22/09/2026
        private void ConsultasMetBtnAyudaClick(object Sender, EventArgs Evento)
        {
            DirectoryInfo Directorio = new DirectoryInfo(Application.StartupPath);

            while (Directorio != null)
            {
                string Ruta = Path.Combine(Directorio.FullName, "ayuda", "componentes", "consultas", "Ayuda_Consultas.chm");

                if (File.Exists(Ruta))
                {
                    Help.ShowHelp(this, Ruta, "ConsultaCompleja.html");
                    return;
                }
                Directorio = Directorio.Parent;
            }

            MessageBox.Show("No se encontró el archivo de ayuda.");
        }

        //Fin de código de Diego Fernando Santizo Samayoa 0901-22-15950 22/09/2026
    }
}