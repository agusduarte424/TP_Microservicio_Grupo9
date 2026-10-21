// OrderService es donde vive la lógica de negocio de Orders.
// El controller le pasa cada request; el Service valida, lanza excepciones de dominio si algo
// no se puede hacer y usa IOrderRepository para leer y guardar.
using Orders.API.Data;
using Orders.API.DTOs;
using Orders.API.Exceptions;
using Orders.API.Models;

namespace Orders.API.Services
{
    /// <summary>
    /// Lógica de negocio de las órdenes.
    /// </summary>
    public class OrderService(IOrderRepository repository)
    {
        /// <summary>
        /// Lista las órdenes. Si viene usuarioId, solo las de ese usuario.
        /// Un usuarioId que no es un GUID no puede tener órdenes: devuelve una lista vacía.
        /// </summary>
        public async Task<IEnumerable<OrderResponse>> ObtenerTodas(string? usuarioId)
        {
            if (string.IsNullOrWhiteSpace(usuarioId))
                return (await repository.ObtenerTodas(null)).Select(MapearAResponse);

            if (!Guid.TryParse(usuarioId, out var id))
                return [];

            return (await repository.ObtenerTodas(id)).Select(MapearAResponse);
        }

        /// <summary>
        /// Devuelve una orden. ORD-001 si no existe (o si el id ni siquiera es un GUID).
        /// </summary>
        public async Task<OrderResponse> ObtenerPorId(string id)
        {
            var orden = await BuscarOrden(id);
            return MapearAResponse(orden);
        }

        // Busca la orden por id o lanza ORD-001. Un id que no es GUID tampoco existe,
        // igual que el ejemplo de la consigna "GET /api/products/99" responde 404.
        private async Task<Order> BuscarOrden(string id)
        {
            if (!Guid.TryParse(id, out var guid))
                throw new NotFoundException(ErrorCodes.ORD_001);

            return await repository.ObtenerPorId(guid)
                ?? throw new NotFoundException(ErrorCodes.ORD_001);
        }

        // Convierte el modelo interno en lo que ve el cliente.
        private static OrderResponse MapearAResponse(Order orden) => new()
        {
            Id = orden.Id,
            UsuarioId = orden.UsuarioId,
            Items = orden.Items.Select(i => new OrderItemResponse
            {
                ProductoId = i.ProductoId,
                Cantidad = i.Cantidad,
                PrecioUnitario = i.PrecioUnitario
            }).ToList(),
            Total = orden.Total,
            Estado = orden.Estado,
            FechaCreacion = orden.FechaCreacion
        };
    }
}
