namespace Orders.API.Swagger
{
    /// <summary>
    /// Declara un ejemplo de respuesta de error para Swagger, con un código del catálogo.
    /// Se puede repetir: por ejemplo, el 404 del POST tiene un ejemplo para ORD-003 y otro para ORD-004.
    /// </summary>
    /// <example>[ErrorExample(ErrorCodes.ORD_001, "Orden no encontrada.")]</example>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class ErrorExampleAttribute(string errorCode, string errorMessage) : Attribute
    {
        /// <summary>Código del catálogo. El status HTTP sale del catálogo.</summary>
        public string ErrorCode { get; } = errorCode;

        /// <summary>errorMessage que se muestra en el ejemplo.</summary>
        public string ErrorMessage { get; } = errorMessage;
    }
}
