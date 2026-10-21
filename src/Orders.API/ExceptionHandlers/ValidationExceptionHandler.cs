using Microsoft.AspNetCore.Diagnostics;
using Orders.API.Exceptions;

namespace Orders.API.ExceptionHandlers
{
    /// <summary>
    /// Convierte una ValidationException en un 400 con ORD-002.
    /// </summary>
    public class ValidationExceptionHandler(ILogger<ValidationExceptionHandler> logger) : IExceptionHandler
    {
        public async ValueTask<bool> TryHandleAsync(
            HttpContext context, Exception exception, CancellationToken cancellationToken)
        {
            if (exception is not ValidationException ex) return false;

            logger.LogWarning("Error de validación {ErrorCode}: {ErrorMessage}", ex.ErrorCode, ex.Message);

            var error = ErrorResponseWriter.Build(context, ex.ErrorCode, ex.Message);
            await ErrorResponseWriter.WriteAsync(context, error, cancellationToken);
            return true;
        }
    }
}
