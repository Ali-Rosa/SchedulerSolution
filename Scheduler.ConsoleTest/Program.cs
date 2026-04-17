using Scheduler.Domain.Models;
using Scheduler.Domain.Services;

namespace Scheduler.ConsoleTest
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var service = new SchedulerService();

            bool continuar = true;

            while (continuar)
            {
                Console.WriteLine("\n" + new string('-', 50));

                #region INPUT

                Console.Write("Current Date (e.g.: dd/MM/yyyy HH:mm): ");
                DateTime currentDate = GetDate();

                Console.Write("Type (1 = Once / 2 = Recurring): ");
                ScheduleType type = ReadType();

                Console.Write("Is it activated? (Y/N): ");
                bool enabled = GetActivate();

                Console.Write("Date/Time of execution (e.g.: dd/MM/yyyy HH:mm): ");
                var executionDateTime = GetOptionalDate();

                if (executionDateTime == null)
                {
                    executionDateTime = currentDate;
                }

                Console.Write("Occurs (1 = Daily): ");
                OccursType occurs = GetOccursType();

                int every = 0;
                if (type == ScheduleType.Recurring)
                {
                    Console.Write("Enter how many times it will happen (Every): ");
                    every = GetPositiveInteger();
                }
                else if (type == ScheduleType.Once)
                {
                    every = 0;
                }

                Console.Write("Start Date or Enter for unlimited: ");
                var startDate = GetOptionalDate();

                if (startDate == null)
                {
                    startDate = executionDateTime;
                }

                Console.Write("End Date or Enter for unlimited: ");
                DateTime? endDate = GetOptionalDate();

                #endregion INPUT

                #region CREATE CONFIG

                var config = new ScheduleConfiguration(
                    Type: type,
                    ExecutionDateTime: executionDateTime.Value,
                    Occurs: occurs,
                    Enabled: enabled,
                    Every: every,
                    StartDate: startDate.Value,
                    EndDate: endDate
                );

                #endregion CREATE CONFIG

                #region CALCULATE

                var response = service.CalculateNextExecution(currentDate, config);

                #endregion CALCULATE

                #region OUTPUT
                
                Console.WriteLine("\n" + new string('=', 50));
                if (response.IsSuccess)
                {
                    Console.WriteLine($"Next execution : {response.NextExecutionTime:dd/MM/yyyy HH:mm}");
                    Console.WriteLine($"Description          : {response.Description}");
                }
                else
                {
                    Console.WriteLine($"Error: {response.ErrorMessage}");
                }

                #endregion OUTPUT

                /* ==================== REPEAT PROCESS ==================== */
                Console.WriteLine("\nDo you want to calculate another schedule? (Y/N)");
                continuar = GetActivate();
                
            }

            Console.WriteLine("\nThank you for using the Scheduler!");
            Console.ReadKey();
        }

        #region AUXILIARY METHODS 

        private static DateTime GetDate()
        {
            while (true)
            {
                string input = Console.ReadLine() ?? "";

                if (DateTime.TryParseExact(input, new[] { "dd/MM/yyyy HH:mm", "dd/MM/yyyy" },
                    System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.None,
                    out DateTime fecha))
                {
                    return fecha;
                }

                Console.Write("   Invalid format. Please try again: ");
            }
        }

        static DateTime? GetOptionalDate()
        {
            while (true)
            {
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

                Console.WriteLine("   Invalid format. Please try again: ");
            }
        }

        private static ScheduleType ReadType()
        {
            while (true)
            {
                string? input = Console.ReadLine()?.Trim();
                if (input == "1") return ScheduleType.Once;
                if (input == "2") return ScheduleType.Recurring;

                Console.Write("   Enter 1 or 2: ");
            }
        }

        private static bool GetActivate()
        {
            while (true)
            {
                string? input = Console.ReadLine()?.Trim().ToUpper();
                if (input == "Y" || input == "YES") return true;
                if (input == "N" || input == "NOT") return false;

                Console.Write("   Enter Y or N: ");
            }
        }

        static OccursType GetOccursType()
        {
            while (true)
            {
                string input = Console.ReadLine() ?? "";
                switch (input) {
                    case "1":
                        return OccursType.Daily;
                }
                Console.WriteLine("   Invalid selection. Please select one of the available values (1).");
            }
        }



        private static int GetPositiveInteger()
        {
            while (true)
            {
                if (int.TryParse(Console.ReadLine(), out int numero) && numero > 0)
                    return numero;

                Console.Write("   It must be a number greater than 0: ");
            }
        }

        #endregion AUXILIARY METHODS 

    }

}
