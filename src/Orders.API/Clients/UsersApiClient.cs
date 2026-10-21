// Cliente HTTP de Users.API. Orders lo usa para validar que el usuario de la orden exista.
// Las URLs y el header X-Correlation-Id los configura Extensions/ClientsExtensions.cs.
namespace Orders.API.Clients
{
    /// <summary>
    /// Consultas que Orders le hace a Users.API.
    /// </summary>
    public interface IUsersApiClient
    {
        /// <summary>
        /// true si el usuario existe, false si Users.API responde 404.
        /// Cualquier otra respuesta (5xx, timeout, servicio apagado) lanza una excepción:
        /// no se sabe si el usuario existe y la orden no se puede crear (ORD-007).
        /// </summary>
        Task<bool> ExisteUsuario(Guid usuarioId, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Implementación con HttpClient: llama a GET /api/users/{id} (endpoint interno de Users.API).
    /// </summary>
    public class UsersApiClient(HttpClient http) : IUsersApiClient
    {
        public async Task<bool> ExisteUsuario(Guid usuarioId, CancellationToken cancellationToken = default)
        {
            using var response = await http.GetAsync($"api/users/{usuarioId}", cancellationToken);

            // Se mira el status antes de usar la respuesta, como recomienda la consigna.
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                return false;

            if (!response.IsSuccessStatusCode)
                throw new HttpRequestException(
                    $"Users.API respondió {(int)response.StatusCode} al validar el usuario.");

            return true;
        }
    }
}
