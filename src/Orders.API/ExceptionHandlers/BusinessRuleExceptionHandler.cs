using Microsoft.AspNetCore.Diagnostics;
using Orders.API.Exceptions;

namespace Orders.API.ExceptionHandlers
{
    /// <summary>
    /// Convierte una BusinessRuleException en su status del catálogo:
    /// 422 para ORD-005 (stock insuficiente) y 409 para ORD-006 (transición inválida).
    /// </summary>
    public class BusinessRuleExceptionHandler(ILogger<BusinessRuleExceptionHandler> logger) : IExceptionHandler
    {
        public async ValueTask<bool> TryHandleAsync(
            HttpContext context, Exception exception, CancellationToken cancellationToken)
        {
            if (exception is not BusinessRuleException ex) return false;

            logger.LogWarning("Error de negocio {ErrorCode}: {ErrorMessage}", ex.ErrorCode, ex.Message);

            var error = ErrorResponseWriter.Build(context, ex.ErrorCode, ex.Message);
            await ErrorResponseWriter.WriteAsync(context, error, cancellationToken);
            return true;
        }
    }
}
