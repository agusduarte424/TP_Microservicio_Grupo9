// IProductRepository es el contrato de persistencia de Products:
// el ProductService solo conoce esta interfaz y no sabe si atrás hay SQLite,
// otra base o un repositorio falso para pruebas.
// Ningún repositorio hace llamadas HTTP: eso es trabajo del Service.
using Products.API.Models;

namespace Products.API.Data
{
    /// <summary>
    /// Operaciones de persistencia de productos.
    /// </summary>
    public interface IProductRepository
    {
        /// <summary>
        /// Devuelve los productos, filtrando opcionalmente por categoría exacta
        /// y por nombre parcial.
        /// </summary>
        Task<IEnumerable<Product>> ObtenerTodos(string? categoria, string? nombre);

        /// <summary>
        /// Devuelve el producto con ese id, o null si no existe.
        /// </summary>
        Task<Product?> ObtenerPorId(Guid id);

        /// <summary>
        /// Indica si ya existe un producto con ese nombre y categoría.
        /// <paramref name="excludeId"/> permite ignorar al propio producto al actualizarlo.
        /// </summary>
        Task<bool> ExistePorNombreYCategoria(string nombre, string categoria, Guid? excludeId = null);

        /// <summary>
        /// Inserta un producto nuevo.
        /// </summary>
        Task Guardar(Product product);

        /// <summary>
        /// Actualiza los datos de un producto existente.
        /// </summary>
        Task Actualizar(Product product);

        /// <summary>
        /// Elimina el producto con ese id.
        /// </summary>
        Task Eliminar(Guid id);
    }
}
