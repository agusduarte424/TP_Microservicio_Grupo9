# Guía de defensa

Responsable: Rama (paquete C3).

La defensa dura **15 a 20 minutos**, con demo en vivo desde Swagger, logs y health checks. **Cada integrante tiene que poder explicar cualquier parte del código**, no solo la suya.

## Reparto del tiempo

| Parte | Minutos | Quién | Qué se muestra |
|---|---|---|---|
| Arquitectura y decisiones | 3 | Rama | Diagrama de `docs/arquitectura.md`, una base por servicio, llamadas HTTP, orden del pipeline |
| Products y Cart | 4 | Agus | CRUD de productos, `PRD-003`, `PRD-004`, carrito con `CRT-003` |
| Users y Notifications | 4 | Pili | Registro, login con bloqueo (`USR-003` → `USR-004`), notificaciones con `NTF-001` |
| Orders y trazabilidad | 4 | Rama | Orden de punta a punta, `ORD-005`, `ORD-006`, mismo Correlation ID en tres servicios |
| Logs y health checks | 3 | Los tres | Archivo JSON de logs, `/health/ready` con un servicio apagado |

Total: 18 minutos, con 2 de margen para preguntas en el medio.

## Antes de empezar (10 minutos antes)

1. `git pull` en `main` y `dotnet build ECommerce.slnx`: 0 errores.
2. Borrar las bases y los logs viejos para arrancar limpio: `src/*/*.db` y `src/*/logs/`.
3. Abrir **5 terminales** y levantar los servicios en este orden (primero los que no dependen de nadie):
   ```
   dotnet run --project src/Products.API
   dotnet run --project src/Users.API
   dotnet run --project src/Cart.API
   dotnet run --project src/Notifications.API
   dotnet run --project src/Orders.API
   ```
4. Abrir en el navegador una pestaña de Swagger por servicio: `http://localhost:5001/swagger` … `5005`.
5. Tener a mano un bloc de notas para copiar los ids (producto, usuario, orden).
6. Revisar `/health` de los 5: todos `Healthy`.

## Guion de la demo

Seguir este orden: cada paso deja creado lo que necesita el siguiente.

| # | Acción | Resultado esperado | Quién |
|---|---|---|---|
| 1 | `POST /api/products` con stock 10 (anotar el id). Repetir el mismo nombre y categoría | 201, después **409 `PRD-003`** | Agus |
| 2 | `POST /api/users/register` (anotar el id). Repetir el email | 201, después **409 `USR-001`** | Pili |
| 3 | `POST /api/users/login` con contraseña incorrecta tres veces | **401 `USR-003`**, **401 `USR-003`** y **403 `USR-004`** | Pili |
| 4 | `POST /api/cart/{userId}/items` con más unidades que el stock | **422 `CRT-003`** | Agus |
| 5 | `POST /api/orders` válida, con header `X-Correlation-Id: demo-defensa` (anotar el id). Repetir con cantidad 50 | 201 `Pendiente`, después **422 `ORD-005`** | Rama |
| 6 | `PUT /api/orders/{id}/status` a `Confirmada`, y después a `Pendiente` | 200 con `fechaActualizacion`, después **409 `ORD-006`** | Rama |
| 7 | `DELETE /api/products/{id}` del producto de la orden | **409 `PRD-004`** | Agus |
| 8 | `POST /api/notifications/send` al usuario; `GET` de sus notificaciones; `POST` a un usuario inexistente | 201, 200, **404 `NTF-001`** | Pili |
| 9 | Buscar `demo-defensa` en los logs de Orders, Users y Products | La misma línea de Correlation ID en los tres servicios | Rama |
| 10 | Apagar Products (Ctrl+C) y abrir `/health/ready` de Orders y Cart | `Degraded`; `/health/live` sigue `Healthy` | Los tres |

> En el paso 3, el usuario queda bloqueado. Para el resto de la demo usar otro usuario o registrar uno nuevo.

**Dónde mostrar los logs (paso 9):** abrir `src/Orders.API/logs/orders-AAAAMMDD.json` y buscar `demo-defensa`, y hacer lo mismo en Products y Users. En la consola también se ve, en la segunda columna de cada línea.

## Qué decir en la parte de arquitectura (Rama, 3 minutos)

1. **Cinco servicios, cinco bases.** Ninguno lee la base de otro; si necesita un dato, lo pide por HTTP.
2. **Quién llama a quién** (tabla de `docs/arquitectura.md`), y la regla: un 404 del otro servicio se traduce al error propio (`ORD-003`), y cualquier otra falla es un 500 propio (`ORD-007`).
3. **Todos los servicios tienen la misma estructura**, con Products como plantilla: Controller → Service → Repository. Los bloques transversales van uno por archivo en `Extensions/`.
4. **El orden del pipeline**: Correlation ID, después logging, después errores. Explicar en una frase por qué el logging va antes que el handler.
5. **Persistencia propia**: la cátedra no entregó la librería, así que usamos SQLite + Dapper detrás de una interfaz, que es lo mismo que pide la consigna.

## Ensayos

| Fecha | Qué se hace |
|---|---|
| Sáb 7/11 | Ensayo completo con cronómetro. Después, **explicación cruzada**: se sortea un archivo o un endpoint y lo explica quien **no** lo escribió; el dueño corrige. |
| Dom 8/11 | Segundo ensayo completo. Repasar las preguntas de `preguntas.md` de a dos: uno pregunta, el otro responde sin mirar. |
| Lun 9/11 | Clonar el repo limpio en otra computadora, levantar todo **solo con el README** y correr la demo una vez. |

Para practicar solo: pedirle a Claude "Tomame la defensa del servicio X: haceme 10 preguntas de a una, esperá mi respuesta y corregime".

## Preguntas probables

Todas tienen respuesta en `docs/defensa/preguntas.md` o en la guía de cada servicio (`docs/servicios/`).

**Generales (las puede hacer a cualquiera):**
- ¿Por qué `IExceptionHandler` y no un middleware propio? ¿Por qué importa el orden en que se registran los handlers?
- ¿Cómo viaja el Correlation ID de Orders a Products y dónde se ve en los logs?
- ¿Qué diferencia hay entre `/health/live` y `/health/ready`?
- ¿Por qué stock insuficiente es 422 y una transición inválida es 409, y no ambos 400?
- ¿Qué pasa si Products.API está caída cuando se crea una orden?
- ¿Cómo se evita mostrar un stack trace en una respuesta de error? ¿Qué cambia entre Development y Production?
- ¿Por qué cada servicio tiene su propia base?
- ¿Para qué sirve la interfaz del repositorio si hay una sola implementación?

**Por servicio:**
- Users: ¿cómo y cuándo se bloquea un usuario? ¿Qué distingue `USR-004` de `USR-005`? ¿Cómo se guarda la contraseña?
- Products: ¿qué hace el `DELETE` si Orders no responde?
- Cart: ¿por qué Cart no valida el usuario contra Users?
- Orders: ¿por qué el precio unitario se guarda en la orden? ¿Qué estados existen y cuáles son finales?
- Notifications: ¿qué tipos de notificación se aceptan y qué pasa con uno desconocido?

## Si algo falla durante la demo

- **Un servicio no levanta**: revisar que no haya otro proceso usando el puerto (cerrar terminales viejas) y volver a correr `dotnet run`.
- **Error 500 inesperado**: abrir el log JSON del servicio y buscar la línea `ERR` con el Correlation ID de la respuesta. Mostrar eso también suma: es justamente para lo que sirve.
- **Datos que no coinciden**: borrar el `.db` del servicio y reiniciarlo; las tablas se crean solas.
