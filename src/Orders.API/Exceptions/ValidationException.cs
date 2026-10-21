namespace Orders.API.Exceptions
{
    /// <summary>
    /// Los datos recibidos son inválidos (400, ORD-002).
    /// </summary>
    public class ValidationException(string errorCode, string? message = null)
        : DomainException(errorCode, message);
}
