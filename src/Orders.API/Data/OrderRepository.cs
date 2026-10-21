// OrderRepository implementa IOrderRepository con SQLite y Dapper:
// el OrderService le pide que guarde, busque o actualice órdenes
// y el Repository se encarga de hablar con la base orders.db.
using System.Globalization;
using Dapper;
using Microsoft.Data.Sqlite;
using Orders.API.Models;

namespace Orders.API.Data
{
    /// <summary>
    /// Persistencia de órdenes en SQLite usando Dapper.
    /// </summary>
    public class OrderRepository : IOrderRepository
    {
        private readonly string _connectionString;

        // la cadena de conexión viene de appsettings.json → ConnectionStrings:DefaultConnection
        public OrderRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException(
                    "Falta ConnectionStrings:DefaultConnection en appsettings.json");
        }

        // una conexión nueva por operación: SQLite es un archivo local y abrirla es barato
        private SqliteConnection CreateConnection() => new(_connectionString);

        public async Task<IEnumerable<Order>> ObtenerTodas(Guid? usuarioId)
        {
            using var conn = CreateConnection();

            var sql = "SELECT * FROM orders";
            if (usuarioId.HasValue)
                sql += " WHERE usuarioId = @UsuarioId";
            sql += " ORDER BY fechaCreacion";

            var rows = (await conn.QueryAsync<OrderRow>(sql,
                new { UsuarioId = usuarioId?.ToString() })).ToList();

            if (rows.Count == 0)
                return [];

            // Todos los items en una sola consulta (en vez de una consulta por orden).
            // Dapper expande @OrderIds a "IN (@p1, @p2, ...)", siempre como parámetros.
            var items = (await conn.QueryAsync<OrderItemRow>(
                    "SELECT * FROM order_items WHERE orderId IN @OrderIds",
                    new { OrderIds = rows.Select(r => r.Id).ToList() }))
                .ToLookup(i => i.OrderId);

            return rows.Select(r => ToOrder(r, items[r.Id]));
        }

        public async Task<Order?> ObtenerPorId(Guid id)
        {
            using var conn = CreateConnection();

            var row = await conn.QueryFirstOrDefaultAsync<OrderRow>(
                "SELECT * FROM orders WHERE id = @Id",
                new { Id = id.ToString() });

            if (row is null)
                return null;

            var items = await conn.QueryAsync<OrderItemRow>(
                "SELECT * FROM order_items WHERE orderId = @OrderId",
                new { OrderId = row.Id });

            return ToOrder(row, items);
        }

        public async Task Guardar(Order order)
        {
            using var conn = CreateConnection();
            await conn.OpenAsync();

            // La orden y sus items se guardan en una transacción: si falla algún insert,
            // no queda una orden sin items en la base.
            using var tx = conn.BeginTransaction();

            await conn.ExecuteAsync("""
                INSERT INTO orders (id, usuarioId, total, estado, fechaCreacion, fechaActualizacion)
                VALUES (@Id, @UsuarioId, @Total, @Estado, @FechaCreacion, @FechaActualizacion)
            """, new
            {
                Id = order.Id.ToString(),
                UsuarioId = order.UsuarioId.ToString(),
                Total = (double)order.Total,
                order.Estado,
                FechaCreacion = ToText(order.FechaCreacion),
                FechaActualizacion = ToText(order.FechaActualizacion)
            }, tx);

            // Dapper ejecuta el insert una vez por cada elemento de la lista.
            await conn.ExecuteAsync("""
                INSERT INTO order_items (orderId, productoId, cantidad, precioUnitario)
                VALUES (@OrderId, @ProductoId, @Cantidad, @PrecioUnitario)
            """, order.Items.Select(i => new
            {
                OrderId = order.Id.ToString(),
                ProductoId = i.ProductoId.ToString(),
                i.Cantidad,
                PrecioUnitario = (double)i.PrecioUnitario
            }), tx);

            tx.Commit();
        }

        public async Task ActualizarEstado(Guid id, string estado, DateTime fechaActualizacion)
        {
            using var conn = CreateConnection();

            await conn.ExecuteAsync("""
                UPDATE orders
                SET estado = @Estado, fechaActualizacion = @FechaActualizacion
                WHERE id = @Id
            """, new
            {
                Id = id.ToString(),
                Estado = estado,
                FechaActualizacion = ToText(fechaActualizacion)
            });
        }

        // Las fechas se guardan en formato ISO 8601 ("o") para no perder la zona horaria.
        private static string ToText(DateTime fecha) =>
            fecha.ToString("o", CultureInfo.InvariantCulture);

        private static DateTime ToDate(string texto) =>
            DateTime.Parse(texto, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);

        // SQLite guarda el Guid y las fechas como texto y los importes como REAL:
        // Dapper llena estas filas con los tipos de la base y ToOrder las pasa al modelo
        private sealed class OrderRow
        {
            public string Id { get; set; } = string.Empty;
            public string UsuarioId { get; set; } = string.Empty;
            public double Total { get; set; }
            public string Estado { get; set; } = string.Empty;
            public string FechaCreacion { get; set; } = string.Empty;
            public string FechaActualizacion { get; set; } = string.Empty;
        }

        private sealed class OrderItemRow
        {
            public string OrderId { get; set; } = string.Empty;
            public string ProductoId { get; set; } = string.Empty;
            public long Cantidad { get; set; }
            public double PrecioUnitario { get; set; }
        }

        private static Order ToOrder(OrderRow row, IEnumerable<OrderItemRow> items) => new()
        {
            Id = Guid.Parse(row.Id),
            UsuarioId = Guid.Parse(row.UsuarioId),
            Total = (decimal)row.Total,
            Estado = row.Estado,
            FechaCreacion = ToDate(row.FechaCreacion),
            FechaActualizacion = ToDate(row.FechaActualizacion),
            Items = items.Select(i => new OrderItem
            {
                ProductoId = Guid.Parse(i.ProductoId),
                Cantidad = (int)i.Cantidad,
                PrecioUnitario = (decimal)i.PrecioUnitario
            }).ToList()
        };
    }
}
