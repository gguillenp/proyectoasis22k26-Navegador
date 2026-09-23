using System;
using System.Data;
using System.Windows.Forms;
using CapaControlador_Consultas;
using CapaVista_Consultas.Controles;

namespace CapaVista_Consultas
{
    
    public partial class FrmConsultasSimples :
        Componentes.ClsBaseTerminus
    {
        private readonly ClsControladorFiltroSimple _Controlador = new ClsControladorFiltroSimple();

        // Inicio de código de "Diego Fernando Santizo Samayoa" - carné: "0901-22-15950" - Fecha: "20/09/26"
        private string _TablaActual;
        private string _CampoId;
        private string _CampoFiltro;
        private string _OperadorFiltro;
        private string _ValorFiltro;

        private const int _RegistrosPorPagina = 15;
        public string CampoSeleccionado { get; private set; }
        public bool SeleccionRealizada { get; private set; }
        public FrmConsultasSimples()
        {
            InitializeComponent();

            ConsultasMetSuscribirEventos();
        }
        public FrmConsultasSimples(string Tabla, string CampoId): this()
        {
            _TablaActual = Tabla;
            _CampoId = CampoId;
            ConsultasUcTablaSimple.ConsultasMetConfigurarSeleccion(CampoId);
            ClsTablaSeleccionada.ConsultasMetGuardarTabla(Tabla);
            ConsultasUcTablaSimple.ConsultasProcActualizarTabla(Tabla);
            ConsultasUcAgregarFiltro.ConsultasProcActualizarTabla(Tabla);
        }

        // Fin de código de "Diego Fernando Santizo Samayoa" - carné: "0901-22-15950" - Fecha: "20/09/26"

        // Inicio de código de "Pedro José Gómez Villalobos" - carné: "0901-23-4868" - Fecha: "20/09/26"
        public FrmConsultasSimples(string[] Tablas, string CampoId) : this()
        {
            if (Tablas == null || Tablas.Length == 0)
            {
                throw new ArgumentException(
                    "Debe proporcionar al menos una tabla.",
                    nameof(Tablas));
            }
            _TablaActual = Tablas[0];
            _CampoId = CampoId;
            ConsultasUcTablaSimple.ConsultasMetConfigurarSeleccion(CampoId);
            ClsTablaSeleccionada.ConsultasMetGuardarTablas(Tablas);
            ConsultasUcTablaSimple.ConsultasProcActualizarTabla(_TablaActual);
            ConsultasUcAgregarFiltro.ConsultasProcActualizarTabla(_TablaActual);
        }
        // Fin de código de "Pedro José Gómez Villalobos" - carné: "0901-23-4868" - Fecha: "20/09/26"


        // Inicio de código de "José Pablo Cano Cóbar" - carné: "0901-23-1727" - Fecha: "15/09/26"
        private void ConsultasMetSuscribirEventos()
        {
            ConsultasUcAgregarFiltro.ConsultasEvtBuscarSolicitado += ConsultasMetAgregarFiltroBuscarSolicitado;
            ConsultasUcAgregarFiltro.ConsultasEvtRefrescarSolicitado += ConsultasMetAgregarFiltroRefrescarSolicitado;
            ConsultasUcTablaSimple.ConsultasEvtFilaSeleccionada += ConsultasMetTablaSimpleFilaSeleccionada;
        }

        private void ConsultasMetTablaSimpleFilaSeleccionada(object Sender, EventArgs Evento)
        {
            CampoSeleccionado = ConsultasUcTablaSimple.CampoSeleccionado;

            SeleccionRealizada = ConsultasUcTablaSimple.SeleccionRealizada;

            if (!SeleccionRealizada)
            {
                return;
            }
            DialogResult = DialogResult.OK;
            Close();
        }

        private void ConsultasMetAgregarFiltroBuscarSolicitado(object Sender,ClsAgregarFiltro.ClsArgumentosFiltro ArgumentosFiltro)
        {
            _CampoFiltro = ArgumentosFiltro.Campo;
            _OperadorFiltro = ArgumentosFiltro.Operador;
            _ValorFiltro = ArgumentosFiltro.Valor;

            ConsultasMetAplicarFiltro();
        }

        private void ConsultasMetAgregarFiltroRefrescarSolicitado(object Sender,EventArgs Evento)
        {
            _CampoFiltro = null;
            _OperadorFiltro = null;
            _ValorFiltro = null;

            ConsultasUcTablaSimple.ConsultasProcActualizarTabla(_TablaActual);
        }

        private void ConsultasMetAplicarFiltro()
        {
            try
            {
                Cursor = Cursors.WaitCursor;
                DataTable Resultado = _Controlador.ConsultasFuncBuscar(_TablaActual, _CampoFiltro, _OperadorFiltro, _ValorFiltro, 1, _RegistrosPorPagina);
                int TotalRegistros = _Controlador.ConsultasFuncContar(_TablaActual,_CampoFiltro,_OperadorFiltro,_ValorFiltro);
                ConsultasUcTablaSimple.ConsultasProcMostrarResultado(Resultado,TotalRegistros);

                if (TotalRegistros == 0)
                {
                    MessageBox.Show("Ningún registro cumple con el filtro indicado.",
                        "Consultas",MessageBoxButtons.OK,MessageBoxIcon.Information);
                }
            }
            catch (Exception Excepcion)
            {
                MessageBox.Show("No se pudo ejecutar la consulta.\n\n" + "Detalle: " + Excepcion.Message,"Consultas",MessageBoxButtons.OK,MessageBoxIcon.Error);
            }
            finally
            {
                Cursor = Cursors.Default;
            }
        }

        private void ConsultasMetBtnComplejasClick(object Sender, EventArgs Evento)
        {
            using (FrmConsultasComplejas FormularioConsultasComplejas = new FrmConsultasComplejas(_TablaActual,_CampoId))
            {
                Hide();

                FormularioConsultasComplejas.ShowDialog();

                if (FormularioConsultasComplejas.SeleccionRealizada)
                {
                    CampoSeleccionado = FormularioConsultasComplejas.CampoSeleccionado;
                    SeleccionRealizada = true;
                    DialogResult = DialogResult.OK;
                    Close();
                    return;
                }

                Show();
                BringToFront();
            }
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
                this.Dispose();
            }
        }
    }

    // Fin del código de "José Pablo Cano Cóbar" - Carné: "0901-23-1727" - Fecha: "15/09/26"
}