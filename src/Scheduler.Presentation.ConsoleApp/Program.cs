using Microsoft.Extensions.DependencyInjection;
using Scheduler.Application.DTOs;
using Scheduler.Application.UseCases;
using Scheduler.Domain.Factories;
using Scheduler.Domain.Services;

namespace Scheduler.Presentation.ConsoleApp;

class Program
{
    static void Main(string[] args)
    {
        // metemos en un contenedor para el DI
        var services = new ServiceCollection();
        // Registramos los servicios del dominio
        services.AddSingleton<IRecurrenceStrategyFactory, RecurrenceStrategyFactory>();
        services.AddSingleton<IScheduleCalculator, ScheduleCalculator>();
        // ahora registramos el caso de uso
        services.AddSingleton<CalculateNextExecutionUseCase>();

        var serviceProvider = services.BuildServiceProvider();
        var useCase = serviceProvider.GetRequiredService<CalculateNextExecutionUseCase>();

        bool continuar = true;

        while (continuar)
        {
            Console.Clear();

            try
            {
                Console.WriteLine("================== INPUT ===================");
                Console.WriteLine("============================================");

                var currentDate = ObtenerFecha("Ingrese la fecha CurrentDate (dd/MM/yyyy): ");

                // defino la configuracion base
                Console.WriteLine("\n");
                Console.WriteLine("============== CONFIGURATION ===============");
                Console.WriteLine("============================================");

                // lo mantendre siempre habilitado este check, mientras logro entender su funcionalidad en la imagen ¿?
                bool enabled = true;

                var type = ObtenerTipo();
                var executionDateTime = ObtenerFechaOpcional("Ingrese la fecha y hora de ejecucion(executionDateTime) (dd/MM/yyyy HH:mm): ");

                // si se deja vacio se asignara el valor de currentDate
                if (executionDateTime == null)
                {
                    executionDateTime = currentDate;
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.WriteLine($"ExecutionDateTime asignado automaticamente a: {executionDateTime:dd/MM/yyyy HH:mm}");
                    Console.ResetColor();
                }

                var occurs = ObtenerOccursType();

                // Solicito el campo Every
                int every = 0;
                if (type == "Recurring")
                {
                    every = ObtenerNumero("Ingrese cuantas veces ocurrira (Every): ");
                }
                else if (type == "Once")  /*Para programacion 'Once', el campo Every no es necesario*/
                {
                    every = 0; //
                }

                // defino los limites
                Console.WriteLine("\n");
                Console.WriteLine("================= LIMITS ===================");
                Console.WriteLine("============================================");
                var startDate = ObtenerFechaOpcional("Ingrese la Fecha de Inicio(startDate) (dd/MM/yyyy): ");
                if (startDate == null)
                {
                    startDate = executionDateTime;
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.WriteLine($"StartDate asignado automaticamente a: {startDate:dd/MM/yyyy}");
                    Console.ResetColor();
                }

                var endDate = ObtenerFechaOpcional("Ingrese la Fecha de Fin(endDate) (dd/MM/yyyy): ");
                if (endDate != null)
                {
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.WriteLine($"EndDate asignado a: {endDate:dd/MM/yyyy}");
                    Console.ResetColor();
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.WriteLine($"EndDate no asignado, se considerará sin fecha de fin");
                    Console.ResetColor();
                }

                // Tengo todos mis datos de entrada, creo el DTO para pasarlo al caso de uso
                var dto = new ScheduleRequestDto(
                    Type: type,
                    ExecutionDateTime: executionDateTime.Value,
                    Occurs: occurs,
                    Every: every,
                    StartDate: startDate.Value,
                    EndDate: endDate,
                    Enabled: enabled
                );

                // Ejecutamos el Caso de Uso
                var result = useCase.Execute(currentDate, dto);

                // Enviamos la Salida
                Console.WriteLine("\n");
                Console.WriteLine("================= OUTPUT ===================");
                Console.WriteLine("============================================");
                Console.WriteLine($"Current Date: {currentDate:dd/MM/yyyy HH:mm}");

                if (result.NextExecutionTime.HasValue)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"Next Execution: {result.NextExecutionTime:dd/MM/yyyy HH:mm}");
                    Console.ResetColor();
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"Next Execution: No hay próxima ejecución");
                    Console.ResetColor();
                }

                Console.WriteLine("\n");
                Console.WriteLine("Description: ");
                Console.WriteLine($"  {result.Description}");
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("\n");
                Console.WriteLine($"Error: {ex.Message}");
                Console.ResetColor();
            }

            Console.WriteLine("\n");
            Console.WriteLine("============================================");
            Console.Write("¿Desea calcular otra Programacion? (S/N): ");
            continuar = Console.ReadLine()?.ToLower() == "s";
        }

        Console.WriteLine("\n");
        Console.WriteLine("======= Gracias... Hasta luego! =======");
    }


    // MIS METODOS
    static DateTime ObtenerFecha(string prompt)
    {
        while (true)
        {
            Console.Write(prompt);
            string input = Console.ReadLine() ?? "";

            if (DateTime.TryParseExact(input, new[] { "dd/MM/yyyy HH:mm", "dd/MM/yyyy" }, 
                System.Globalization.CultureInfo.InvariantCulture, 
                System.Globalization.DateTimeStyles.None, 
                out DateTime fecha))
            {
                return fecha;
            }

            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Formato NO VALIDO. Por favor use: dd/MM/yyyy o dd/MM/yyyy HH:mm");
            Console.ResetColor();
        }
    }

    static DateTime? ObtenerFechaOpcional(string prompt)
    {
        while (true)
        {
            Console.Write(prompt);
            string input = Console.ReadLine() ?? "";

            if (string.IsNullOrWhiteSpace(input))
            {
                return null;
            }

            if (DateTime.TryParseExact(input, new[] { "dd/MM/yyyy HH:mm", "dd/MM/yyyy" }, 
                System.Globalization.CultureInfo.InvariantCulture, 
                System.Globalization.DateTimeStyles.None, 
                out DateTime fecha))
            {
                return fecha;
            }

            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Formato NO VALIDO. Por favor use dd/MM/yyyy o dd/MM/yyyy HH:mm");
            Console.ResetColor();
        }
    }

    static string ObtenerTipo()
    {
        Console.WriteLine("Tipos de programacion disponibles:");
        Console.WriteLine("  1. Once - Unica");
        Console.WriteLine("  2. Recurring - Recurrente");

        while (true)
        {
            Console.Write("Seleccione un tipo de programación (1 o 2): ");
            string input = Console.ReadLine() ?? "";

            if (input == "1") return "Once";
            if (input == "2") return "Recurring";

            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Seleccion Invalida, Por favor; seleccione uno de los valores disponibles");
            Console.ResetColor();
        }
    }

    static string ObtenerOccursType()
    {
        Console.WriteLine("Tipo de ocurrencias disponibles:");
        Console.WriteLine("  1. Daily");

        while (true)
        {
            Console.Write("Seleccione un tipo de ocurrencia (1): ");
            string input = Console.ReadLine() ?? "";

            if (input == "1") return "Daily";

            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Seleccion Invalida, Por favor; seleccione uno de los valores disponibles");
            Console.ResetColor();
        }
    }

    static int ObtenerNumero(string prompt)
    {
        while (true)
        {
            Console.Write(prompt);
            string input = Console.ReadLine() ?? "";

            if (int.TryParse(input, out int numero) && numero >= 0)
            {
                return numero;
            }

            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Ingrese un numero valido (mayor o igual a 0)");
            Console.ResetColor();
        }
    }
}
