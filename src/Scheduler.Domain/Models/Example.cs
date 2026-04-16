
namespace ResultPatternExample
{
    /// <summary>
    /// Estructura genérica para representar un resultado exitoso o un error.
    /// </summary>
    public readonly struct Result<T>
    {
        public bool IsSuccess { get; }
        public T Value { get; }
        public string Error { get; }

        // Constructor privado para controlar la creación
        private Result(T value, bool isSuccess, string error)
        {
            IsSuccess = isSuccess;
            Value = value;
            Error = error;
        }

        /// <summary>
        /// Crea un resultado exitoso.
        /// </summary>
        public static Result<T> Success(T value) =>
            new Result<T>(value, true, string.Empty);

        /// <summary>
        /// Crea un resultado fallido.
        /// </summary>
        public static Result<T> Failure(string error)
        {
            if (string.IsNullOrWhiteSpace(error))
                throw new ArgumentException("El mensaje de error no puede estar vacío.", nameof(error));

            return new Result<T>(default!, false, error);
        }

        /// <summary>
        /// Devuelve una representación en texto del resultado.
        /// </summary>
        public override string ToString() =>
            IsSuccess ? $"Éxito: {Value}" : $"Error: {Error}";
    }

    class Program
    {
        // Ejemplo de función que usa el patrón Result
        static Result<int> Dividir(int a, int b)
        {
            if (b == 0)
                return Result<int>.Failure("No se puede dividir entre cero.");

            return Result<int>.Success(a / b);
        }

        static void Main()
        {
            var resultado1 = Dividir(10, 2);
            var resultado2 = Dividir(5, 0);

            // Manejo explícito de resultados
            if (resultado1.IsSuccess)
                Console.WriteLine($"Resultado: {resultado1.Value}");
            else
                Console.WriteLine($"Error: {resultado1.Error}");

            if (resultado2.IsSuccess)
                Console.WriteLine($"Resultado: {resultado2.Value}");
            else
                Console.WriteLine($"Error: {resultado2.Error}");
        }
    }
}
