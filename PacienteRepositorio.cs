using Microsoft.Data.SqlClient;

namespace RegistroPacientes;

/// <summary>
/// Acceso a datos con ADO.NET. Todas las consultas usan PARAMETROS (@Nombre, @Id, ...)
/// para evitar la inyeccion SQL: los valores del usuario nunca se concatenan en el SQL.
/// </summary>
public class PacienteRepositorio
{
    // Consulta base: une Pacientes con TiposSangre para traer el nombre del tipo de sangre
    private const string ConsultaBase =
        "SELECT p.Id, p.Nombre, p.Apellido, p.Cedula, p.FechaNacimiento, p.Telefono, t.Nombre AS TipoSangre, p.Direccion " +
        "FROM Pacientes p INNER JOIN TiposSangre t ON t.Id = p.TipoSangreId ";

    // ===================== CONEXION =====================
    public void ProbarConexion()
    {
        using (SqlConnection conexion = new SqlConnection(ConexionBD.CadenaConexion))
        {
            conexion.Open();
        }
    }

    // ===================== AGREGAR =====================
    // Devuelve el Id que genero la base de datos
    public int Agregar(Paciente paciente)
    {
        string sql = "INSERT INTO Pacientes (Nombre, Apellido, Cedula, FechaNacimiento, Telefono, TipoSangreId, Direccion) " +
                     "OUTPUT INSERTED.Id " +
                     "VALUES (@Nombre, @Apellido, @Cedula, @FechaNacimiento, @Telefono, " +
                     "(SELECT Id FROM TiposSangre WHERE Nombre = @TipoSangre), @Direccion)";
        int id = 0;

        using (SqlConnection conexion = new SqlConnection(ConexionBD.CadenaConexion))
        {
            conexion.Open();
            using (SqlCommand comando = new SqlCommand(sql, conexion))
            {
                AgregarParametros(comando, ParametrosDe(paciente));
                id = Convert.ToInt32(comando.ExecuteScalar());
            }
        }

        RegistrarAuditoria(id, "INSERT", "Paciente creado: " + paciente.NombreCompleto);
        return id;
    }

    // ===================== CONSULTAS =====================
    public List<Paciente> ObtenerActivos()
    {
        return EjecutarConsulta(ConsultaBase + "WHERE p.Activo = 1 ORDER BY p.Id", new Dictionary<string, object>());
    }

    // Devuelve null si no existe (o esta eliminado)
    public Paciente BuscarPorId(int id)
    {
        Dictionary<string, object> parametros = new Dictionary<string, object>();
        parametros.Add("@Id", id);
        List<Paciente> resultado = EjecutarConsulta(ConsultaBase + "WHERE p.Id = @Id AND p.Activo = 1", parametros);
        return resultado.FirstOrDefault();
    }

    public List<Paciente> BuscarPorNombre(string texto)
    {
        Dictionary<string, object> parametros = new Dictionary<string, object>();
        parametros.Add("@Texto", "%" + texto + "%");
        return EjecutarConsulta(ConsultaBase +
            "WHERE p.Activo = 1 AND (p.Nombre LIKE @Texto OR p.Apellido LIKE @Texto) ORDER BY p.Apellido, p.Nombre", parametros);
    }

    public List<Paciente> BuscarPorCedula(string cedula)
    {
        Dictionary<string, object> parametros = new Dictionary<string, object>();
        parametros.Add("@Cedula", cedula);
        return EjecutarConsulta(ConsultaBase + "WHERE p.Activo = 1 AND p.Cedula = @Cedula", parametros);
    }

    public List<Paciente> BuscarPorTipoSangre(string tipoSangre)
    {
        Dictionary<string, object> parametros = new Dictionary<string, object>();
        parametros.Add("@TipoSangre", tipoSangre);
        return EjecutarConsulta(ConsultaBase +
            "WHERE p.Activo = 1 AND t.Nombre = @TipoSangre ORDER BY p.Apellido, p.Nombre", parametros);
    }

    // Revisa todas las filas (activas e inactivas) porque la cedula es UNIQUE en la tabla
    public bool ExisteCedula(string cedula, int idExcluir)
    {
        string sql = "SELECT COUNT(*) FROM Pacientes WHERE Cedula = @Cedula AND Id <> @Id";
        Dictionary<string, object> parametros = new Dictionary<string, object>();
        parametros.Add("@Cedula", cedula);
        parametros.Add("@Id", idExcluir);
        int cantidad = 0;

        using (SqlConnection conexion = new SqlConnection(ConexionBD.CadenaConexion))
        {
            conexion.Open();
            using (SqlCommand comando = new SqlCommand(sql, conexion))
            {
                AgregarParametros(comando, parametros);
                cantidad = Convert.ToInt32(comando.ExecuteScalar());
            }
        }
        return cantidad > 0;
    }

    // ===================== MODIFICAR =====================
    public void Actualizar(Paciente paciente)
    {
        string sql = "UPDATE Pacientes SET Nombre = @Nombre, Apellido = @Apellido, Cedula = @Cedula, " +
                     "FechaNacimiento = @FechaNacimiento, Telefono = @Telefono, " +
                     "TipoSangreId = (SELECT Id FROM TiposSangre WHERE Nombre = @TipoSangre), " +
                     "Direccion = @Direccion, FechaModificacion = GETDATE() " +
                     "WHERE Id = @Id AND Activo = 1";
        Dictionary<string, object> parametros = ParametrosDe(paciente);
        parametros.Add("@Id", paciente.Id);

        int filas = EjecutarComando(sql, parametros);
        if (filas > 0)
        {
            RegistrarAuditoria(paciente.Id, "UPDATE", "Paciente modificado: " + paciente.NombreCompleto);
        }
    }

    // ===================== ELIMINAR (borrado logico) =====================
    // No borra la fila: la marca como inactiva para conservar el historial
    public bool Eliminar(int id)
    {
        string sql = "UPDATE Pacientes SET Activo = 0, FechaModificacion = GETDATE() WHERE Id = @Id AND Activo = 1";
        Dictionary<string, object> parametros = new Dictionary<string, object>();
        parametros.Add("@Id", id);

        int filas = EjecutarComando(sql, parametros);
        if (filas > 0)
        {
            RegistrarAuditoria(id, "DELETE", "Paciente desactivado (borrado lógico)");
        }
        return filas > 0;
    }

    // ===================== HISTORIAL =====================
    public List<string> ObtenerHistorial()
    {
        string sql = "SELECT a.Fecha, a.Accion, a.PacienteId, a.Detalle FROM AuditoriaPacientes a ORDER BY a.Id DESC";
        List<string> lineas = new List<string>();

        using (SqlConnection conexion = new SqlConnection(ConexionBD.CadenaConexion))
        {
            conexion.Open();
            using (SqlCommand comando = new SqlCommand(sql, conexion))
            {
                using (SqlDataReader lector = comando.ExecuteReader())
                {
                    while (lector.Read())
                    {
                        string detalle = lector.IsDBNull(3) ? "" : lector.GetString(3);
                        lineas.Add(lector.GetDateTime(0).ToString("dd/MM/yyyy HH:mm") + "  "
                                 + lector.GetString(1).PadRight(8)
                                 + "Paciente " + lector.GetInt32(2).ToString().PadRight(5)
                                 + detalle);
                    }
                }
            }
        }
        return lineas;
    }

    // ===================== METODOS PRIVADOS =====================
    private void RegistrarAuditoria(int pacienteId, string accion, string detalle)
    {
        string sql = "INSERT INTO AuditoriaPacientes (PacienteId, Accion, Detalle) VALUES (@PacienteId, @Accion, @Detalle)";
        Dictionary<string, object> parametros = new Dictionary<string, object>();
        parametros.Add("@PacienteId", pacienteId);
        parametros.Add("@Accion", accion);
        parametros.Add("@Detalle", detalle);
        EjecutarComando(sql, parametros);
    }

    // Ejecuta un SELECT y convierte cada fila en un objeto Paciente
    private List<Paciente> EjecutarConsulta(string sql, Dictionary<string, object> parametros)
    {
        List<Paciente> lista = new List<Paciente>();

        using (SqlConnection conexion = new SqlConnection(ConexionBD.CadenaConexion))
        {
            conexion.Open();
            using (SqlCommand comando = new SqlCommand(sql, conexion))
            {
                AgregarParametros(comando, parametros);
                using (SqlDataReader lector = comando.ExecuteReader())
                {
                    while (lector.Read())
                    {
                        lista.Add(ConvertirAPaciente(lector));
                    }
                }
            }
        }
        return lista;
    }

    // Ejecuta INSERT / UPDATE / DELETE y devuelve cuantas filas afecto
    private int EjecutarComando(string sql, Dictionary<string, object> parametros)
    {
        int filas = 0;

        using (SqlConnection conexion = new SqlConnection(ConexionBD.CadenaConexion))
        {
            conexion.Open();
            using (SqlCommand comando = new SqlCommand(sql, conexion))
            {
                AgregarParametros(comando, parametros);
                filas = comando.ExecuteNonQuery();
            }
        }
        return filas;
    }

    private void AgregarParametros(SqlCommand comando, Dictionary<string, object> parametros)
    {
        foreach (KeyValuePair<string, object> parametro in parametros)
        {
            comando.Parameters.AddWithValue(parametro.Key, parametro.Value);
        }
    }

    private Dictionary<string, object> ParametrosDe(Paciente paciente)
    {
        Dictionary<string, object> parametros = new Dictionary<string, object>();
        parametros.Add("@Nombre", paciente.Nombre);
        parametros.Add("@Apellido", paciente.Apellido);
        parametros.Add("@Cedula", paciente.Cedula);
        parametros.Add("@FechaNacimiento", paciente.FechaNacimiento);
        parametros.Add("@Telefono", paciente.Telefono);
        parametros.Add("@TipoSangre", paciente.TipoSangre);
        parametros.Add("@Direccion", paciente.Direccion);
        return parametros;
    }

    // El orden de las columnas coincide con el SELECT de ConsultaBase
    private Paciente ConvertirAPaciente(SqlDataReader lector)
    {
        return new Paciente(
            lector.GetInt32(0),
            lector.GetString(1),
            lector.GetString(2),
            lector.GetString(3),
            lector.GetDateTime(4),
            lector.GetString(5),
            lector.GetString(6),
            lector.IsDBNull(7) ? "" : lector.GetString(7));
    }
}
