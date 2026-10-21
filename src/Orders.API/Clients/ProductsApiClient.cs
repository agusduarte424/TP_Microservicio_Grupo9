// Cliente HTTP de Products.API. Orders lo usa para validar cada producto, su stock y su precio.
// Las URLs y el header X-Correlation-Id los configura Extensions/ClientsExtensions.cs.
using System.Net.Http.Json;

namespace Orders.API.Clients
{
    /// <summary>
    /// Datos de un producto que Orders necesita para crear la orden.
    /// Es solo una parte de lo que devuelve Products.API (no hace falta el resto).
    /// </summary>
    public record ProductoInfo(Guid Id, string Nombre, decimal Precio, int Stock);

    /// <summary>
    /// Consultas que Orders le hace a Products.API.
    /// </summary>
    public interface IProductsApiClient
    {
        /// <summary>
        /// Devuelve el producto, o null si Products.API responde 404.
        /// Cualquier otra respuesta (5xx, timeout, servicio apagado) lanza una excepción (ORD-007).
        /// </summary>
        Task<ProductoInfo?> ObtenerProducto(Guid productoId, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Implementación con HttpClient: llama a GET /api/products/{id}.
    /// </summary>
    public class ProductsApiClient(HttpClient http) : IProductsApiClient
    {
        public async Task<ProductoInfo?> ObtenerProducto(Guid productoId, CancellationToken cancellationToken = default)
        {
            using var response = await http.GetAsync($"api/products/{productoId}", cancellationToken);

            // Se mira el status antes de leer el body, como recomienda la consigna.
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                return null;

            if (!response.IsSuccessStatusCode)
                throw new HttpRequestException(
                    $"Products.API respondió {(int)response.StatusCode} al consultar el producto {productoId}.");

            return await response.Content.ReadFromJsonAsync<ProductoInfo>(cancellationToken)
                ?? throw new HttpRequestException(
                    $"Products.API devolvió un producto vacío para {productoId}.");
        }
    }
}
