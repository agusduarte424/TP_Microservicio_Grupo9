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

        /// <summary>
        /// Cambia el estado de una orden respetando la máquina de estados.
        /// ORD-002 si el estado no existe, ORD-001 si la orden no existe,
        /// ORD-006 si la transición no está permitida.
        /// </summary>
        public async Task<OrderStatusResponse> ActualizarEstado(string id, UpdateOrderStatusRequest request)
        {
            // Primero el dato recibido: un estado que no existe es un error del request (400).
            var nuevo = EstadosOrden.Normalizar(request.Estado)
                ?? throw new ValidationException(ErrorCodes.ORD_002,
                    $"El estado '{request.Estado}' no es válido. Valores posibles: {string.Join(", ", EstadosOrden.Todos)}.");

            var orden = await BuscarOrden(id);

            // El estado existe pero no se puede llegar a él desde el actual: conflicto (409).
            if (!EstadosOrden.PuedeCambiar(orden.Estado, nuevo))
                throw new BusinessRuleException(ErrorCodes.ORD_006, MensajeTransicionInvalida(orden.Estado, nuevo));

            var fechaActualizacion = DateTime.UtcNow;
            await repository.ActualizarEstado(orden.Id, nuevo, fechaActualizacion);

            return new OrderStatusResponse
            {
                Id = orden.Id,
                Estado = nuevo,
                FechaActualizacion = fechaActualizacion
            };
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

        private static string MensajeTransicionInvalida(string actual, string nuevo)
        {
            if (actual == nuevo)
                return $"La orden ya está en estado '{actual}'.";

            // Mismo formato que el ejemplo de la consigna para los retrocesos.
            var retrocede = EstadosOrden.Todos.ToList().IndexOf(nuevo) < EstadosOrden.Todos.ToList().IndexOf(actual);
            return retrocede
                ? $"Una orden en estado '{actual}' no puede volver a '{nuevo}'."
                : $"Una orden en estado '{actual}' no puede pasar a '{nuevo}'.";
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
