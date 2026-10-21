# Orders.API

Responsable: Rama (paquete S3). Puerto: **5003**. Swagger: http://localhost:5003/swagger

## Propósito

Gestiona las órdenes de compra: las crea validando al usuario contra Users.API y cada producto, su stock y su precio contra Products.API, y controla los cambios de estado. **No** modifica el stock de Products ni guarda datos de usuarios o productos: solo los consulta.

## Endpoints

| Método | Ruta | Qué hace | Status posibles |
|---|---|---|---|
| GET | `/api/orders` | Lista las órdenes; con `?usuarioId=` solo las de ese usuario | 200, 500 |
| GET | `/api/orders/{id}` | Detalle de una orden | 200, 404, 500 |
| POST | `/api/orders` | Crea una orden en estado `Pendiente` | 201, 400, 404, 409\*, 422, 500 |
| PUT | `/api/orders/{id}/status` | Cambia el estado | 200, 400, 404, 409, 500 |

\* La consigna lista 409 para el POST, pero crear una orden no puede producir un conflicto de estado: se documenta en Swagger y no se lanza.

Además: `/health`, `/health/live` y `/health/ready` (ver `docs/plantilla/health.md`).

## Recorrido de una request

Ejemplo: `POST /api/orders`.

1. **`CorrelationIdExtensions`** toma el `X-Correlation-Id` o genera uno.
2. **`LoggingExtensions`** escribe `Inicio POST /api/orders`.
3. ASP.NET Core lee el body en un **`CreateOrderRequest`** y valida sus Data Annotations. Si el JSON está roto o falta un dato, responde 400 `ORD-002` sin llegar al controller (`ErrorHandlingExtensions`, `InvalidModelStateResponseFactory`).
4. **`OrdersController.Create`** le pasa el request a **`OrderService.CrearOrden`**.
5. El Service:
   1. Repite las validaciones de datos y agrega las que las anotaciones no ven, como un GUID de ceros (`ORD-002`).
   2. Pregunta a Users.API si existe el usuario, con **`UsersApiClient`** (`ORD-003`).
   3. Suma las cantidades si un producto viene repetido.
   4. Por cada producto, pide sus datos a Products.API con **`ProductsApiClient`** (`ORD-004`) y compara el stock (`ORD-005`).
   5. Toma el precio actual de cada producto, calcula el total y crea la orden en `Pendiente`.
   6. La guarda con **`IOrderRepository.Guardar`**: orden e items en una transacción.
6. El controller responde **201** con la orden y el header `Location`.
7. **`LoggingExtensions`** escribe `Fin POST /api/orders -> 201 en N ms`.

Si en el paso 5 se lanza una excepción, **`UseExceptionHandler`** la pasa por los handlers en orden, y el que la reconoce arma la respuesta con el contrato de errores.

## Archivos clave

| Archivo | Qué hace | Por qué existe |
|---|---|---|
| `Controllers/OrdersController.cs` | Los 4 endpoints, con XML comments y ejemplos de Swagger. | Solo traduce HTTP ↔ Service; no tiene lógica ni try/catch. |
| `Services/OrderService.cs` | Validaciones, orquestación del POST y máquina de estados. | Toda la lógica de negocio en un solo lugar. |
| `Models/Order.cs`, `OrderItem.cs` | Entidades que se guardan. | Separan lo que se guarda de lo que se muestra (DTOs). |
| `Models/EstadosOrden.cs` | Estados, transiciones permitidas y estados activos. | La máquina de estados en un solo lugar, fácil de leer y de cambiar. |
| `DTOs/*.cs` | Requests y responses de la API, con Data Annotations y `<example>`. | El contrato exacto de la consigna, sin exponer el modelo interno. |
| `DTOs/ErrorResponse.cs` | Contrato de errores. | Lo usan las respuestas y Swagger, así no hay dos versiones. |
| `Data/IOrderRepository.cs` y `OrderRepository.cs` | Persistencia con SQLite + Dapper. | El Service no sabe que hay SQLite atrás (patrón de `docs/persistencia.md`). |
| `Data/DatabaseInitializer.cs` | Crea las tablas `orders` y `order_items`. | La base se crea sola al arrancar. |
| `Clients/UsersApiClient.cs`, `ProductsApiClient.cs` | Llamadas HTTP a Users y Products. | El Service no arma URLs ni lee JSON; miran el status antes de leer el body. |
| `Exceptions/ErrorCodes.cs` | Catálogo `ORD-001` a `ORD-007` con status, detail y mensaje. | Nada de códigos escritos a mano; el status sale de un solo lugar. |
| `Exceptions/*Exception.cs` | `NotFoundException`, `BusinessRuleException`, `ValidationException`. | Una excepción por tipo de error, como pide la consigna. |
| `ExceptionHandlers/*.cs` | Un `IExceptionHandler` por excepción y uno genérico. | Arman la respuesta y loguean con el nivel correcto. |
| `Swagger/ErrorExample*.cs` | Atributo y filtro para los ejemplos de error en Swagger. | Cada status muestra su `errorCode` y `errorMessage` reales. |
| `Extensions/*.cs` | Un archivo por bloque: logging, Correlation ID, health, errores, Swagger, persistencia, clientes. | `Program.cs` solo compone, una línea por bloque. |

## Llamadas a otros servicios

| Llama a | Endpoint | Para qué | Si responde 404 | Si falla (5xx, timeout, apagado) |
|---|---|---|---|---|
| Users.API | `GET /api/users/{id}` (interno) | Validar el usuario | 404 `ORD-003` | 500 `ORD-007` |
| Products.API | `GET /api/products/{id}` | Producto, stock y precio | 404 `ORD-004` | 500 `ORD-007` |

Las dos llamadas llevan el header `X-Correlation-Id` (lo agrega `.AddCorrelationIdPropagation()` en `ClientsExtensions`) y tienen un timeout de 5 segundos.

**Quién llama a Orders:** Products.API usa `GET /api/orders` antes de borrar un producto, para saber si aparece en alguna orden `Pendiente` o `Confirmada` (`PRD-004`). Por eso el listado devuelve los items y el estado de cada orden.

## Errores

| errorCode | HTTP | Cuándo se lanza | Dónde |
|---|---|---|---|
| ORD-001 | 404 | La orden no existe, o el id no es un GUID | `OrderService.BuscarOrden` |
| ORD-002 | 400 | JSON inválido; falta `usuarioId` o `items`; lista vacía; cantidad ≤ 0; GUID de ceros; estado que no existe en el PUT | `ErrorHandlingExtensions` (antes del controller), `OrderService.ValidarDatos`, `OrderService.ActualizarEstado` |
| ORD-003 | 404 | Users.API responde 404 para el `usuarioId` | `OrderService.CrearOrden` |
| ORD-004 | 404 | Products.API responde 404 para un producto | `OrderService.CrearOrden` |
| ORD-005 | 422 | La cantidad pedida supera el stock | `OrderService.CrearOrden` |
| ORD-006 | 409 | La transición de estado no está permitida (incluye pedir el mismo estado) | `OrderService.ActualizarEstado` |
| ORD-007 | 500 | Error inesperado: base de datos, o Users/Products caídos o con 5xx | `GlobalExceptionHandler` |

## Máquina de estados

```
Pendiente ──► Confirmada ──► Enviada ──► Entregada
    │             │
    └──► Cancelada ◄┘
```

`Entregada` y `Cancelada` son finales. Cualquier otro cambio da 409 `ORD-006`. Un estado que no existe (por ejemplo `"Volando"`) da 400 `ORD-002`, porque el problema es el dato y no el estado de la orden. El nombre del estado no distingue mayúsculas (`"confirmada"` funciona).

## Decisiones de diseño

- **ORD-005 es 422 y ORD-006 es 409, y los dos son `BusinessRuleException`.** El status sale del catálogo (`ErrorCodes.Catalogo`), no de la excepción. Así no hace falta una clase por status, y la tabla de la consigna está escrita una sola vez.
- **Un id que no es GUID da 404 y no 400.** Sigue el ejemplo de la consigna (`GET /api/products/99` → 404) y respeta los status permitidos en cada endpoint: el GET por id no admite 400. Por eso `id` y `usuarioId` se reciben como texto y el Service los convierte.
- **Validación en dos capas.** Las Data Annotations documentan las reglas en Swagger y cortan antes del controller; el Service las repite y agrega el caso del GUID de ceros (brecha 3 de G17). Los problemas se informan todos juntos, separados por `;`.
- **Productos repetidos se suman.** Dos items del mismo producto se validan contra el stock como una sola cantidad, y quedan como un solo item en la orden.
- **No se descuenta stock en Products.** La consigna no lo pide, y hacerlo obligaría a Products a exponer un endpoint de escritura para Orders y a manejar qué pasa si la orden falla después. Se descartó para no sumar acoplamiento.
- **No se valida si el usuario está bloqueado.** El catálogo no tiene un código para eso; `ORD-003` es solo "no existe".
- **El PUT responde `{id, estado, fechaActualizacion}`.** Es el contrato de la consigna (brecha 2 de G17). `FechaActualizacion` se guarda en la base, pero no se muestra en los GET porque la consigna no la incluye ahí.
- **Clientes HTTP propios en `Clients/`.** El Service pide "¿existe el usuario?" y no sabe de URLs ni de JSON. El repositorio nunca hace HTTP (brecha 7 de G17).
- **Detalle del 500 por entorno.** En Development el `detail` agrega el motivo (por ejemplo "Connection refused"); en Production es genérico. El stack trace nunca sale en la respuesta, solo en el log.

## Cómo probarlo

Para probarlo completo hay que levantar Users (5002), Products (5001) y Orders (5003).

**Caso exitoso:**
1. Crear un producto con stock 2 en Products y registrar un usuario en Users.
2. En `http://localhost:5003/swagger`, `POST /api/orders` con ese usuario y cantidad 1 → **201**, estado `Pendiente`, `precioUnitario` y `total` calculados.
3. `PUT /api/orders/{id}/status` con `{"estado":"Confirmada"}` → **200** con `fechaActualizacion`.

**Casos de error:**
- El mismo POST con cantidad 5 → **422 `ORD-005`**.
- `PUT` con `{"estado":"Pendiente"}` sobre la orden confirmada → **409 `ORD-006`**.
- `GET /api/orders/99` → **404 `ORD-001`**.
- Apagar Products y repetir el POST → **500 `ORD-007`**; `/health/ready` pasa a `Degraded`.

**Trazabilidad:** enviar el POST con el header `X-Correlation-Id: demo-1` y buscar `demo-1` en los logs de Orders, Users y Products (`src/*/logs/*.json`).

Capturas en `docs/capturas/orders-*.png`.

## Preguntas de defensa

Están en `docs/defensa/preguntas.md`, sección Rama › S3.
