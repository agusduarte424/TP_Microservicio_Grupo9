# Logging (Serilog)

Responsable: Rama (paquete F2).

Cada servicio usa **Serilog** con dos salidas:

- **Consola**, en formato legible, para mirar mientras se prueba.
- **Archivo JSON** en `src/<Servicio>.API/logs/<servicio>-AAAAMMDD.json`, un evento por línea, para buscar y filtrar. La carpeta `logs/` está en el `.gitignore`.

Products.API es la implementación de referencia; los demás servicios copian el mismo archivo.

## Idea en una frase

Todo el logging vive en `Extensions/LoggingExtensions.cs`, y `Program.cs` lo usa con dos líneas.

```csharp
var builder = WebApplication.CreateBuilder(args);
builder.AddSerilogLogging();     // 1) primera configuración, antes que todo
...
var app = builder.Build();
app.UseRequestLogging();         // 2) antes de app.UseExceptionHandler()
```

## Qué trae cada log

| Campo | De dónde sale |
|---|---|
| `Timestamp` | Lo pone Serilog en cada evento. |
| Nivel (`Level`) | `Information`, `Warning` o `Error` según quién loguea (ver abajo). |
| `Servicio` | `builder.Environment.ApplicationName` (por ejemplo `Products.API`). Se agrega a todos los eventos con `Enrich.WithProperty`. |
| `Endpoint` | Método y ruta del request (`GET /api/products/5`). Lo agrega `UseRequestLogging` al contexto del request. |
| `CorrelationId` | Lo agrega el middleware de Correlation ID (F3). Hasta que F3 esté, aparece vacío. |
| `ErrorCode` | Lo escriben los handlers de errores (F4) cuando loguean una excepción. |

Ejemplo en consola:

```
[19:52:03 INF] Products.API | 7f3c... | GET /api/products/99 | Inicio GET /api/products/99
[19:52:03 WRN] Products.API | 7f3c... | GET /api/products/99 | Error de negocio PRD-001: Producto no encontrado.
[19:52:03 WRN] Products.API | 7f3c... | GET /api/products/99 | Fin GET /api/products/99 -> 404 en 12 ms
```

## Inicio y fin de cada request

`UseRequestLogging()` registra dos middlewares:

1. **Inicio:** guarda `Endpoint` en el contexto de logs y escribe `Inicio {Metodo} {Ruta}`.
2. **Fin:** `UseSerilogRequestLogging` escribe `Fin ... -> {StatusCode} en {Elapsed} ms`, con un nivel que depende del status:

| Resultado | Nivel |
|---|---|
| 2xx / 3xx | `Information` |
| 4xx (error de negocio o validación) | `Warning` |
| 5xx o excepción sin manejar | `Error` |

### Por qué va antes de `UseExceptionHandler()`

Los middlewares se ejecutan como capas: la que se registra primero envuelve a las siguientes. Si el log de requests envuelve al manejador de errores, cuando el request vuelve el handler ya convirtió la excepción en su respuesta (por ejemplo 404 con `PRD-001`) y el log registra ese status real, como `Warning`.

En G17 estaba al revés (después de `UseExceptionHandler`): el log veía la excepción sin manejar y registraba los errores de negocio como `Error` 500.

## Cómo tienen que loguear los handlers de errores (para F4)

Para que el `ErrorCode` quede en los logs con el nivel correcto:

```csharp
// Handlers de negocio (NotFound, BusinessRule, Validation...): Warning
logger.LogWarning("Error de negocio {ErrorCode}: {ErrorMessage}", ex.ErrorCode, ex.Message);

// Handler genérico (error inesperado): Error, con la excepción para tener el stack trace en el log
logger.LogError(exception, "Error inesperado {ErrorCode}", "PRD-005");
```

El stack trace queda **solo en el log**; nunca va en la respuesta HTTP.

## Niveles mínimos

Se configuran en `appsettings.json`, sección `Serilog`. Los logs internos de ASP.NET Core y de `HttpClient` se suben a `Warning` para que no tapen los nuestros.

## Cómo replicarlo en otro servicio

1. Copiar `Extensions/LoggingExtensions.cs` y cambiar el `namespace` (por ejemplo `Orders.API.Extensions`).
2. Agregar al `.csproj` los mismos paquetes, con las mismas versiones que Products: `Serilog.AspNetCore`, `Serilog.Sinks.Console` y `Serilog.Sinks.File`.
3. Reemplazar la sección `Logging` de `appsettings.json` por la sección `Serilog` de Products.
4. Agregar las dos líneas en `Program.cs`, en el mismo lugar que en Products.

## Cómo probarlo

1. `dotnet run --project src/Products.API`.
2. Hacer un request, por ejemplo abrir `http://localhost:5001/api/products` en el navegador.
3. En la consola tienen que aparecer las líneas `Inicio` y `Fin` con el status y la duración.
4. Abrir `src/Products.API/logs/products-AAAAMMDD.json`: cada línea es un JSON con `Timestamp`, `Level` y, dentro de `Properties`, `Servicio` y `Endpoint`.
