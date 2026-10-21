namespace Orders.API.DTOs
{
    /// <summary>
    /// Contrato de todas las respuestas de error (4xx y 5xx), según la sección 3 de la consigna.
    /// </summary>
    public class ErrorResponse
    {
        /// <summary>Referencia al RFC que describe el status HTTP.</summary>
        /// <example>https://tools.ietf.org/html/rfc7231#section-6.5.4</example>
        public string Type { get; set; } = string.Empty;

        /// <summary>Nombre del status HTTP.</summary>
        /// <example>Not Found</example>
        public string Title { get; set; } = string.Empty;

        /// <summary>Status HTTP.</summary>
        /// <example>404</example>
        public int Status { get; set; }

        /// <summary>Descripción genérica del tipo de error.</summary>
        /// <example>El recurso solicitado no fue encontrado.</example>
        public string Detail { get; set; } = string.Empty;

        /// <summary>Ruta del request que falló.</summary>
        /// <example>/api/orders/f1e2d3c4-0000-0000-0000-aabbccddeeff</example>
        public string Instance { get; set; } = string.Empty;

        /// <summary>Código del catálogo de Orders.</summary>
        /// <example>ORD-001</example>
        public string ErrorCode { get; set; } = string.Empty;

        /// <summary>Mensaje del error. En errores de validación lista todos los problemas separados por punto y coma.</summary>
        /// <example>Orden no encontrada.</example>
        public string ErrorMessage { get; set; } = string.Empty;

        /// <summary>Correlation ID del request, para buscarlo en los logs.</summary>
        /// <example>3a275d6f-adca-4af3-8a89-808b33e69614</example>
        public string CorrelationId { get; set; } = string.Empty;
    }
}
