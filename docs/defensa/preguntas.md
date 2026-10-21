# Preguntas de defensa

Cada paquete terminado suma 3 preguntas con su respuesta corta.

## Agus

### F7 · Persistencia

1. **¿Por qué el Service usa `IProductRepository` y no `ProductRepository`?**
   Para no depender de SQLite: el Service solo conoce el contrato y se puede
   cambiar la base o usar un repositorio falso en pruebas sin tocarlo.
2. **¿Qué pasa si borro `products.db`?** Al arrancar, `InitializeDatabase()`
   ejecuta `CREATE TABLE IF NOT EXISTS` y la base se crea vacía; por eso no se
   versiona.
3. **¿Cómo se evita la inyección SQL?** Todos los valores van como parámetros de
   Dapper (`@Nombre`, `@Id`); el texto del usuario nunca se concatena en el SQL.

## Pili

## Rama

### F2 · Logging con Serilog

1. **¿Por qué Serilog se configura antes que todo en `Program.cs`?** Para que
   cualquier cosa que se registre después (persistencia, errores, Swagger) ya
   escriba con Serilog. Si se configurara al final, los logs del arranque
   saldrían con el logger por defecto, sin `Servicio` ni formato JSON.
2. **¿Por qué el log de requests va antes de `UseExceptionHandler()`?** Porque
   los middlewares son capas: el que se registra primero envuelve a los
   siguientes. Así, cuando el request vuelve, el handler ya armó la respuesta
   (por ejemplo 404 `PRD-001`) y el log registra ese status real como
   `Warning`. Al revés, registraría cada error de negocio como un `Error` 500.
3. **¿Cómo aparece el `Endpoint` en un log que escribe el Service, si el Service
   no lo conoce?** `UseRequestLogging` lo guarda con `LogContext.PushProperty`
   al empezar el request, y `Enrich.FromLogContext()` lo agrega a todos los
   logs que se escriben mientras ese request está en curso.

### F3 · Correlation ID

1. **¿Cómo viaja el Correlation ID de Orders a Products?** El middleware de
   Orders lo lee del header `X-Correlation-Id` (o genera uno) y lo guarda en el
   request. Cuando Orders llama a Products con su `HttpClient`, el
   `CorrelationIdDelegatingHandler` copia ese ID al header de la llamada.
   Products lo recibe, lo respeta en vez de generar otro, y lo escribe en sus
   logs. Buscando ese ID se ven los logs de los dos servicios.
2. **¿Por qué un `DelegatingHandler` y no copiar el header en cada Service?**
   Porque se registra una vez por `HttpClient` y se aplica a todas sus
   llamadas. Si se copiara a mano, bastaría con olvidarse en una sola llamada
   para que la traza se corte.
3. **¿Por qué `UseCorrelationId()` va antes que `UseRequestLogging()`?** Los
   middlewares corren en orden de registro. Si el ID se guarda primero, la
   línea `Inicio ...` del log ya lo incluye; al revés, el inicio de cada request
   quedaría sin ID.

### F5 · Health checks

1. **¿Qué diferencia hay entre `/health/live` y `/health/ready`?** Live solo
   dice si el proceso está vivo (sirve para decidir si reiniciarlo). Ready dice
   si puede atender requests: verifica la base de datos y los servicios de los
   que depende. Si la base se cae, live sigue `Healthy` y ready no.
2. **¿Por qué un servicio caído da `Degraded` y la base caída da `Unhealthy`?**
   Sin base el servicio no puede hacer nada. Si se cae otro servicio, este
   sigue funcionando para casi todo: Products lista y crea productos aunque
   Orders esté apagado; solo falla el `DELETE`.
3. **¿Por qué el check de una dependencia llama a `/health/live` del otro
   servicio y no a `/health/ready`?** Para no encadenar fallas: con ready,
   cada servicio revisaría también las dependencias de sus dependencias, y una
   sola caída marcaría a todos como degradados.

### S3 · Orders.API

1. **¿Qué pasa si Products.API está caída cuando se crea una orden?**
   `ProductsApiClient` no puede conectarse (o recibe un 5xx) y lanza una
   excepción. No es una excepción de dominio, así que la toma el
   `GlobalExceptionHandler`, que responde 500 con `ORD-007` y la loguea como
   `Error`. La orden no se guarda, porque el guardado está al final. Además,
   `/health/ready` de Orders pasa a `Degraded`.
2. **¿Por qué stock insuficiente es 422 y una transición inválida es 409, y no
   ambos 400?** 400 es para un request mal armado. En los dos casos el request
   está bien formado: 422 dice "entiendo lo que pedís pero no se puede
   procesar" (no hay stock), y 409 dice "choca con el estado actual del
   recurso" (la orden ya está `Entregada`). En el código, los dos son
   `BusinessRuleException` y el status sale del catálogo `ErrorCodes`.
3. **¿Por qué el `precioUnitario` se guarda en la orden en vez de consultarlo
   cada vez?** Porque es el precio en el momento de la compra. Si después
   Products cambia el precio, la orden ya creada no puede cambiar su total.
4. **¿Por qué `GET /api/orders/99` responde 404 y no 400?** La consigna
   permite solo 200, 404 y 500 en ese endpoint, y su propio ejemplo
   (`GET /api/products/99`) responde 404. Un id que no es GUID no puede
   existir, así que se trata como "orden no encontrada" (`ORD-001`).
5. **¿Cómo se evita que un JSON mal formado devuelva un 400 sin `errorCode`?**
   Con `[ApiController]`, ASP.NET Core valida el body antes del controller y
   responde solo. En `ErrorHandlingExtensions` se reemplaza esa respuesta
   (`InvalidModelStateResponseFactory`) por el contrato de la consigna con
   `ORD-002` y todos los problemas separados por `;`.
