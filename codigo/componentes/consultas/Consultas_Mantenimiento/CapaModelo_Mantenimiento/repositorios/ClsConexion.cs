using System;
using System.Data.Odbc;

namespace CapaModelo_Consultas
{

    //Inicio del código realizado por Diego Fernando Santizo Samayoa 0901-22-15950 21/09/2026
    internal class ClsConexion
    {
        public OdbcConnection ConsultasFuncConexion()
        {
            OdbcConnection Conexion = new OdbcConnection("Dsn=EmbutidosS.A");
            try
            {
                Conexion.Open();
            }
            catch (OdbcException Exception)
            {
                Console.WriteLine("Conexion fallida. Error: " + Exception.Message);
            }
            return Conexion;
        }

        public void ConsultasProcDesconexion(OdbcConnection Conexion)
        {
            try
            {
                Conexion.Close();
            }
            catch (OdbcException Exception)
            {
                Console.WriteLine("Error al cerrar la conexión. Error: " + Exception.Message);
            }
        }
        //Fin del código realizado por Diego Fernando Santizo Samayoa 0901-22-15950 21/09/2026
    }
}
