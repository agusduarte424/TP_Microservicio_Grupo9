// DatabaseInitializer crea las tablas de orders.db cuando arranca la app.
// Si las tablas ya existen no hace nada (CREATE TABLE IF NOT EXISTS),
// así que se puede ejecutar en cada arranque sin perder datos.
using Microsoft.Data.Sqlite;

namespace Orders.API.Data
{
    /// <summary>
    /// Crea el esquema de la base de Orders si todavía no existe.
    /// </summary>
    public class DatabaseInitializer
    {
        private readonly string _connectionString;

        public DatabaseInitializer(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException(
                    "Falta ConnectionStrings:DefaultConnection en appsettings.json");
        }

        /// <summary>
        /// Crea las tablas orders y order_items. Se llama una sola vez desde Program.cs al arrancar.
        /// </summary>
        public void Initialize()
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            // Una orden tiene muchos items: van en tablas separadas, unidas por orderId.
            // El Guid y las fechas se guardan como texto porque SQLite no tiene esos tipos.
            var command = connection.CreateCommand();
            command.CommandText = """
                CREATE TABLE IF NOT EXISTS orders (
                    id TEXT PRIMARY KEY,
                    usuarioId TEXT NOT NULL,
                    total REAL NOT NULL,
                    estado TEXT NOT NULL,
                    fechaCreacion TEXT NOT NULL,
                    fechaActualizacion TEXT NOT NULL
                );

                CREATE TABLE IF NOT EXISTS order_items (
                    orderId TEXT NOT NULL REFERENCES orders(id),
                    productoId TEXT NOT NULL,
                    cantidad INTEGER NOT NULL,
                    precioUnitario REAL NOT NULL
                );

                CREATE INDEX IF NOT EXISTS ix_orders_usuarioId ON orders(usuarioId);
                CREATE INDEX IF NOT EXISTS ix_order_items_orderId ON order_items(orderId);
            """;
            command.ExecuteNonQuery();
        }
    }
}
