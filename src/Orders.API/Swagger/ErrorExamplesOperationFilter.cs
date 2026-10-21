// Filtro de Swagger: por cada [ErrorExample] de un endpoint agrega, en la respuesta de su status,
// un ejemplo completo con el contrato de errores de la consigna.
using System.Reflection;
using System.Text.Json;
using Microsoft.OpenApi.Models;
using Orders.API.DTOs;
using Orders.API.ExceptionHandlers;
using Orders.API.Exceptions;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Orders.API.Swagger
{
    public class ErrorExamplesOperationFilter : IOperationFilter
    {
        private const string ProblemJson = "application/problem+json";

        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            var ejemplos = context.MethodInfo.GetCustomAttributes<ErrorExampleAttribute>();

            foreach (var ejemplo in ejemplos)
            {
                var definicion = ErrorCodes.Catalogo[ejemplo.ErrorCode];
                var status = definicion.Status.ToString();

                if (!operation.Responses.TryGetValue(status, out var response))
                    continue;

                if (!response.Content.TryGetValue(ProblemJson, out var mediaType))
                {
                    mediaType = new OpenApiMediaType();
                    response.Content[ProblemJson] = mediaType;
                }

                var error = new ErrorResponse
                {
                    Type = Tipos[definicion.Status].Type,
                    Title = Tipos[definicion.Status].Title,
                    Status = definicion.Status,
                    Detail = definicion.Detail,
                    Instance = Instancia(context.ApiDescription.RelativePath),
                    ErrorCode = ejemplo.ErrorCode,
                    ErrorMessage = ejemplo.ErrorMessage,
                    CorrelationId = "3a275d6f-adca-4af3-8a89-808b33e69614"
                };

                // El JSON se arma con las mismas opciones que la API (camelCase).
                var json = JsonSerializer.Serialize(error, new JsonSerializerOptions(JsonSerializerDefaults.Web));
                mediaType.Examples[ejemplo.ErrorCode] = new OpenApiExample
                {
                    Summary = $"{ejemplo.ErrorCode}: {ErrorCodes.Catalogo[ejemplo.ErrorCode].Message}",
                    Value = OpenApiAnyFactory.CreateFromJson(json)
                };
            }
        }

        // Ruta de ejemplo con un id concreto en vez de "{id}".
        private static string Instancia(string? rutaRelativa) =>
            "/" + (rutaRelativa ?? string.Empty).Replace("{id}", "f1e2d3c4-0000-0000-0000-aabbccddeeff");

        // Los mismos type y title que usan las respuestas reales.
        private static IReadOnlyDictionary<int, (string Type, string Title)> Tipos => ErrorResponseWriter.Statuses;
    }
}
