// DatabaseInitializer crea las tablas de products.db cuando arranca la app.
// Si las tablas ya existen no hace nada (CREATE TABLE IF NOT EXISTS),
// así que se puede ejecutar en cada arranque sin perder datos.
using Microsoft.Data.Sqlite;

namespace Products.API.Data
{
    /// <summary>
    /// Crea el esquema de la base de Products si todavía no existe.
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
        /// Crea la tabla products. Se llama una sola vez desde Program.cs al arrancar.
        /// </summary>
        public void Initialize()
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();

            // cada producto tiene: id, nombre, descripcion, precio, stock, categoria y fechaCreacion
            // el Guid y la fecha se guardan como texto porque SQLite no tiene esos tipos
            var command = connection.CreateCommand();
            command.CommandText = """
                CREATE TABLE IF NOT EXISTS products (
                    id TEXT PRIMARY KEY,
                    nombre TEXT NOT NULL,
                    descripcion TEXT,
                    precio REAL NOT NULL,
                    stock INTEGER NOT NULL,
                    categoria TEXT NOT NULL,
                    fechaCreacion TEXT NOT NULL
                );
            """;
            command.ExecuteNonQuery();
        }
    }
}
