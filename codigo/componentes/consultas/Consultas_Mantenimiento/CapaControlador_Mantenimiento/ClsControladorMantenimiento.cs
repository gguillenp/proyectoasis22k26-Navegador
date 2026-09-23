using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using CapaModelo_Consultas;

namespace CapaControlador_Consultas
{
    //Inicio del código realizado por Diana Mishel Loeiza Ramírez 9959-23-3457
    public class ClsControladorMantenimiento
    {
        private readonly ClsModeloMantenimiento _Modelo = new ClsModeloMantenimiento();

        private static readonly string[] _OperadoresValidos =
            { "=", "<>", ">", "<", ">=", "<=", "LIKE", "NOT LIKE", "IS NULL", "IS NOT NULL" };

        private static readonly HashSet<string> _TiposNumericos = new HashSet<string>(
            new string[] { "int", "integer", "bigint", "smallint", "mediumint", "tinyint",
                           "decimal", "numeric", "float", "double" });

        public List<KeyValuePair<string, string>> ConsultasMetObtenerColumnas(string Tabla)
        {
            ConsultasProcValidarIdentificador(Tabla, "tabla");
            return _Modelo.ObtenerColumnas(Tabla);
        }


        public void ConsultasProcValidarCondicion(ClsCondicion Condicion, Dictionary<string, string> Tipos)
        {
            ConsultasProcValidarIdentificador(Condicion.Campo, "campo");
            if (!string.IsNullOrEmpty(Condicion.Operador))
            {
                ConsultasFuncFormatearCondicion(Condicion, Tipos);
            }
        }

        public string ConsultasFuncConstruirQuery(string Tabla, List<ClsCondicion> Filas, Dictionary<string, string> Tipos)
        {
            ConsultasProcValidarIdentificador(Tabla, "tabla");

            StringBuilder Where = new StringBuilder();
            List<string> Orden = new List<string>();

            foreach (ClsCondicion Condicion in Filas)
            {
                ConsultasProcValidarIdentificador(Condicion.Campo, "campo");
                if (Tipos != null && Tipos.Count > 0 && !Tipos.ContainsKey(Condicion.Campo))
                {
                    throw new ArgumentException("El campo " + Condicion.Campo + " no existe en " + Tabla + ".");
                }

                if (!string.IsNullOrEmpty(Condicion.Operador))
                {
                    if (Where.Length > 0)
                    {
                        Where.Append(Condicion.Conector == "OR" ? " OR " : " AND ");
                    }
                    Where.Append(ConsultasFuncFormatearCondicion(Condicion, Tipos));
                }

                if (Condicion.Orden == "ASC" || Condicion.Orden == "DESC")
                {
                    Orden.Add(Condicion.Campo + " " + Condicion.Orden);
                }
            }

            StringBuilder Sql = new StringBuilder("SELECT * FROM " + Tabla);
            if (Where.Length > 0)
            {
                Sql.Append(" WHERE ").Append(Where.ToString());
            }
            if (Orden.Count > 0)
            {
                Sql.Append(" ORDER BY ").Append(string.Join(", ", Orden));
            }
            Sql.Append(";");
            return Sql.ToString();
        }

        private static string ConsultasFuncFormatearCondicion(ClsCondicion Condicion, Dictionary<string, string> Tipos)
        {
            if (Array.IndexOf(_OperadoresValidos, Condicion.Operador) < 0)
            {
                throw new ArgumentException("Operador no valido: " + Condicion.Operador);
            }

            if (Condicion.Operador == "IS NULL" || Condicion.Operador == "IS NOT NULL")
            {
                return Condicion.Campo + " " + Condicion.Operador;
            }

            string valor = (Condicion.Valor ?? "").Trim();
            if (valor.Length == 0)
            {
                throw new ArgumentException("Escribe un valor para el campo " + Condicion.Campo + ".");
            }

            bool ConfirmarLike = Condicion.Operador.EndsWith("LIKE");
            string Tipo = null;
            if (Tipos != null)
            {
                Tipos.TryGetValue(Condicion.Campo, out Tipo);
            }

            string literal;
            if (!ConfirmarLike && Tipo != null && _TiposNumericos.Contains(Tipo.ToLowerInvariant()))
            {
                decimal numero;
                if (!decimal.TryParse(valor,
                        NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint,
                        CultureInfo.InvariantCulture, out numero))
                {
                    throw new ArgumentException("El campo " + Condicion.Campo + " es numerico. Escribe un numero, por ejemplo 5000 o 12.50.");
                }
                literal = numero.ToString(CultureInfo.InvariantCulture);
            }
            else
            {
                literal = "'" + valor.Replace("\\", "\\\\").Replace("'", "''") + "'";
            }

            return Condicion.Campo + " " + Condicion.Operador + " " + literal;
        }

        private static void ConsultasProcValidarIdentificador(string Nombre, string Auxiliar)
        {
            if (string.IsNullOrWhiteSpace(Nombre))
            {
                throw new ArgumentException("Selecciona " + (Auxiliar == "tabla" ? "una tabla o vista." : "un campo."));
            }
            if (!Regex.IsMatch(Nombre, "^[A-Za-z0-9_]+$"))
            {
                throw new ArgumentException("El nombre de " + Auxiliar + " no es valido: " + Nombre);
            }
        }

    

        public void ConsultasProcGuardar(string Nombre, string Tabla, string Query)
        {
            Nombre = (Nombre ?? "").Trim();

            if (Nombre.Length == 0)
            {
                throw new ArgumentException("Escribe un nombre para la consulta.");
            }
            if (Nombre.Length > 100)
            {
                throw new ArgumentException("El nombre no puede pasar de 100 caracteres.");
            }
            if (string.IsNullOrWhiteSpace(Tabla))
            {
                throw new ArgumentException("Selecciona una tabla o vista.");
            }
            ConsultasProcValidarEsSelect(Query);

            if (_Modelo.ExisteNombre(Nombre))
            {
                throw new ArgumentException("Ya existe una consulta con ese nombre. Usa otro.");
            }

            _Modelo.Insertar(Nombre, Tabla, Query);
        }

        public DataTable ConsultasFuncPrueba(string Query)
        {
            ConsultasProcValidarEsSelect(Query);
            return _Modelo.Ejecutar(Query, 500);
        }

        private static void ConsultasProcValidarEsSelect(string Query)
        {
            if (string.IsNullOrWhiteSpace(Query) ||
                !Query.TrimStart().StartsWith("SELECT ", StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException("Primero arma la consulta: elige una tabla o vista.");
            }
        }
    }

    //Fin del código realizado por Diana Mishel Loeiza Ramírez 9959-23-3457
}