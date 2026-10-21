namespace Orders.API.Models
{
    /// <summary>
    /// Un producto dentro de una orden.
    /// </summary>
    public class OrderItem
    {
        /// <summary>Producto comprado (existe en Products.API).</summary>
        public Guid ProductoId { get; set; }

        /// <summary>Unidades compradas. Mayor a 0.</summary>
        public int Cantidad { get; set; }

        /// <summary>
        /// Precio del producto en el momento de crear la orden. Queda fijo:
        /// si después el producto cambia de precio, la orden no cambia.
        /// </summary>
        public decimal PrecioUnitario { get; set; }
    }
}
