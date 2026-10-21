// Bloque de Swagger: documentación de la API en /swagger.
// Program.cs lo usa con dos líneas:
//   builder.Services.AddSwaggerDocs();
//   app.UseSwaggerDocs();
using System.Reflection;
using Microsoft.OpenApi.Models;
using Orders.API.Swagger;

namespace Orders.API.Extensions
{
    public static class SwaggerExtensions
    {
        /// <summary>
        /// Configura Swashbuckle: datos del documento, XML comments de controllers y modelos,
        /// y ejemplos de error a partir de los atributos [ErrorExample].
        /// </summary>
        public static IServiceCollection AddSwaggerDocs(this IServiceCollection services)
        {
            services.AddEndpointsApiExplorer();
            services.AddSwaggerGen(options =>
            {
                options.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "Orders.API",
                    Version = "v1",
                    Description = "Órdenes de compra del E-Commerce. Valida usuarios contra Users.API " +
                                  "y productos, stock y precios contra Products.API. " +
                                  "Todos los errores devuelven errorCode y errorMessage del catálogo ORD-001 a ORD-007."
                });

                // El XML lo genera el .csproj (GenerateDocumentationFile) a partir de los comentarios ///.
                var xml = Path.Combine(AppContext.BaseDirectory, $"{Assembly.GetExecutingAssembly().GetName().Name}.xml");
                options.IncludeXmlComments(xml, includeControllerXmlComments: true);

                options.OperationFilter<ErrorExamplesOperationFilter>();
            });

            return services;
        }

        /// <summary>
        /// Publica el JSON de OpenAPI y la interfaz en /swagger.
        /// Se habilita en todos los entornos porque la consigna pide Swagger en cada servicio.
        /// </summary>
        public static WebApplication UseSwaggerDocs(this WebApplication app)
        {
            app.UseSwagger();
            app.UseSwaggerUI(options =>
            {
                options.SwaggerEndpoint("/swagger/v1/swagger.json", "Orders.API v1");
                options.DocumentTitle = "Orders.API";
            });

            return app;
        }
    }
}
