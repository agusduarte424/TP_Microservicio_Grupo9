// IOrderRepository es el contrato de persistencia de Orders:
// el OrderService solo conoce esta interfaz y no sabe si atrás hay SQLite,
// otra base o un repositorio falso para pruebas.
// Ningún repositorio hace llamadas HTTP: eso es trabajo del Service y de los clientes de Clients/.
using Orders.API.Models;

namespace Orders.API.Data
{
    /// <summary>
    /// Operaciones de persistencia de órdenes.
    /// </summary>
    public interface IOrderRepository
    {
        /// <summary>
        /// Devuelve las órdenes con sus items. Si viene <paramref name="usuarioId"/>,
        /// solo las de ese usuario.
        /// </summary>
        Task<IEnumerable<Order>> ObtenerTodas(Guid? usuarioId);

        /// <summary>
        /// Devuelve la orden con sus items, o null si no existe.
        /// </summary>
        Task<Order?> ObtenerPorId(Guid id);

        /// <summary>
        /// Inserta una orden nueva con todos sus items (todo o nada).
        /// </summary>
        Task Guardar(Order order);

        /// <summary>
        /// Cambia el estado de una orden y su fecha de actualización.
        /// </summary>
        Task ActualizarEstado(Guid id, string estado, DateTime fechaActualizacion);
    }
}
