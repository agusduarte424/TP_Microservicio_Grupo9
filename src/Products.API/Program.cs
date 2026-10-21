// Program.cs solo compone: cada bloque transversal (logging, Correlation ID,
// errores, health, Swagger) se agrega con una línea que llama a su archivo
// dentro de Extensions/.
using Products.API.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Serilog va antes que cualquier otra configuración.
builder.AddSerilogLogging();

builder.Services.AddControllers();
builder.Services.AddPersistence();

var app = builder.Build();

app.InitializeDatabase();

app.MapControllers();

app.Run();
