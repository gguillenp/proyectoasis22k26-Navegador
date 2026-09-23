using CapaControlador_Consultas;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace CapaVista_Consultas
{
    //Inicio del código realizado por Diana Mishel Loeiza Ramírez 9959-23-3457 20/09/2026
    public partial class FrmMantenimientoConsultas : Componentes.ClsBaseTerminus
    {
        private readonly ClsControladorMantenimiento _Control = new ClsControladorMantenimiento();
        private Dictionary<string, string> _Tipos = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private string _Tabla;

        private const int _ColCampo = 0;
        private const int _ColOperador = 1;
        private const int _ColValor = 2;
        private const int _ColOrden = 3;

        public FrmMantenimientoConsultas(string Tabla)
        {
            InitializeComponent();
            _Tabla = (Tabla ?? "").Trim();
            Load += ConsultasMetFrmMantenimientoConsultasLoad;
        }

        private void ConsultasMetFrmMantenimientoConsultasLoad(object Sender, EventArgs Evento)
        {
            try
            {
                ConsultasProcConectarEventos();
                ConsultasProcCargarOperadores();


                if (_Tabla == "")
                {
                    BeginInvoke(new MethodInvoker(Close));
                    return;
                }

                ConsultasProcCargarColumnas();
            }
            catch (Exception Exception)
            {
                ConsultasProcMostrarError("No se pudo cargar el formulario.", Exception);
                BeginInvoke(new MethodInvoker(Close));
            }
        }

        private void ConsultasProcConectarEventos()
        {
            ConsultasCboOperador.SelectedIndexChanged += ConsultasCboOperador_SelectedIndexChanged;
            ConsultasBtnIngresar.Click += ConsultasMetBtnIngresarClick;
            ConsultasBtnEliminar.Click += ConsultasMetBtnEliminarClick;
            ConsultasBtnGuardar.Click += ConsultasMetBtnGuardarClick;
        }

        private void ConsultasProcCargarOperadores()
        {
            ConsultasCboOperador.Items.Clear();
            ConsultasCboOperador.Items.AddRange(new object[]
            {
                "=", "<>", ">", "<", ">=", "<=", "LIKE", "NOT LIKE", "IS NULL", "IS NOT NULL"
            });
            ConsultasCboOperador.SelectedIndex = -1;
        }

        private void ConsultasProcCargarColumnas()
        {
            _Tipos = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            ConsultasCboOperadorCampo.Items.Clear();

            foreach (KeyValuePair<string, string> col in _Control.ConsultasMetObtenerColumnas(_Tabla))
            {
                _Tipos[col.Key] = col.Value;
                ConsultasCboOperadorCampo.Items.Add(col.Key);
            }

            if (_Tipos.Count == 0)
            {
                throw new ArgumentException(
                    "No se encontraron campos para \"" + _Tabla + "\". Revisa que exista en la base de datos dbConsulta.");
            }

            ConsultasCboOperadorCampo.SelectedIndex = -1;
        }
        private void ConsultasCboOperador_SelectedIndexChanged(object Sender, EventArgs Evento)
        {
            string Operador = ConsultasCboOperador.SelectedItem == null ? "" : ConsultasCboOperador.SelectedItem.ToString();
            bool SinValor = Operador == "IS NULL" || Operador == "IS NOT NULL";

            ConsultasTxtValor.Enabled = !SinValor;
            if (SinValor)
            {
                ConsultasTxtValor.Clear();
            }
        }

        private void ConsultasMetBtnIngresarClick(object Sender, EventArgs Evento)
        {
            try
            {
                if (ConsultasCboOperadorCampo.SelectedItem == null)
                {
                    ConsultasProcAviso("Selecciona un campo.");
                    return;
                }

                string Campo = ConsultasCboOperadorCampo.SelectedItem.ToString();
                string Operador = ConsultasCboOperador.SelectedItem == null ? "" : ConsultasCboOperador.SelectedItem.ToString();
                string Valor = ConsultasTxtValor.Text.Trim();
                string Orden = ConsultasRdoAscendente.Checked ? "ASC" : (ConsultasRdoDescendente.Checked ? "DESC" : "");

                if (Operador == "" && Valor != "")
                {
                    ConsultasProcAviso("Selecciona un operador para usar el valor.");
                    return;
                }
                if (Operador == "" && Orden == "")
                {
                    ConsultasProcAviso("Selecciona un operador con su valor, o un ordenamiento (ASC / DESC).");
                    return;
                }

                ClsCondicion Fila = new ClsCondicion();
                Fila.Campo = Campo;
                Fila.Operador = Operador;
                Fila.Valor = Valor;
                Fila.Orden = Orden;

                _Control.ConsultasProcValidarCondicion(Fila, _Tipos);

                ConsultasDgvConsultasFiltros.Rows.Add(Fila.Campo, Fila.Operador, Fila.Valor, Fila.Orden);

                ConsultasTxtValor.Clear();
                ConsultasCboOperadorCampo.SelectedIndex = -1;
                ConsultasCboOperador.SelectedIndex = -1;
                ConsultasRdoAscendente.Checked = true;
                ConsultasRdoDescendente.Checked = false;
            }
            catch (ArgumentException Exception)
            {
                ConsultasProcAviso(Exception.Message);
            }
            catch (Exception Exception)
            {
                ConsultasProcMostrarError("No se pudo agregar la condicion.", Exception);
            }
        }

        private void ConsultasMetBtnEliminarClick(object Sender, EventArgs Evento)
        {
            if (ConsultasDgvConsultasFiltros.SelectedRows.Count == 0)
            {
                ConsultasProcAviso("Selecciona en la tabla la condicion que quieres quitar.");
                return;
            }

            ConsultasDgvConsultasFiltros.Rows.Remove(ConsultasDgvConsultasFiltros.SelectedRows[0]);
        }

        private void ConsultasMetBtnGuardarClick(object Sender, EventArgs Evento)
        {
            try
            {
                string Nombre = ConsultasTxtNombre.Text.Trim();
                if (Nombre == "")
                {
                    ConsultasProcAviso("Escribe un nombre para la consulta.");
                    ConsultasTxtNombre.Focus();
                    return;
                }

                string Query = _Control.ConsultasFuncConstruirQuery(_Tabla, ConsultasMetLeerCondiciones(), _Tipos);

                DialogResult r = MessageBox.Show(this,
                    "Se guardara la consulta \"" + Nombre + "\":" + Environment.NewLine + Environment.NewLine +
                    Query + Environment.NewLine + Environment.NewLine + "¿Guardar?",
                    "Guardar consulta", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

                if (r != DialogResult.Yes)
                {
                    return;
                }

                _Control.ConsultasProcGuardar(Nombre, _Tabla, Query);

                MessageBox.Show(this, "Consulta guardada.", "Mantenimiento de consultas",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);

                Close();
            }
            catch (ArgumentException Exception)
            {
                ConsultasProcAviso(Exception.Message);
            }
            catch (Exception Exception)
            {
                ConsultasProcMostrarError("No se pudo guardar la consulta.", Exception);
            }
        }



        private List<ClsCondicion> ConsultasMetLeerCondiciones()
        {
            List<ClsCondicion> Lista = new List<ClsCondicion>();
            bool ExisteCondicionPrevia = false;

            foreach (DataGridViewRow Fila in ConsultasDgvConsultasFiltros.Rows)
            {
                ClsCondicion Condicion = new ClsCondicion();
                Condicion.Campo = ConsultasFuncTexto(Fila.Cells[_ColCampo]);
                Condicion.Operador = ConsultasFuncTexto(Fila.Cells[_ColOperador]);
                Condicion.Valor = ConsultasFuncTexto(Fila.Cells[_ColValor]);
                Condicion.Orden = ConsultasFuncTexto(Fila.Cells[_ColOrden]);

                if (Condicion.Operador != "")
                {
                    Condicion.Conector = ExisteCondicionPrevia ? "AND" : "";
                    ExisteCondicionPrevia = true;
                }
                Lista.Add(Condicion);
            }
            return Lista;
        }

        private void ConsultasProcLimpiarFormulario()
        {
            ConsultasDgvConsultasFiltros.Rows.Clear();
            ConsultasTxtNombre.Clear();
            ConsultasTxtValor.Clear();
            ConsultasTxtValor.Enabled = true;
            ConsultasCboOperadorCampo.SelectedIndex = -1;
            ConsultasCboOperador.SelectedIndex = -1;
            ConsultasRdoAscendente.Checked = false;
            ConsultasRdoDescendente.Checked = false;
        }

        private static string ConsultasFuncTexto(DataGridViewCell Celda)
        {
            return Celda.Value == null ? "" : Celda.Value.ToString();
        }

        private void ConsultasProcAviso(string Mensaje)
        {
            MessageBox.Show(this, Mensaje, "Mantenimiento de consultas",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        private void ConsultasProcMostrarError(string Mensaje, Exception Exception)
        {
            MessageBox.Show(this, Mensaje + Environment.NewLine + Environment.NewLine + Exception.Message,
                "Mantenimiento de consultas", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        //Fin del código realizado por Diana Mishel Loeiza Ramírez 9959-23-3457 20/09/2026

        //Inicio del código realizado por Diego Fernando Santizo Samayoa 0901-22-15950 22/09/2026
        private void ConsultasMetBtnAyudaClick(object sender, EventArgs e)
        {
            DirectoryInfo Directorio = new DirectoryInfo(Application.StartupPath);

            while (Directorio != null)
            {
                string Ruta = Path.Combine(Directorio.FullName, "ayuda", "componentes", "consultas", "Ayuda_Consultas.chm");

                if (File.Exists(Ruta))
                {
                    Help.ShowHelp(this, Ruta, "ConsultasReutilizables.html");
                    return;
                }
                Directorio = Directorio.Parent;
            }

            MessageBox.Show("No se encontró el archivo de ayuda.");
        }
        //Fin del código realizado por Diego Fernando Santizo Samayoa 0901-22-15950 22/09/2026
    }

    //Fin del código realizado por Diana Mishel Loeiza Ramírez 9959-23-3457 20/09/2026
}
