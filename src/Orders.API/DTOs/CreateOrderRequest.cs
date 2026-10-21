using System.ComponentModel.DataAnnotations;

namespace Orders.API.DTOs
{
    /// <summary>
    /// Body de POST /api/orders. Solo trae quién compra y qué: el precio, el total,
    /// el estado y las fechas los pone el sistema.
    /// </summary>
    public class CreateOrderRequest
    {
        /// <summary>Usuario que realiza la orden. Tiene que existir en Users.API.</summary>
        /// <example>a1b2c3d4-0000-0000-0000-111122223333</example>
        [Required(ErrorMessage = "El usuarioId es obligatorio.")]
        public Guid? UsuarioId { get; set; }

        /// <summary>Productos a comprar. Al menos uno.</summary>
        [Required(ErrorMessage = "La orden debe tener al menos un item.")]
        [MinLength(1, ErrorMessage = "La orden debe tener al menos un item.")]
        public List<CreateOrderItemRequest>? Items { get; set; }
    }

    /// <summary>
    /// Un producto a comprar.
    /// </summary>
    public class CreateOrderItemRequest
    {
        /// <summary>Producto a comprar. Tiene que existir en Products.API.</summary>
        /// <example>3fa85f64-5717-4562-b3fc-2c963f66afa6</example>
        [Required(ErrorMessage = "El productoId de cada item es obligatorio.")]
        public Guid? ProductoId { get; set; }

        /// <summary>Unidades a comprar. Mayor a 0.</summary>
        /// <example>2</example>
        [Range(1, int.MaxValue, ErrorMessage = "La cantidad de cada item debe ser mayor a 0.")]
        public int Cantidad { get; set; }
    }
}
