// Bloque de persistencia: registra el repositorio y crea la base al arrancar.
// Program.cs lo usa con dos líneas: builder.Services.AddPersistence() y app.InitializeDatabase().
using Products.API.Data;

namespace Products.API.Extensions
{
    public static class PersistenceExtensions
    {
        /// <summary>
        /// Registra en DI el inicializador de la base y el repositorio de productos.
        /// Los Services piden IProductRepository y reciben ProductRepository.
        /// </summary>
        public static IServiceCollection AddPersistence(this IServiceCollection services)
        {
            services.AddSingleton<DatabaseInitializer>();
            services.AddScoped<IProductRepository, ProductRepository>();
            return services;
        }

        /// <summary>
        /// Crea las tablas si no existen. Se llama antes de app.Run().
        /// </summary>
        public static WebApplication InitializeDatabase(this WebApplication app)
        {
            app.Services.GetRequiredService<DatabaseInitializer>().Initialize();
            return app;
        }
    }
}
