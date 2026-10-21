// Bloque de logging (F2): Serilog con consola legible y archivo JSON estructurado.
// Program.cs lo usa con una línea:
//   builder.AddSerilogLogging();   -> primera configuración, antes que todo
// Para replicarlo en otro servicio: copiar este archivo y cambiar solo el namespace.
using Serilog;
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
    }
}
