using System.ComponentModel.DataAnnotations;

namespace Orders.API.DTOs
{
    /// <summary>
    /// Body de PUT /api/orders/{id}/status.
    /// </summary>
    public class UpdateOrderStatusRequest
    {
        /// <summary>Nuevo estado: Pendiente, Confirmada, Enviada, Entregada o Cancelada.</summary>
        /// <example>Confirmada</example>
        [Required(ErrorMessage = "El estado es obligatorio.")]
        public string? Estado { get; set; }
    }

    /// <summary>
    /// Respuesta de PUT /api/orders/{id}/status.
    /// </summary>
    public class OrderStatusResponse
    {
        /// <summary>Identificador de la orden.</summary>
        /// <example>f1e2d3c4-0000-0000-0000-aabbccddeeff</example>
        public Guid Id { get; set; }

        /// <summary>Estado nuevo de la orden.</summary>
        /// <example>Confirmada</example>
        public string Estado { get; set; } = string.Empty;

        /// <summary>Fecha del cambio de estado (UTC).</summary>
        /// <example>2024-03-10T12:00:00Z</example>
        public DateTime FechaActualizacion { get; set; }
    }
}
