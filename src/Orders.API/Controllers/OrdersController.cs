using Microsoft.AspNetCore.Mvc;
using Orders.API.DTOs;
using Orders.API.Exceptions;
using Orders.API.Services;
using Orders.API.Swagger;

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
        [ErrorExample(ErrorCodes.ORD_007, "Error interno al procesar la orden.")]
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
        [ErrorExample(ErrorCodes.ORD_001, "Orden no encontrada.")]
        [ErrorExample(ErrorCodes.ORD_007, "Error interno al procesar la orden.")]
        [ProducesResponseType(typeof(OrderResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound, "application/problem+json")]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError, "application/problem+json")]
        public async Task<ActionResult<OrderResponse>> GetById(string id)
        {
            return Ok(await service.ObtenerPorId(id));
        }

        /// <summary>
        /// Crea una orden nueva en estado Pendiente.
        /// </summary>
        /// <remarks>
        /// Valida que el usuario exista en Users.API y que cada producto exista y tenga stock en
        /// Products.API. El precio unitario se toma de Products.API en ese momento y el total se
        /// calcula solo. Si un producto aparece en dos items, se suman las cantidades.
        /// </remarks>
        /// <param name="request">Usuario y productos de la orden.</param>
        /// <param name="cancellationToken">Se cancela si el cliente corta el request.</param>
        /// <response code="201">Orden creada. El header Location apunta a GET /api/orders/{id}.</response>
        /// <response code="400">Faltan datos, la lista de items está vacía o una cantidad es menor o igual a 0 (ORD-002).</response>
        /// <response code="404">El usuario (ORD-003) o algún producto (ORD-004) no existe.</response>
        /// <response code="409">Conflicto de estado (ORD-006). La creación no lo produce: figura porque la consigna lo lista para este endpoint.</response>
        /// <response code="422">Stock insuficiente para algún producto (ORD-005).</response>
        /// <response code="500">Error interno, o Users.API o Products.API no responden (ORD-007).</response>
        [HttpPost]
        [ErrorExample(ErrorCodes.ORD_002, "La orden debe tener al menos un item; La cantidad de cada item debe ser mayor a 0.")]
        [ErrorExample(ErrorCodes.ORD_003, "Usuario no encontrado al crear la orden.")]
        [ErrorExample(ErrorCodes.ORD_004, "Producto no encontrado al crear la orden: 3fa85f64-5717-4562-b3fc-2c963f66afa6.")]
        [ErrorExample(ErrorCodes.ORD_005, "Stock insuficiente para 'Notebook Dell XPS 15'. Disponible: 2, solicitado: 5.")]
        [ErrorExample(ErrorCodes.ORD_006, "El estado de la orden no puede ser modificado.")]
        [ErrorExample(ErrorCodes.ORD_007, "Error interno al procesar la orden.")]
        [Consumes("application/json")]
        [ProducesResponseType(typeof(OrderResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest, "application/problem+json")]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound, "application/problem+json")]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict, "application/problem+json")]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status422UnprocessableEntity, "application/problem+json")]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status500InternalServerError, "application/problem+json")]
        public async Task<ActionResult<OrderResponse>> Create([FromBody] CreateOrderRequest request, CancellationToken cancellationToken)
        {
            var orden = await service.CrearOrden(request, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = orden.Id }, orden);
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
        [ErrorExample(ErrorCodes.ORD_002, "El estado 'Volando' no es válido. Valores posibles: Pendiente, Confirmada, Enviada, Entregada, Cancelada.")]
        [ErrorExample(ErrorCodes.ORD_001, "Orden no encontrada.")]
        [ErrorExample(ErrorCodes.ORD_006, "Una orden en estado 'Entregada' no puede volver a 'Pendiente'.")]
        [ErrorExample(ErrorCodes.ORD_007, "Error interno al procesar la orden.")]
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
