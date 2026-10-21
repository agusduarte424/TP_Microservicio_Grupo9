using Microsoft.AspNetCore.Diagnostics;
using Orders.API.Exceptions;

namespace Orders.API.ExceptionHandlers
{
    /// <summary>
    /// Convierte una NotFoundException en un 404 con su errorCode (ORD-001, ORD-003 u ORD-004).
    /// </summary>
    public class NotFoundExceptionHandler(ILogger<NotFoundExceptionHandler> logger) : IExceptionHandler
    {
        public async ValueTask<bool> TryHandleAsync(
            HttpContext context, Exception exception, CancellationToken cancellationToken)
        {
            // Si no es de este tipo, devuelve false y ASP.NET Core prueba el siguiente handler.
            if (exception is not NotFoundException ex) return false;

            // Error de negocio: Warning, con el código para poder buscarlo en los logs.
            logger.LogWarning("Error de negocio {ErrorCode}: {ErrorMessage}", ex.ErrorCode, ex.Message);

            var error = ErrorResponseWriter.Build(context, ex.ErrorCode, ex.Message);
            await ErrorResponseWriter.WriteAsync(context, error, cancellationToken);
            return true;
        }
    }
}
