namespace RegistroPacientes;

/// <summary>
/// Guarda en un solo lugar la cadena de conexion a SQL Server.
/// </summary>
public static class ConexionBD
{
    // Server: ".\\SQLEXPRESS" = SQL Server Express instalado en este mismo equipo.
    //         Si usa otra instancia, cambie el nombre (por ejemplo "MI-PC\\SQLEXPRESS" o "." ).
    // Database: base creada con el script ModeloDatos.sql
    // Trusted_Connection: usa la cuenta de Windows (no requiere usuario ni clave)
    public const string CadenaConexion =
        "Server=.\\SQLEXPRESS;Database=RegistroPacientesDB;Trusted_Connection=True;TrustServerCertificate=True;";
}
