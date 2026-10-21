// Excepciones de dominio: el Service las lanza cuando algo no se puede hacer,
// y el IExceptionHandler de cada tipo arma la respuesta HTTP. No hay try/catch en el controller.
//
// Uso:
//   throw new NotFoundException(ErrorCodes.ORD_001);                     -> mensaje del catálogo
//   throw new BusinessRuleException(ErrorCodes.ORD_005, "Stock ...");    -> mensaje propio
namespace Orders.API.Exceptions
{
    /// <summary>
    /// Base de las excepciones de negocio: todas llevan un código del catálogo.
    /// Si no se pasa mensaje, se usa el errorMessage sugerido del catálogo.
    /// </summary>
    public abstract class DomainException(string errorCode, string? message)
        : Exception(message ?? ErrorCodes.Catalogo[errorCode].Message)
    {
        /// <summary>Código del catálogo (por ejemplo "ORD-001").</summary>
        public string ErrorCode { get; } = errorCode;
    }
}
