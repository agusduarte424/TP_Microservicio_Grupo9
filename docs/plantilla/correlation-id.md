# Correlation ID

Responsable: Rama (paquete F3).

El **Correlation ID** es un identificador único por request que viaja en el header `X-Correlation-Id`. Sirve para seguir una operación a través de varios servicios: cuando Orders llama a Products, los dos escriben el mismo ID en sus logs, y con buscarlo se ve todo el recorrido.

Products.API es la implementación de referencia; los demás servicios copian el mismo archivo.

## Idea en una frase

Todo vive en `Extensions/CorrelationIdExtensions.cs`, y `Program.cs` lo usa con dos líneas.

```csharp
builder.Services.AddCorrelationId();   // registra el handler para las llamadas salientes
...
var app = builder.Build();
app.UseCorrelationId();                 // primer middleware, antes de app.UseRequestLogging()
app.UseRequestLogging();
```

## Qué hace en cada request

```
Cliente ──X-Correlation-Id: abc──► Orders.API ──X-Correlation-Id: abc──► Products.API
           (o sin header:          (lo guarda, lo pone       (lo respeta porque ya viene,
            Orders genera uno)      en sus logs y en la       lo pone en sus logs)
                                    llamada saliente)
```

1. **Lee o genera.** Si el request trae `X-Correlation-Id`, lo respeta (viene de otro servicio o del cliente). Si no, genera un `Guid` nuevo.
2. **Lo guarda** en `HttpContext.Items`, para que cualquier parte del request lo pueda leer con `context.GetCorrelationId()`.
3. **Lo agrega a los logs** con `LogContext.PushProperty("CorrelationId", ...)`. Serilog lo incluye en todo lo que se loguea mientras dura el request (ver `logging.md`).
4. **Lo devuelve** en el header `X-Correlation-Id` de la respuesta, así el cliente puede reportarlo.

### Por qué es el primer middleware

Los middlewares se ejecutan en el orden en que se registran. Si `UseCorrelationId()` va antes de `UseRequestLogging()`, la línea `Inicio ...` del log ya sale con el ID. Si fuera después, el inicio del request quedaría sin ID.

## Propagación a otros servicios

`CorrelationIdDelegatingHandler` es un `DelegatingHandler`: una pieza que se mete en el camino de cada llamada que hace un `HttpClient`. Antes de enviar, copia el Correlation ID del request actual al header de la llamada saliente.

Se activa en cada `HttpClient` que llama a otro servicio, agregando `.AddCorrelationIdPropagation()` al registrarlo:

```csharp
builder.Services.AddHttpClient("OrdersApi", client =>
{
    client.BaseAddress = new Uri(builder.Configuration["Services:OrdersApi"]!);
})
.AddCorrelationIdPropagation();
```

Así ningún Service tiene que acordarse de copiar el header a mano: cualquier llamada hecha con ese cliente lo lleva.

| Servicio | HttpClient que tiene que propagarlo |
|---|---|
| Products.API | `OrdersApi` (para `PRD-004`) |
| Orders.API | `UsersApi` y `ProductsApi` |
| Cart.API | `ProductsApi` |
| Notifications.API | `UsersApi` |
| Users.API | — (no llama a nadie) |

## En las respuestas de error (para F4)

La consigna pide el Correlation ID como campo extra en las respuestas de error. Los handlers lo obtienen así:

```csharp
correlationId = context.GetCorrelationId()
```

## Cómo replicarlo en otro servicio

1. Copiar `Extensions/CorrelationIdExtensions.cs` y cambiar el `namespace` (por ejemplo `Orders.API.Extensions`).
2. Agregar las dos líneas en `Program.cs`, en el mismo lugar que en Products.
3. Agregar `.AddCorrelationIdPropagation()` a cada `HttpClient` del servicio.

## Cómo probarlo

1. Levantar Products: `dotnet run --project src/Products.API`.
2. Desde Swagger o con un `.http`, hacer un request **con** el header `X-Correlation-Id: prueba-123`. En la consola, las líneas del request muestran `prueba-123`, y la respuesta trae el mismo header.
3. Hacer un request **sin** el header: se genera un ID nuevo y aparece en los logs y en el header de la respuesta.
4. Con Orders y Products levantados, crear una orden: el mismo ID aparece en los logs de los dos servicios.
