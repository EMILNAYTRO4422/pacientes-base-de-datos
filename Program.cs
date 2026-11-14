/*
    Tarea: Sistema de Registro de Pacientes con Base de Datos (consola)
    Autor: Emil Gomez Rodriguez - 2026-0935

    - POO: Paciente, Entrada, GestorPacientes, PacienteRepositorio, ConexionBD
    - Base de datos: SQL Server con ADO.NET (Microsoft.Data.SqlClient) y consultas parametrizadas
    - Manejo de errores: errores de base de datos, de formato y errores inesperados
    - El programa no se cierra hasta que el usuario elige la opcion Salir

    Antes de ejecutar: correr el script ModeloDatos.sql en SQL Server Management Studio.
*/

using Microsoft.Data.SqlClient;
using RegistroPacientes;

PacienteRepositorio repositorio = new PacienteRepositorio();
GestorPacientes gestor = new GestorPacientes(repositorio);
int opcion = 0;

Console.WriteLine("========================================");
Console.WriteLine("      REGISTRO DE PACIENTES");
Console.WriteLine("========================================");

// Se prueba la conexion al iniciar. Si falla, el programa sigue abierto y muestra el motivo.
try
{
    repositorio.ProbarConexion();
    Console.WriteLine("Conexión a la base de datos exitosa.");
}
catch (SqlException ex)
{
    Console.WriteLine("No se pudo conectar a la base de datos: " + ex.Message);
    Console.WriteLine("Revise la cadena de conexión en ConexionBD.cs y que ModeloDatos.sql se haya ejecutado.");
}

while (opcion != 7)
{
    try
    {
        Console.WriteLine();
        Console.WriteLine("-------------- MENÚ PRINCIPAL --------------");
        Console.WriteLine("1. Agregar paciente");
        Console.WriteLine("2. Listar pacientes");
        Console.WriteLine("3. Buscar paciente");
        Console.WriteLine("4. Modificar paciente");
        Console.WriteLine("5. Eliminar paciente");
        Console.WriteLine("6. Ver historial de cambios");
        Console.WriteLine("7. Salir");
        opcion = Entrada.PedirEntero("Elija una opción: ");
        Console.WriteLine();

        switch (opcion)
        {
            case 1:
                gestor.Agregar();
                break;
            case 2:
                gestor.Listar();
                break;
            case 3:
                gestor.Buscar();
                break;
            case 4:
                gestor.Modificar();
                break;
            case 5:
                gestor.Eliminar();
                break;
            case 6:
                gestor.VerHistorial();
                break;
            case 7:
                Console.WriteLine("Gracias por usar el sistema. ¡Hasta luego!");
                break;
            default:
                Console.WriteLine("Opción inválida. Digite un número del 1 al 7.");
                break;
        }
    }
    catch (SqlException ex)
    {
        // 2627 y 2601: se violo una restriccion UNIQUE (cedula repetida)
        if (ex.Number == 2627 || ex.Number == 2601)
        {
            Console.WriteLine("Error: ya existe un paciente con esa cédula.");
        }
        else
        {
            Console.WriteLine("Error de base de datos: " + ex.Message);
        }
    }
    catch (Exception ex)
    {
        // Va de ultimo porque Exception contiene a todas las demas
        Console.WriteLine("Ocurrió un error inesperado: " + ex.Message);
    }
    finally
    {
        Console.WriteLine("--------------------------------------------");
    }
}
