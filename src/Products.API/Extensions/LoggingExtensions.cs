// Bloque de logging (F2): Serilog con consola legible y archivo JSON estructurado.
// Program.cs lo usa con dos líneas:
//   builder.AddSerilogLogging();   -> primera configuración, antes que todo
//   app.UseRequestLogging();       -> antes de app.UseExceptionHandler()
// Para replicarlo en otro servicio: copiar este archivo y cambiar solo el namespace.
using Serilog;
using Serilog.Context;
using Serilog.Events;
using Serilog.Formatting.Json;

namespace Products.API.Extensions
{
    public static class LoggingExtensions
    {
        // Formato de consola: una línea legible por evento.
        // CorrelationId lo agrega F3; Endpoint lo agrega UseRequestLogging.
        private const string ConsoleTemplate =
            "[{Timestamp:HH:mm:ss} {Level:u3}] {Servicio} | {CorrelationId} | {Endpoint} | {Message:lj}{NewLine}{Exception}";

        /// <summary>
        /// Reemplaza el logging por defecto de ASP.NET Core por Serilog.
        /// Escribe en consola (legible) y en logs/&lt;servicio&gt;-AAAAMMDD.json (JSON, un evento por línea).
        /// </summary>
        public static WebApplicationBuilder AddSerilogLogging(this WebApplicationBuilder builder)
        {
            // "Products.API": el nombre del proyecto, sin configurarlo a mano en cada servicio.
            var servicio = builder.Environment.ApplicationName;

            // logs/ queda dentro de la carpeta del proyecto, como pide la consigna.
            var archivo = Path.Combine(
                builder.Environment.ContentRootPath, "logs",
                $"{servicio.Replace(".API", "").ToLowerInvariant()}-.json");

            builder.Services.AddSerilog(config => config
                // Niveles mínimos desde appsettings.json (sección "Serilog").
                .ReadFrom.Configuration(builder.Configuration)
                // Campo Servicio en todos los logs.
                .Enrich.WithProperty("Servicio", servicio)
                // Toma los campos que se agregan durante el request (Endpoint, CorrelationId).
                .Enrich.FromLogContext()
                .WriteTo.Console(outputTemplate: ConsoleTemplate)
                // JsonFormatter escribe Timestamp, Level, MessageTemplate y Properties
                // (Servicio, Endpoint, CorrelationId, ErrorCode...). renderMessage agrega el texto final.
                .WriteTo.File(new JsonFormatter(renderMessage: true), archivo,
                    rollingInterval: RollingInterval.Day));

            return builder;
        }

        /// <summary>
        /// Loggea el inicio y el fin de cada request. El fin incluye status y duración,
        /// con nivel según el resultado: 5xx o excepción = Error, 4xx = Warning, resto = Information.
        /// Va antes de UseExceptionHandler para registrar el status final que arma el handler.
        /// </summary>
        public static WebApplication UseRequestLogging(this WebApplication app)
        {
            // 1) Inicio del request y campo Endpoint para todos los logs que se escriban adentro.
            app.Use(async (context, next) =>
            {
                var endpoint = $"{context.Request.Method} {context.Request.Path}";
                using (LogContext.PushProperty("Endpoint", endpoint))
                {
                    var logger = context.RequestServices
                        .GetRequiredService<ILoggerFactory>()
                        .CreateLogger("Request");
                    logger.LogInformation("Inicio {Metodo} {Ruta}",
                        context.Request.Method, context.Request.Path.Value);

                    await next(context);
                }
            });

            // 2) Fin del request: status y duración en milisegundos.
            app.UseSerilogRequestLogging(options =>
            {
                options.MessageTemplate =
                    "Fin {RequestMethod} {RequestPath} -> {StatusCode} en {Elapsed:0} ms";

                options.GetLevel = (context, elapsed, exception) =>
                    exception is not null || context.Response.StatusCode >= 500
                        ? LogEventLevel.Error
                        : context.Response.StatusCode >= 400
                            ? LogEventLevel.Warning
                            : LogEventLevel.Information;
            });

            return app;
        }
    }
}
