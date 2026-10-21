namespace Orders.API.DTOs
{
    /// <summary>
    /// Orden tal como la devuelven GET /api/orders, GET /api/orders/{id} y POST /api/orders.
    /// </summary>
    public class OrderResponse
    {
        /// <summary>Identificador único de la orden.</summary>
        /// <example>f1e2d3c4-0000-0000-0000-aabbccddeeff</example>
        public Guid Id { get; set; }

        /// <summary>Usuario que realizó la orden.</summary>
        /// <example>a1b2c3d4-0000-0000-0000-111122223333</example>
        public Guid UsuarioId { get; set; }

        /// <summary>Productos de la orden, con el precio que tenían al crearla.</summary>
        public List<OrderItemResponse> Items { get; set; } = [];

        /// <summary>Suma de cantidad × precio unitario de cada item.</summary>
        /// <example>3000.00</example>
        public decimal Total { get; set; }

        /// <summary>Estado actual: Pendiente, Confirmada, Enviada, Entregada o Cancelada.</summary>
        /// <example>Pendiente</example>
        public string Estado { get; set; } = string.Empty;

        /// <summary>Fecha de creación (UTC).</summary>
        /// <example>2024-03-10T11:00:00Z</example>
        public DateTime FechaCreacion { get; set; }
    }

    /// <summary>
    /// Un producto dentro de una orden.
    /// </summary>
    public class OrderItemResponse
    {
        /// <summary>Producto comprado.</summary>
        /// <example>3fa85f64-5717-4562-b3fc-2c963f66afa6</example>
        public Guid ProductoId { get; set; }

        /// <summary>Unidades compradas.</summary>
        /// <example>2</example>
        public int Cantidad { get; set; }

        /// <summary>Precio del producto al momento de crear la orden.</summary>
        /// <example>1500.00</example>
        public decimal PrecioUnitario { get; set; }
    }
}
