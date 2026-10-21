namespace Products.API.Models
{
    /// <summary>
    /// Modelo que representa un producto dentro del sistema.
    /// </summary>
    public class Product
    {
        /// <summary>
        /// Identificador único del producto.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Nombre del producto.
        /// </summary>
        public string Nombre { get; set; } = string.Empty;

        /// <summary>
        /// Descripción del producto.
        /// </summary>
        public string? Descripcion { get; set; }

        /// <summary>
        /// Precio unitario del producto.
        /// </summary>
        public decimal Precio { get; set; }

        /// <summary>
        /// Cantidad disponible en stock.
        /// </summary>
        public int Stock { get; set; }

        /// <summary>
        /// Categoría a la que pertenece el producto.
        /// </summary>
        public string Categoria { get; set; } = string.Empty;

        /// <summary>
        /// Fecha de creación del producto.
        /// </summary>
        public DateTime FechaCreacion { get; set; }
    }
}
