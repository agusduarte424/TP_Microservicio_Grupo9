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

// Log de inicio y fin de cada request. Va antes de app.UseExceptionHandler()
// para registrar el status final que arma el handler de errores.
app.UseRequestLogging();

app.MapControllers();

app.Run();
