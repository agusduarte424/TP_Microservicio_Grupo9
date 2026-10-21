// Bloque de Correlation ID (F3): un identificador por request que viaja entre servicios.
// Program.cs lo usa con dos líneas:
//   builder.Services.AddCorrelationId();   -> registra lo necesario para propagarlo
//   app.UseCorrelationId();                 -> primer middleware, antes de app.UseRequestLogging()
// Cada HttpClient que llama a otro servicio agrega .AddCorrelationIdPropagation().
// Para replicarlo en otro servicio: copiar este archivo y cambiar solo el namespace.
using Serilog.Context;

namespace Orders.API.Extensions
{
    public static class CorrelationIdExtensions
    {
        /// <summary>Nombre del header HTTP que transporta el Correlation ID.</summary>
        public const string HeaderName = "X-Correlation-Id";

        // Clave con la que se guarda en HttpContext.Items durante el request.
        private const string ItemKey = "CorrelationId";

        /// <summary>
        /// Registra el acceso al request actual y el handler que agrega el header
        /// en las llamadas HTTP salientes.
        /// </summary>
        public static IServiceCollection AddCorrelationId(this IServiceCollection services)
        {
            services.AddHttpContextAccessor();
            services.AddTransient<CorrelationIdDelegatingHandler>();
            return services;
        }

        /// <summary>
        /// Toma el X-Correlation-Id del request (si otro servicio lo mandó) o genera uno nuevo.
        /// Lo guarda en el request, lo devuelve en el header de la respuesta y lo agrega
        /// como campo CorrelationId a todos los logs que se escriben durante el request.
        /// </summary>
        public static WebApplication UseCorrelationId(this WebApplication app)
        {
            app.Use(async (context, next) =>
            {
                var recibido = context.Request.Headers[HeaderName].ToString();
                var correlationId = string.IsNullOrWhiteSpace(recibido)
                    ? Guid.NewGuid().ToString()
                    : recibido;

                context.Items[ItemKey] = correlationId;

                // El cliente también lo recibe, para poder reportarlo.
                context.Response.OnStarting(() =>
                {
                    context.Response.Headers[HeaderName] = correlationId;
                    return Task.CompletedTask;
                });

                using (LogContext.PushProperty("CorrelationId", correlationId))
                {
                    await next(context);
                }
            });

            return app;
        }

        /// <summary>
        /// Devuelve el Correlation ID del request actual. Lo usan los handlers de errores (F4)
        /// para el campo correlationId de la respuesta.
        /// </summary>
        public static string GetCorrelationId(this HttpContext context) =>
            context.Items.TryGetValue(ItemKey, out var valor) && valor is string id
                ? id
                : context.TraceIdentifier;

        /// <summary>
        /// Agrega el header X-Correlation-Id a todas las llamadas que hace este HttpClient.
        /// Uso: builder.Services.AddHttpClient("OrdersApi", ...).AddCorrelationIdPropagation();
        /// </summary>
        public static IHttpClientBuilder AddCorrelationIdPropagation(this IHttpClientBuilder builder) =>
            builder.AddHttpMessageHandler<CorrelationIdDelegatingHandler>();
    }

    /// <summary>
    /// Handler que se ejecuta en cada llamada HTTP saliente y le copia el
    /// Correlation ID del request que se está atendiendo.
    /// </summary>
    public class CorrelationIdDelegatingHandler(IHttpContextAccessor accessor) : DelegatingHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var context = accessor.HttpContext;
            if (context is not null && !request.Headers.Contains(CorrelationIdExtensions.HeaderName))
            {
                request.Headers.Add(CorrelationIdExtensions.HeaderName, context.GetCorrelationId());
            }

            return base.SendAsync(request, cancellationToken);
        }
    }
}
