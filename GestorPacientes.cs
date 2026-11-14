namespace RegistroPacientes;

/// <summary>
/// Logica del sistema. En esta version los datos viven en SQL Server:
/// el gestor pide los datos al usuario y se los entrega al repositorio.
/// </summary>
public class GestorPacientes
{
    private PacienteRepositorio _repositorio;

    public GestorPacientes(PacienteRepositorio repositorio)
    {
        _repositorio = repositorio;
    }

    // ===================== AGREGAR =====================
    public void Agregar()
    {
        Console.WriteLine("--- AGREGAR PACIENTE ---");
        Paciente paciente = new Paciente();
        paciente.Nombre = Entrada.PedirTextoObligatorio("Nombre");
        paciente.Apellido = Entrada.PedirTextoObligatorio("Apellido");
        paciente.Cedula = PedirCedulaUnica(0);
        paciente.FechaNacimiento = Entrada.PedirFechaNacimiento();
        paciente.Telefono = Entrada.PedirTelefono();
        paciente.TipoSangre = Entrada.PedirTipoSangre();
        paciente.Direccion = Entrada.PedirTextoObligatorio("Dirección");

        int id = _repositorio.Agregar(paciente);
        paciente.AsignarId(id);

        Console.WriteLine("Paciente agregado correctamente con el Id " + paciente.Id + ".");
    }

    // ===================== LISTAR =====================
    public void Listar()
    {
        Console.WriteLine("--- LISTADO DE PACIENTES ---");
        List<Paciente> pacientes = _repositorio.ObtenerActivos();
        if (pacientes.Count == 0)
        {
            Console.WriteLine("No hay pacientes registrados.");
        }
        else
        {
            MostrarTabla(pacientes);
        }
    }

    // ===================== BUSCAR =====================
    public void Buscar()
    {
        List<Paciente> encontrados = new List<Paciente>();
        int criterio = 0;

        Console.WriteLine("--- BUSCAR PACIENTE ---");
        Console.WriteLine("1. Por Id");
        Console.WriteLine("2. Por nombre o apellido");
        Console.WriteLine("3. Por cédula");
        Console.WriteLine("4. Por tipo de sangre");
        criterio = Entrada.PedirEntero("Elija el criterio de búsqueda: ");

        switch (criterio)
        {
            case 1:
                int idBuscado = Entrada.PedirEntero("Digite el Id: ");
                Paciente porId = _repositorio.BuscarPorId(idBuscado);
                if (porId != null)
                {
                    encontrados.Add(porId);
                }
                break;
            case 2:
                Console.Write("Digite el nombre o apellido (o parte de él): ");
                encontrados = _repositorio.BuscarPorNombre(Entrada.Leer());
                break;
            case 3:
                Console.Write("Digite la cédula: ");
                encontrados = _repositorio.BuscarPorCedula(Entrada.Leer());
                break;
            case 4:
                encontrados = _repositorio.BuscarPorTipoSangre(Entrada.PedirTipoSangre());
                break;
            default:
                Console.WriteLine("Criterio inválido. Digite un número del 1 al 4.");
                break;
        }

        if (criterio >= 1 && criterio <= 4)
        {
            if (encontrados.Count == 0)
            {
                Console.WriteLine("No se encontraron pacientes con ese criterio.");
            }
            else
            {
                Console.WriteLine("Resultados encontrados: " + encontrados.Count);
                MostrarTabla(encontrados);
            }
        }
    }

    // ===================== MODIFICAR =====================
    public void Modificar()
    {
        Console.WriteLine("--- MODIFICAR PACIENTE ---");
        List<Paciente> pacientes = _repositorio.ObtenerActivos();
        if (pacientes.Count == 0)
        {
            Console.WriteLine("No hay pacientes para modificar.");
            return;
        }

        MostrarTabla(pacientes);
        int id = Entrada.PedirEntero("Digite el Id del paciente a modificar: ");
        Paciente paciente = _repositorio.BuscarPorId(id);

        if (paciente == null)
        {
            Console.WriteLine("No existe un paciente con ese Id.");
            return;
        }

        bool huboCambios = false;
        int campo = -1;
        while (campo != 0)
        {
            Console.WriteLine();
            Console.WriteLine("Paciente: " + paciente.NombreCompleto);
            Console.WriteLine("1. Nombre");
            Console.WriteLine("2. Apellido");
            Console.WriteLine("3. Cédula");
            Console.WriteLine("4. Fecha de nacimiento");
            Console.WriteLine("5. Teléfono");
            Console.WriteLine("6. Tipo de sangre");
            Console.WriteLine("7. Dirección");
            Console.WriteLine("0. Guardar y terminar");
            campo = Entrada.PedirEntero("¿Qué dato desea modificar?: ");

            switch (campo)
            {
                case 1:
                    paciente.Nombre = Entrada.PedirTextoObligatorio("Nuevo nombre");
                    huboCambios = true;
                    break;
                case 2:
                    paciente.Apellido = Entrada.PedirTextoObligatorio("Nuevo apellido");
                    huboCambios = true;
                    break;
                case 3:
                    paciente.Cedula = PedirCedulaUnica(paciente.Id);
                    huboCambios = true;
                    break;
                case 4:
                    paciente.FechaNacimiento = Entrada.PedirFechaNacimiento();
                    huboCambios = true;
                    break;
                case 5:
                    paciente.Telefono = Entrada.PedirTelefono();
                    huboCambios = true;
                    break;
                case 6:
                    paciente.TipoSangre = Entrada.PedirTipoSangre();
                    huboCambios = true;
                    break;
                case 7:
                    paciente.Direccion = Entrada.PedirTextoObligatorio("Nueva dirección");
                    huboCambios = true;
                    break;
                case 0:
                    break;
                default:
                    Console.WriteLine("Opción inválida. Digite un número del 0 al 7.");
                    break;
            }
        }

        // Los cambios se guardan en la base de datos una sola vez, al terminar
        if (huboCambios)
        {
            _repositorio.Actualizar(paciente);
            Console.WriteLine("Cambios guardados correctamente.");
        }
        else
        {
            Console.WriteLine("No se realizaron cambios.");
        }
    }

    // ===================== ELIMINAR =====================
    public void Eliminar()
    {
        Console.WriteLine("--- ELIMINAR PACIENTE ---");
        List<Paciente> pacientes = _repositorio.ObtenerActivos();
        if (pacientes.Count == 0)
        {
            Console.WriteLine("No hay pacientes para eliminar.");
            return;
        }

        MostrarTabla(pacientes);
        int id = Entrada.PedirEntero("Digite el Id del paciente a eliminar: ");
        Paciente paciente = _repositorio.BuscarPorId(id);

        if (paciente == null)
        {
            Console.WriteLine("No existe un paciente con ese Id.");
            return;
        }

        int confirmacion = 0;
        while (confirmacion != 1 && confirmacion != 2)
        {
            Console.WriteLine("¿Seguro que desea eliminar a " + paciente.NombreCompleto + "? 1. Sí  2. No");
            confirmacion = Entrada.PedirEntero("Opción: ");
            if (confirmacion != 1 && confirmacion != 2)
            {
                Console.WriteLine("Opción inválida. Digite 1 para Sí o 2 para No.");
            }
        }

        if (confirmacion == 1)
        {
            if (_repositorio.Eliminar(id))
            {
                Console.WriteLine("Paciente eliminado correctamente.");
            }
            else
            {
                Console.WriteLine("No se pudo eliminar el paciente.");
            }
        }
        else
        {
            Console.WriteLine("Eliminación cancelada.");
        }
    }

    // ===================== HISTORIAL =====================
    public void VerHistorial()
    {
        Console.WriteLine("--- HISTORIAL DE CAMBIOS ---");
        List<string> historial = _repositorio.ObtenerHistorial();
        if (historial.Count == 0)
        {
            Console.WriteLine("Todavía no hay cambios registrados.");
        }
        else
        {
            foreach (string linea in historial)
            {
                Console.WriteLine(linea);
            }
        }
    }

    // ===================== METODOS AUXILIARES =====================
    private void MostrarTabla(List<Paciente> lista)
    {
        Console.WriteLine(Paciente.ObtenerEncabezado());
        foreach (Paciente paciente in lista)
        {
            Console.WriteLine(paciente.ObtenerFila());
        }
    }

    // idExcluir: Id del paciente que se esta modificando (0 si se esta agregando uno nuevo)
    private string PedirCedulaUnica(int idExcluir)
    {
        string cedula = "";
        bool repetida = true;
        while (repetida)
        {
            cedula = Entrada.PedirCedula();
            repetida = _repositorio.ExisteCedula(cedula, idExcluir);
            if (repetida)
            {
                Console.WriteLine("Ya existe un paciente registrado con esa cédula (activo o eliminado).");
            }
        }
        return cedula;
    }
}
