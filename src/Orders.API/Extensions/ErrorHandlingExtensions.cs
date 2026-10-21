// Bloque de manejo de errores: IExceptionHandler por tipo de excepción y uno genérico,
// más la respuesta para datos inválidos que detecta ASP.NET Core antes de llegar al controller.
// Program.cs lo usa con dos líneas:
//   builder.Services.AddErrorHandling();
//   app.UseExceptionHandler();      -> después de app.UseRequestLogging()
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Orders.API.ExceptionHandlers;
using Orders.API.Exceptions;

namespace Orders.API.Extensions
{
    public static class ErrorHandlingExtensions
    {
        /// <summary>
        /// Registra los handlers en orden de especificidad: ASP.NET Core los prueba en el orden
        /// en que se registran y usa el primero que devuelve true. El genérico va último porque
        /// acepta cualquier excepción; si fuera primero, los específicos nunca se ejecutarían.
        /// </summary>
        public static IServiceCollection AddErrorHandling(this IServiceCollection services)
        {
            services.AddExceptionHandler<NotFoundExceptionHandler>();
            services.AddExceptionHandler<BusinessRuleExceptionHandler>();
            services.AddExceptionHandler<ValidationExceptionHandler>();
            services.AddExceptionHandler<GlobalExceptionHandler>();
            services.AddProblemDetails();

            // Con [ApiController], si el body no es un JSON válido, un GUID de la ruta no se puede
            // leer o falla una Data Annotation, ASP.NET Core responde 400 solo, sin pasar por el
            // controller. Acá se reemplaza esa respuesta por el contrato de la consigna (ORD-002).
            services.Configure<ApiBehaviorOptions>(options =>
            {
                options.InvalidModelStateResponseFactory = context =>
                {
                    var mensajes = context.ModelState
                        .Where(e => e.Value?.Errors.Count > 0)
                        .SelectMany(e => e.Value!.Errors.Select(error => Traducir(e.Key, error)))
                        .Distinct()
                        .ToList();

                    // Si el JSON no se pudo leer, ASP.NET Core además avisa que "falta el body".
                    // Ese segundo aviso confunde, así que se deja solo el del JSON.
                    if (mensajes.Contains(JsonInvalido))
                        mensajes = [JsonInvalido];

                    // La consigna pide listar todos los problemas separados por punto y coma:
                    // "El estado es obligatorio; La cantidad debe ser mayor a 0."
                    var errorMessage = string.Join("; ", mensajes.Select(m => m.TrimEnd('.'))) + ".";

                    var logger = context.HttpContext.RequestServices
                        .GetRequiredService<ILoggerFactory>().CreateLogger("Validacion");
                    logger.LogWarning("Error de validación {ErrorCode}: {ErrorMessage}",
                        ErrorCodes.ORD_002, errorMessage);

                    var error = ErrorResponseWriter.Build(context.HttpContext, ErrorCodes.ORD_002, errorMessage);
                    return new ObjectResult(error)
                    {
                        StatusCode = error.Status,
                        ContentTypes = { "application/problem+json" }
                    };
                };
            });

            return services;
        }

        private const string JsonInvalido =
            "El cuerpo de la solicitud no es un JSON válido o tiene un campo con formato incorrecto.";

        // Los mensajes de las Data Annotations ya están en español (los escribimos en los DTOs).
        // Los que genera ASP.NET Core al leer el request vienen en inglés y con detalles técnicos,
        // así que se reemplazan por uno claro.
        private static string Traducir(string campo, ModelError error)
        {
            // Body que no se puede leer como JSON (llave sin cerrar, texto en un número, etc.).
            if (error.Exception is not null || campo.StartsWith('$'))
                return JsonInvalido;

            // Body vacío.
            if (campo is "request" or "" && error.ErrorMessage.Contains("required", StringComparison.OrdinalIgnoreCase))
                return "El cuerpo de la solicitud es obligatorio.";

            // Parámetro de la ruta o de la query con formato inválido (por ejemplo un id que no es GUID).
            if (error.ErrorMessage.Contains("is not valid", StringComparison.OrdinalIgnoreCase))
                return $"El valor de '{campo}' no tiene un formato válido.";

            return error.ErrorMessage;
        }
    }
}
