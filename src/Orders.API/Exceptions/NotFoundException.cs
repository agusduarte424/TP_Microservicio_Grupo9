namespace Orders.API.Exceptions
{
    /// <summary>
    /// El recurso pedido no existe (404): una orden (ORD-001), o el usuario (ORD-003)
    /// o un producto (ORD-004) al crear la orden.
    /// </summary>
    public class NotFoundException(string errorCode, string? message = null)
        : DomainException(errorCode, message);
}
