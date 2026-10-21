// Arma y escribe la respuesta de error con el contrato de la consigna.
// La usan todos los handlers y la respuesta automática de datos inválidos,
// así el formato es siempre el mismo.
using Orders.API.DTOs;
using Orders.API.Exceptions;
using Orders.API.Extensions;

namespace Orders.API.ExceptionHandlers
{
    public static class ErrorResponseWriter
    {
        // type y title de cada status, con los mismos RFC que usa la consigna en sus ejemplos.
        private static readonly Dictionary<int, (string Type, string Title)> Statuses = new()
        {
            [400] = ("https://tools.ietf.org/html/rfc7231#section-6.5.1", "Bad Request"),
            [404] = ("https://tools.ietf.org/html/rfc7231#section-6.5.4", "Not Found"),
            [409] = ("https://tools.ietf.org/html/rfc7231#section-6.5.9", "Conflict"),
            [422] = ("https://tools.ietf.org/html/rfc4918#section-11.2", "Unprocessable Entity"),
            [500] = ("https://tools.ietf.org/html/rfc7231#section-6.6.1", "Internal Server Error")
        };

        /// <summary>
        /// Construye la respuesta para un código del catálogo. El status y el detail salen del
        /// catálogo; <paramref name="errorMessage"/> es el mensaje concreto de este caso.
        /// </summary>
        public static ErrorResponse Build(HttpContext context, string errorCode, string errorMessage, string? detail = null)
        {
            var definicion = ErrorCodes.Catalogo[errorCode];
            var (type, title) = Statuses[definicion.Status];

            return new ErrorResponse
            {
                Type = type,
                Title = title,
                Status = definicion.Status,
                Detail = detail ?? definicion.Detail,
                Instance = context.Request.Path.Value ?? string.Empty,
                ErrorCode = errorCode,
                ErrorMessage = errorMessage,
                CorrelationId = context.GetCorrelationId()
            };
        }

        /// <summary>Escribe la respuesta como application/problem+json con su status.</summary>
        public static async Task WriteAsync(HttpContext context, ErrorResponse error, CancellationToken cancellationToken)
        {
            context.Response.StatusCode = error.Status;
            await context.Response.WriteAsJsonAsync(error, options: null,
                contentType: "application/problem+json", cancellationToken);
        }
    }
}
