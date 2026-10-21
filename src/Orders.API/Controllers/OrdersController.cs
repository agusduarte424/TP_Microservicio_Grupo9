using Microsoft.AspNetCore.Mvc;
using Orders.API.DTOs;
using Orders.API.Services;

namespace Orders.API.Controllers
{
    /// <summary>
    /// Órdenes de compra: consulta, creación y cambio de estado.
    /// </summary>
    /// <remarks>
    /// El controller no tiene lógica ni try/catch: le pasa cada request al OrderService,
    /// y si algo falla el Service lanza una excepción que convierte en respuesta su IExceptionHandler.
    /// </remarks>
    [ApiController]
    [Route("api/orders")]
    [Tags("Orders")]
    [Produces("application/json")]
    public class OrdersController(OrderService service) : ControllerBase
    {
        /// <summary>
        /// Lista las órdenes, opcionalmente filtradas por usuario.
        /// </summary>
        /// <param name="usuarioId">Si se informa, devuelve solo las órdenes de ese usuario.</param>
        /// <response code="200">Lista de órdenes (vacía si no hay ninguna).</response>
        /// <response code="500">Error interno (ORD-007).</response>
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<OrderResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError, "application/problem+json")]
        public async Task<ActionResult<IEnumerable<OrderResponse>>> GetAll([FromQuery] string? usuarioId)
        {
            return Ok(await service.ObtenerTodas(usuarioId));
        }

        /// <summary>
        /// Devuelve el detalle de una orden.
        /// </summary>
        /// <param name="id">Identificador de la orden.</param>
        /// <response code="200">La orden con sus items.</response>
        /// <response code="404">La orden no existe (ORD-001).</response>
        /// <response code="500">Error interno (ORD-007).</response>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(OrderResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound, "application/problem+json")]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError, "application/problem+json")]
        public async Task<ActionResult<OrderResponse>> GetById(string id)
        {
            return Ok(await service.ObtenerPorId(id));
        }

        /// <summary>
        /// Cambia el estado de una orden.
        /// </summary>
        /// <remarks>
        /// Cambios permitidos:
        ///
        ///     Pendiente  → Confirmada o Cancelada
        ///     Confirmada → Enviada o Cancelada
        ///     Enviada    → Entregada
        ///     Entregada y Cancelada son estados finales.
        ///
        /// </remarks>
        /// <param name="id">Identificador de la orden.</param>
        /// <param name="request">Nuevo estado.</param>
        /// <response code="200">Estado actualizado.</response>
        /// <response code="400">El estado no existe o falta en el body (ORD-002).</response>
        /// <response code="404">La orden no existe (ORD-001).</response>
        /// <response code="409">La transición no está permitida (ORD-006).</response>
        /// <response code="500">Error interno (ORD-007).</response>
        [HttpPut("{id}/status")]
        [Consumes("application/json")]
        [ProducesResponseType(typeof(OrderStatusResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest, "application/problem+json")]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound, "application/problem+json")]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict, "application/problem+json")]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError, "application/problem+json")]
        public async Task<ActionResult<OrderStatusResponse>> UpdateStatus(string id, [FromBody] UpdateOrderStatusRequest request)
        {
            return Ok(await service.ActualizarEstado(id, request));
        }
    }
}
