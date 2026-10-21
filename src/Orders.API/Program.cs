// Program.cs solo compone: cada bloque transversal (logging, Correlation ID,
// errores, health, Swagger) se agrega con una línea que llama a su archivo
// dentro de Extensions/.
using Orders.API.Extensions;
using Orders.API.Services;

var builder = WebApplication.CreateBuilder(args);

// Serilog va antes que cualquier otra configuración.
builder.AddSerilogLogging();

builder.Services.AddControllers();
builder.Services.AddErrorHandling();
builder.Services.AddPersistence();
builder.Services.AddCorrelationId();
builder.Services.AddServiceClients(builder.Configuration);
builder.Services.AddHealth(builder.Configuration);
builder.Services.AddSwaggerDocs();
builder.Services.AddScoped<OrderService>();

var app = builder.Build();

app.InitializeDatabase();

// Correlation ID primero, para que todos los logs del request ya lo tengan.
app.UseCorrelationId();

// Log de inicio y fin de cada request. Va antes de app.UseExceptionHandler()
// para registrar el status final que arma el handler de errores.
app.UseRequestLogging();

// Errores: cada excepción la convierte en respuesta su IExceptionHandler.
app.UseExceptionHandler();

app.UseSwaggerDocs();
app.MapControllers();
app.MapHealth();

app.Run();
