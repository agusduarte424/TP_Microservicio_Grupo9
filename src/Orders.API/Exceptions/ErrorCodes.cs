namespace Orders.API.Exceptions
{
    /// <summary>
    /// Datos fijos de un código del catálogo: status HTTP, detail genérico y errorMessage sugerido.
    /// </summary>
    public record ErrorDefinition(int Status, string Detail, string Message);

    /// <summary>
    /// Catálogo de errores de Orders.API (sección 4.3 de la consigna).
    /// Los códigos se usan siempre a través de estas constantes, nunca escritos a mano.
    /// </summary>
    public static class ErrorCodes
    {
        public const string ORD_001 = "ORD-001";
        public const string ORD_002 = "ORD-002";
        public const string ORD_003 = "ORD-003";
        public const string ORD_004 = "ORD-004";
        public const string ORD_005 = "ORD-005";
        public const string ORD_006 = "ORD-006";
        public const string ORD_007 = "ORD-007";

        /// <summary>Status, detail y mensaje de cada código, tal como los define la consigna.</summary>
        public static readonly IReadOnlyDictionary<string, ErrorDefinition> Catalogo =
            new Dictionary<string, ErrorDefinition>
            {
                [ORD_001] = new(StatusCodes.Status404NotFound,
                    "El recurso solicitado no fue encontrado.", "Orden no encontrada."),
                [ORD_002] = new(StatusCodes.Status400BadRequest,
                    "La solicitud contiene datos inválidos.", "Los datos de la orden son inválidos."),
                [ORD_003] = new(StatusCodes.Status404NotFound,
                    "El recurso solicitado no fue encontrado.", "Usuario no encontrado al crear la orden."),
                [ORD_004] = new(StatusCodes.Status404NotFound,
                    "El recurso solicitado no fue encontrado.", "Producto no encontrado al crear la orden."),
                [ORD_005] = new(StatusCodes.Status422UnprocessableEntity,
                    "No se puede procesar la solicitud.", "Stock insuficiente para uno o más productos."),
                [ORD_006] = new(StatusCodes.Status409Conflict,
                    "No se puede modificar el estado.", "El estado de la orden no puede ser modificado."),
                [ORD_007] = new(StatusCodes.Status500InternalServerError,
                    "Ocurrió un error inesperado.", "Error interno al procesar la orden.")
            };
    }
}
