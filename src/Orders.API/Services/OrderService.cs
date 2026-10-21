// OrderService es donde vive la lógica de negocio de Orders.
// El controller le pasa cada request; el Service valida, lanza excepciones de dominio si algo
// no se puede hacer, usa IOrderRepository para leer y guardar, y los clientes de Clients/
// para consultar a Users.API y Products.API.
using Orders.API.Clients;
using Orders.API.Data;
using Orders.API.DTOs;
using Orders.API.Exceptions;
using Orders.API.Models;

namespace Orders.API.Services
{
    /// <summary>
    /// Lógica de negocio de las órdenes.
    /// </summary>
    public class OrderService(
        IOrderRepository repository,
        IUsersApiClient usersApi,
        IProductsApiClient productsApi,
        ILogger<OrderService> logger)
    {
        /// <summary>
        /// Crea una orden en estado Pendiente. Pasos:
        /// 1) valida los datos (ORD-002); 2) valida el usuario en Users.API (ORD-003);
        /// 3) valida cada producto en Products.API (ORD-004) y su stock (ORD-005);
        /// 4) toma el precio actual de cada producto, calcula el total y guarda.
        /// Si Users.API o Products.API fallan o no responden, la excepción llega al
        /// GlobalExceptionHandler y se responde 500 con ORD-007.
        /// </summary>
        public async Task<OrderResponse> CrearOrden(CreateOrderRequest request, CancellationToken cancellationToken = default)
        {
            ValidarDatos(request);
            var usuarioId = request.UsuarioId!.Value;

            if (!await usersApi.ExisteUsuario(usuarioId, cancellationToken))
                throw new NotFoundException(ErrorCodes.ORD_003);

            // Si el mismo producto viene en dos items, se suman las cantidades:
            // el stock se valida contra el total pedido de ese producto.
            var pedidos = request.Items!
                .GroupBy(i => i.ProductoId!.Value)
                .Select(g => (ProductoId: g.Key, Cantidad: g.Sum(i => i.Cantidad)))
                .ToList();

            var items = new List<OrderItem>();
            foreach (var (productoId, cantidad) in pedidos)
            {
                var producto = await productsApi.ObtenerProducto(productoId, cancellationToken)
                    ?? throw new NotFoundException(ErrorCodes.ORD_004,
                        $"Producto no encontrado al crear la orden: {productoId}.");

                if (producto.Stock < cantidad)
                    throw new BusinessRuleException(ErrorCodes.ORD_005,
                        $"Stock insuficiente para '{producto.Nombre}'. Disponible: {producto.Stock}, solicitado: {cantidad}.");

                // El precio se toma de Products.API en este momento y queda fijo en la orden.
                items.Add(new OrderItem
                {
                    ProductoId = productoId,
                    Cantidad = cantidad,
                    PrecioUnitario = producto.Precio
                });
            }

            var ahora = DateTime.UtcNow;
            var orden = new Order
            {
                Id = Guid.NewGuid(),
                UsuarioId = usuarioId,
                Items = items,
                Total = items.Sum(i => i.Cantidad * i.PrecioUnitario),
                Estado = EstadosOrden.Pendiente,
                FechaCreacion = ahora,
                FechaActualizacion = ahora
            };

            await repository.Guardar(orden);
            logger.LogInformation("Orden {OrdenId} creada para el usuario {UsuarioId} por {Total}",
                orden.Id, orden.UsuarioId, orden.Total);

            return MapearAResponse(orden);
        }

        // Validaciones de ORD-002 que no dependen de otros servicios. Las Data Annotations del DTO
        // ya cubren campos faltantes y cantidades <= 0; acá se repiten por si el Service se usa
        // sin pasar por el controller, y se agrega lo que las anotaciones no ven (GUID vacío).
        // Se juntan todos los problemas y se informan juntos, separados por punto y coma.
        private static void ValidarDatos(CreateOrderRequest request)
        {
            var problemas = new List<string>();

            if (request.UsuarioId is null || request.UsuarioId == Guid.Empty)
                problemas.Add("El usuarioId es obligatorio");

            if (request.Items is null || request.Items.Count == 0)
            {
                problemas.Add("La orden debe tener al menos un item");
            }
            else
            {
                if (request.Items.Any(i => i.ProductoId is null || i.ProductoId == Guid.Empty))
                    problemas.Add("El productoId de cada item es obligatorio");

                if (request.Items.Any(i => i.Cantidad <= 0))
                    problemas.Add("La cantidad de cada item debe ser mayor a 0");
            }

            if (problemas.Count > 0)
                throw new ValidationException(ErrorCodes.ORD_002, string.Join("; ", problemas) + ".");
        }

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
