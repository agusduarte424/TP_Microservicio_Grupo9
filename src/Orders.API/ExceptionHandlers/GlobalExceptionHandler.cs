using Microsoft.AspNetCore.Diagnostics;
using Orders.API.Exceptions;

namespace Orders.API.ExceptionHandlers
{
    /// <summary>
    /// Red de seguridad: atrapa cualquier error no previsto (una falla de la base, Users.API o
    /// Products.API caídos o respondiendo 5xx) y responde 500 con ORD-007.
    /// Va registrado último, después de los handlers específicos.
    /// </summary>
    public class GlobalExceptionHandler(
        ILogger<GlobalExceptionHandler> logger,
        IHostEnvironment environment) : IExceptionHandler
    {
        public async ValueTask<bool> TryHandleAsync(
            HttpContext context, Exception exception, CancellationToken cancellationToken)
        {
            // Error inesperado: Error, con la excepción completa. El stack trace queda solo en el log.
            logger.LogError(exception, "Error inesperado {ErrorCode}", ErrorCodes.ORD_007);

            // En Development se agrega el motivo para depurar; en Production, solo el texto genérico.
            // Nunca se devuelve el stack trace.
            var detail = environment.IsDevelopment()
                ? $"{ErrorCodes.Catalogo[ErrorCodes.ORD_007].Detail} Motivo: {exception.Message}"
                : null;

            var error = ErrorResponseWriter.Build(context, ErrorCodes.ORD_007,
                ErrorCodes.Catalogo[ErrorCodes.ORD_007].Message, detail);
            await ErrorResponseWriter.WriteAsync(context, error, cancellationToken);
            return true;
        }
    }
}
