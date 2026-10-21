// Bloque de health checks (F5): /health, /health/live y /health/ready en JSON.
// Program.cs lo usa con dos líneas:
//   builder.Services.AddHealth(builder.Configuration);
//   app.MapHealth();
// Para replicarlo en otro servicio: copiar este archivo y cambiar solo el namespace.
// Los checks se arman solos a partir de appsettings.json:
//   - ConnectionStrings:DefaultConnection -> check de la base (ready)
//   - cada URL de la sección "Services"    -> check de esa dependencia HTTP (ready)
using System.Text.Encodings.Web;
using System.Text.Json;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Products.API.Extensions
{
    public static class HealthExtensions
    {
        // Etiquetas para separar qué check corre en cada endpoint.
        private const string Live = "live";
        private const string Ready = "ready";

        /// <summary>Nombre del HttpClient que usan los checks de dependencias.</summary>
        public const string HealthClient = "health";

        /// <summary>
        /// Registra los checks: el proceso (live), la base de datos (ready) y una
        /// dependencia HTTP por cada servicio configurado en "Services" (ready).
        /// </summary>
        public static IServiceCollection AddHealth(this IServiceCollection services, IConfiguration configuration)
        {
            // Timeout corto: un servicio caído no puede dejar colgado al health check.
            services.AddHttpClient(HealthClient, client => client.Timeout = TimeSpan.FromSeconds(2));

            var checks = services.AddHealthChecks()
                // Live: si este código responde, el proceso está vivo. No mira nada más.
                .AddCheck("proceso", () => HealthCheckResult.Healthy("El servicio está en ejecución."), tags: [Live]);

            var connectionString = configuration.GetConnectionString("DefaultConnection");
            if (!string.IsNullOrWhiteSpace(connectionString))
            {
                // Ready: sin base de datos el servicio no puede atender requests -> Unhealthy.
                checks.AddCheck("base-de-datos", new SqliteHealthCheck(connectionString), tags: [Ready]);
            }

            foreach (var dependencia in configuration.GetSection("Services").GetChildren())
            {
                if (string.IsNullOrWhiteSpace(dependencia.Value)) continue;

                // Ready: si otro servicio no responde, este sigue andando pero con funciones
                // limitadas (por ejemplo Products no puede verificar órdenes) -> Degraded.
                var nombre = dependencia.Key; // "OrdersApi", "UsersApi"...
                var url = new Uri(new Uri(dependencia.Value), "health/live");
                checks.Add(new HealthCheckRegistration(
                    nombre,
                    sp => new HttpDependencyHealthCheck(sp.GetRequiredService<IHttpClientFactory>(), url),
                    HealthStatus.Degraded,
                    [Ready]));
            }

            return services;
        }

        /// <summary>
        /// Expone los tres endpoints con respuesta JSON:
        /// /health (todos los checks), /health/live (solo el proceso) y /health/ready (base y dependencias).
        /// </summary>
        public static WebApplication MapHealth(this WebApplication app)
        {
            app.MapHealthChecks("/health", new HealthCheckOptions
            {
                ResponseWriter = WriteJson
            });

            app.MapHealthChecks("/health/live", new HealthCheckOptions
            {
                Predicate = check => check.Tags.Contains(Live),
                ResponseWriter = WriteJson
            });

            app.MapHealthChecks("/health/ready", new HealthCheckOptions
            {
                Predicate = check => check.Tags.Contains(Ready),
                ResponseWriter = WriteJson
            });

            return app;
        }

        // Respuesta JSON con el estado general y el detalle de cada check.
        // Healthy y Degraded responden 200; Unhealthy responde 503.
        private static Task WriteJson(HttpContext context, HealthReport report)
        {
            context.Response.ContentType = "application/json; charset=utf-8";

            var body = new
            {
                status = report.Status.ToString(),
                servicio = context.RequestServices.GetRequiredService<IWebHostEnvironment>().ApplicationName,
                duracionMs = Math.Round(report.TotalDuration.TotalMilliseconds),
                checks = report.Entries.Select(e => new
                {
                    nombre = e.Key,
                    status = e.Value.Status.ToString(),
                    descripcion = e.Value.Description,
                    duracionMs = Math.Round(e.Value.Duration.TotalMilliseconds)
                })
            };

            return context.Response.WriteAsync(JsonSerializer.Serialize(body,
                new JsonSerializerOptions(JsonSerializerDefaults.Web)
                {
                    WriteIndented = true,
                    // Deja los acentos legibles ("está" en vez de "est\u00E1").
                    Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                }));
        }
    }

    /// <summary>
    /// Verifica que la base SQLite se pueda abrir y consultar.
    /// </summary>
    public class SqliteHealthCheck(string connectionString) : IHealthCheck
    {
        public async Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            try
            {
                await using var connection = new SqliteConnection(connectionString);
                await connection.OpenAsync(cancellationToken);
                await using var command = connection.CreateCommand();
                command.CommandText = "SELECT 1";
                await command.ExecuteScalarAsync(cancellationToken);
                return HealthCheckResult.Healthy("La base de datos responde.");
            }
            catch (Exception ex)
            {
                // Sin detalles internos en la respuesta: solo el tipo de problema.
                return new HealthCheckResult(context.Registration.FailureStatus,
                    "No se pudo acceder a la base de datos.", ex);
            }
        }
    }

    /// <summary>
    /// Verifica que otro microservicio responda en su /health/live.
    /// </summary>
    public class HttpDependencyHealthCheck(IHttpClientFactory factory, Uri url) : IHealthCheck
    {
        public async Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            try
            {
                var client = factory.CreateClient(HealthExtensions.HealthClient);
                using var response = await client.GetAsync(url, cancellationToken);

                return response.IsSuccessStatusCode
                    ? HealthCheckResult.Healthy($"{url.GetLeftPart(UriPartial.Authority)} responde.")
                    : new HealthCheckResult(context.Registration.FailureStatus,
                        $"{url.GetLeftPart(UriPartial.Authority)} respondió {(int)response.StatusCode}.");
            }
            catch (Exception)
            {
                // Servicio apagado, timeout o error de red.
                return new HealthCheckResult(context.Registration.FailureStatus,
                    $"{url.GetLeftPart(UriPartial.Authority)} no responde.");
            }
        }
    }
}
