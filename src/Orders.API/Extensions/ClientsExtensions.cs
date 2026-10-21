// Bloque de clientes HTTP: registra los clientes de Users.API y Products.API.
// Program.cs lo usa con una línea: builder.Services.AddServiceClients(builder.Configuration);
using Orders.API.Clients;

namespace Orders.API.Extensions
{
    public static class ClientsExtensions
    {
        /// <summary>
        /// Registra los clientes con IHttpClientFactory. Cada uno toma su URL de la sección
        /// "Services" de appsettings.json, tiene un timeout corto y propaga el Correlation ID.
        /// </summary>
        public static IServiceCollection AddServiceClients(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddHttpClient<IUsersApiClient, UsersApiClient>(client =>
            {
                client.BaseAddress = ObtenerUrl(configuration, "UsersApi");
                client.Timeout = TimeSpan.FromSeconds(5);
            })
            .AddCorrelationIdPropagation();

            services.AddHttpClient<IProductsApiClient, ProductsApiClient>(client =>
            {
                client.BaseAddress = ObtenerUrl(configuration, "ProductsApi");
                client.Timeout = TimeSpan.FromSeconds(5);
            })
            .AddCorrelationIdPropagation();

            return services;
        }

        // Si falta la URL, se corta al arrancar con un mensaje claro en vez de fallar en el primer request.
        private static Uri ObtenerUrl(IConfiguration configuration, string servicio) =>
            new(configuration[$"Services:{servicio}"]
                ?? throw new InvalidOperationException($"Falta Services:{servicio} en appsettings.json"));
    }
}
