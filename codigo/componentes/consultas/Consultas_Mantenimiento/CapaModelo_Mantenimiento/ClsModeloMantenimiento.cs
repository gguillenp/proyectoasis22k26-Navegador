using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Odbc;

namespace CapaModelo_Consultas
{

    //Inicio del código realizado por Diana Mishel Loeiza Ramírez 9959-23-3457 20/09/2026
    public class ClsModeloMantenimiento
    {
        private static OdbcConnection Abrir()
        {
            OdbcConnection Conexion = new ClsConexion().ConsultasFuncConexion();
            if (Conexion.State != ConnectionState.Open)
            {
                throw new InvalidOperationException(
                    "No se pudo conectar a la base de datos. Revisa que el DSN \"EmbutidosS.A\" exista y apunte a dbConsulta.");
            }
            return Conexion;
        }
        
        public List<string> ObtenerTablas()
        {
            List<string> lista = new List<string>();
            const string Sql =
                "SELECT TABLE_NAME FROM information_schema.TABLES " +
                "WHERE TABLE_SCHEMA = DATABASE() ORDER BY TABLE_NAME";

            using (OdbcConnection Conexion = Abrir())
            using (OdbcCommand Comando = new OdbcCommand(Sql, Conexion))
            {
                using (OdbcDataReader DataReader = Comando.ExecuteReader())
                {
                    while (DataReader.Read())
                    {
                        lista.Add(Convert.ToString(DataReader[0]));
                    }
                }
            }
            return lista;
        }

        
        public List<KeyValuePair<string, string>> ObtenerColumnas(string Tabla)
        {
            List<KeyValuePair<string, string>> Lista = new List<KeyValuePair<string, string>>();
            const string Sql =
                "SELECT COLUMN_NAME, DATA_TYPE FROM information_schema.COLUMNS " +
                "WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = ? ORDER BY ORDINAL_POSITION";

            using (OdbcConnection Conexion = Abrir())
            using (OdbcCommand Comando = new OdbcCommand(Sql, Conexion))
            {
                Comando.Parameters.AddWithValue("@tabla", Tabla);
                using (OdbcDataReader DataReader = Comando.ExecuteReader())
                {
                    while (DataReader.Read())
                    {
                        Lista.Add(new KeyValuePair<string, string>(
                            Convert.ToString(DataReader[0]), Convert.ToString(DataReader[1])));
                    }
                }
            }
            return Lista;
        }

        public bool ExisteNombre(string Nombre)
        {
            const string Sql = "SELECT COUNT(*) FROM tblConsulta WHERE nombreConsulta = ?";

            using (OdbcConnection Conexion = Abrir())
            using (OdbcCommand Comando = new OdbcCommand(Sql, Conexion))
            {
                Comando.Parameters.AddWithValue("@nombre", Nombre);
                return Convert.ToInt32(Comando.ExecuteScalar()) > 0;
            }
        }

        public void Insertar(string Nombre, string Tabla, string Query)
        {
            const string Sql =
                "INSERT INTO tblConsulta (nombreConsulta, tablaConsulta, queryConsulta) VALUES (?, ?, ?)";

            using (OdbcConnection Conexion = Abrir())
            using (OdbcCommand Comando = new OdbcCommand(Sql, Conexion))
            {
                Comando.Parameters.AddWithValue("@nombre", Nombre);
                Comando.Parameters.AddWithValue("@tabla", Tabla);
                Comando.Parameters.AddWithValue("@query", Query);
                Comando.ExecuteNonQuery();
            }
        }

      
        public DataTable Ejecutar(string Query, int FilasMaximas)
        {
            using (OdbcConnection Conexion = Abrir())
            using (OdbcDataAdapter DataAdapter = new OdbcDataAdapter(Query, Conexion))
            {
                DataSet ds = new DataSet();
                DataAdapter.Fill(ds, 0, FilasMaximas, "resultado");
                return ds.Tables["resultado"];
            }
        }
    }

    //Fin del código realizado por Diana Mishel Loeiza Ramírez 9959-23-3457 20/09/2026
}