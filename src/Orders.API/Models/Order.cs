namespace Orders.API.Models
{
    /// <summary>
    /// Orden de compra de un usuario. Es lo que se guarda en la base de Orders.
    /// </summary>
    public class Order
    {
        /// <summary>Identificador único de la orden. Lo genera el sistema al crearla.</summary>
        public Guid Id { get; set; }

        /// <summary>Usuario que realizó la orden (existe en Users.API).</summary>
        public Guid UsuarioId { get; set; }

        /// <summary>Productos incluidos en la orden.</summary>
        public List<OrderItem> Items { get; set; } = [];

        /// <summary>Suma de cantidad × precio unitario de cada item. Se calcula al crear la orden.</summary>
        public decimal Total { get; set; }

        /// <summary>Estado actual: Pendiente, Confirmada, Enviada, Entregada o Cancelada.</summary>
        public string Estado { get; set; } = EstadosOrden.Pendiente;

        /// <summary>Fecha de creación, asignada automáticamente.</summary>
        public DateTime FechaCreacion { get; set; }

        /// <summary>
        /// Fecha del último cambio. Al crear la orden es igual a FechaCreacion; se actualiza
        /// en cada cambio de estado y es lo que devuelve PUT /status.
        /// </summary>
        public DateTime FechaActualizacion { get; set; }
    }
}
