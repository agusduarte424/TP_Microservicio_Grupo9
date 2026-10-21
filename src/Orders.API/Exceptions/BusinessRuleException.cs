namespace Orders.API.Exceptions
{
    /// <summary>
    /// Se viola una regla de negocio. El status sale del catálogo:
    /// ORD-005 (stock insuficiente) es 422 y ORD-006 (transición de estado inválida) es 409.
    /// </summary>
    public class BusinessRuleException(string errorCode, string? message = null)
        : DomainException(errorCode, message);
}
