// Bloque de manejo de errores: IExceptionHandler por tipo de excepción y uno genérico.
// Program.cs lo usa con dos líneas:
//   builder.Services.AddErrorHandling();
//   app.UseExceptionHandler();      -> después de app.UseRequestLogging()
using Orders.API.ExceptionHandlers;

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

            return services;
        }
    }
}
