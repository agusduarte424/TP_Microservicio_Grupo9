// ProductRepository implementa IProductRepository con SQLite y Dapper:
// el ProductService le pide que guarde, busque, actualice o elimine productos
// y el Repository se encarga de hablar con la base products.db.
using System.Globalization;
using Dapper;
using Microsoft.Data.Sqlite;
using Products.API.Models;

namespace Products.API.Data
{
    /// <summary>
    /// Persistencia de productos en SQLite usando Dapper.
    /// </summary>
    public class ProductRepository : IProductRepository
    {
        private readonly string _connectionString;

        // la cadena de conexión viene de appsettings.json → ConnectionStrings:DefaultConnection
        // si falta, se corta al arrancar en vez de crear una base en un lugar inesperado
        public ProductRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException(
                    "Falta ConnectionStrings:DefaultConnection en appsettings.json");
        }

        // se abre una conexión nueva por operación; SQLite es un archivo local,
        // así que abrirla es barato y no hay que compartirla entre requests
        private SqliteConnection CreateConnection() => new(_connectionString);

        public async Task<IEnumerable<Product>> ObtenerTodos(string? categoria, string? nombre)
        {
            using var conn = CreateConnection();

            var sql = "SELECT * FROM products WHERE 1=1";

            // los filtros se agregan solo si vienen, siempre como parámetros (nunca concatenando el valor)
            if (!string.IsNullOrWhiteSpace(categoria))
                sql += " AND categoria = @Categoria";

            if (!string.IsNullOrWhiteSpace(nombre))
                sql += " AND nombre LIKE @Nombre";

            var rows = await conn.QueryAsync<ProductRow>(sql, new
            {
                Categoria = categoria,
                Nombre = $"%{nombre}%"
            });

            return rows.Select(ToProduct);
        }

        public async Task<Product?> ObtenerPorId(Guid id)
        {
            using var conn = CreateConnection();

            var row = await conn.QueryFirstOrDefaultAsync<ProductRow>(
                "SELECT * FROM products WHERE id = @Id",
                new { Id = id.ToString() });

            return row is null ? null : ToProduct(row);
        }

        public async Task<bool> ExistePorNombreYCategoria(string nombre, string categoria, Guid? excludeId = null)
        {
            using var conn = CreateConnection();

            var sql = "SELECT COUNT(*) FROM products WHERE nombre = @Nombre AND categoria = @Categoria";

            if (excludeId.HasValue)
                sql += " AND id != @ExcludeId";

            var count = await conn.ExecuteScalarAsync<int>(sql, new
            {
                Nombre = nombre,
                Categoria = categoria,
                ExcludeId = excludeId?.ToString()
            });

            return count > 0;
        }

        public async Task Guardar(Product product)
        {
            using var conn = CreateConnection();

            await conn.ExecuteAsync("""
                INSERT INTO products (id, nombre, descripcion, precio, stock, categoria, fechaCreacion)
                VALUES (@Id, @Nombre, @Descripcion, @Precio, @Stock, @Categoria, @FechaCreacion)
            """, new
            {
                Id = product.Id.ToString(),
                product.Nombre,
                product.Descripcion,
                Precio = (double)product.Precio,
                product.Stock,
                product.Categoria,
                FechaCreacion = product.FechaCreacion.ToString("o", CultureInfo.InvariantCulture)
            });
        }

        public async Task Actualizar(Product product)
        {
            using var conn = CreateConnection();

            await conn.ExecuteAsync("""
                UPDATE products
                SET nombre = @Nombre,
                    descripcion = @Descripcion,
                    precio = @Precio,
                    stock = @Stock,
                    categoria = @Categoria
                WHERE id = @Id
            """, new
            {
                Id = product.Id.ToString(),
                product.Nombre,
                product.Descripcion,
                Precio = (double)product.Precio,
                product.Stock,
                product.Categoria
            });
        }

        public async Task Eliminar(Guid id)
        {
            using var conn = CreateConnection();

            await conn.ExecuteAsync(
                "DELETE FROM products WHERE id = @Id",
                new { Id = id.ToString() });
        }

        // SQLite guarda el Guid y la fecha como texto y el precio como REAL:
        // Dapper llena esta fila con los tipos de la base y ToProduct la pasa al modelo
        private sealed class ProductRow
        {
            public string Id { get; set; } = string.Empty;
            public string Nombre { get; set; } = string.Empty;
            public string? Descripcion { get; set; }
            public double Precio { get; set; }
            public long Stock { get; set; }
            public string Categoria { get; set; } = string.Empty;
            public string FechaCreacion { get; set; } = string.Empty;
        }

        private static Product ToProduct(ProductRow row) => new()
        {
            Id = Guid.Parse(row.Id),
            Nombre = row.Nombre,
            Descripcion = row.Descripcion,
            Precio = (decimal)row.Precio,
            Stock = (int)row.Stock,
            Categoria = row.Categoria,
            FechaCreacion = DateTime.Parse(row.FechaCreacion, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind)
        };
    }
}
