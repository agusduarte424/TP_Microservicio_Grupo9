// Program.cs solo compone: cada bloque transversal (logging, Correlation ID,
// errores, health, Swagger) se agrega con una línea que llama a su archivo
// dentro de Extensions/.
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

var app = builder.Build();

app.MapControllers();

app.Run();
