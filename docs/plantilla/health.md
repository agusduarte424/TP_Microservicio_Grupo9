# Health checks

Responsable: Rama (paquete F5).

Cada servicio expone tres endpoints que dicen si está funcionando. Responden JSON con un estado general (`Healthy`, `Degraded` o `Unhealthy`) y el detalle de cada verificación.

Products.API es la implementación de referencia; los demás servicios copian el mismo archivo.

## Los tres endpoints

| Endpoint | Pregunta que responde | Qué verifica |
|---|---|---|
| `GET /health/live` | ¿El proceso está vivo? | Solo que el servicio responde. No mira la base ni otros servicios. |
| `GET /health/ready` | ¿Puede atender requests? | La base de datos y los servicios de los que depende. |
| `GET /health` | Resumen completo | Todos los checks juntos. |

### Por qué live y ready son distintos

- **Live** sirve para saber si hay que reiniciar el proceso. Si mirara la base, una caída de la base haría pensar que hay que reiniciar el servicio, y reiniciarlo no arregla la base.
- **Ready** sirve para saber si conviene mandarle tráfico. Si la base no responde, el servicio está vivo pero no puede trabajar.

## Estados

| Situación | Estado | HTTP |
|---|---|---|
| Todo responde | `Healthy` | 200 |
| Un servicio del que dependo no responde (por ejemplo Orders, para Products) | `Degraded` | 200 |
| La base de datos no responde | `Unhealthy` | 503 |

Una dependencia caída da **Degraded** y no Unhealthy, porque el servicio sigue funcionando para casi todo: Products puede listar y crear productos aunque Orders esté apagado; solo falla el `DELETE`, que necesita consultar órdenes (`PRD-005`).

## Ejemplo de respuesta

`GET http://localhost:5001/health/ready` con Orders apagado:

```json
{
  "status": "Degraded",
  "servicio": "Products.API",
  "duracionMs": 70,
  "checks": [
    { "nombre": "base-de-datos", "status": "Healthy", "descripcion": "La base de datos responde.", "duracionMs": 6 },
    { "nombre": "OrdersApi", "status": "Degraded", "descripcion": "http://localhost:5003 no responde.", "duracionMs": 64 }
  ]
}
```

## Cómo funciona

Todo vive en `Extensions/HealthExtensions.cs`, y `Program.cs` lo usa con dos líneas:

```csharp
builder.Services.AddHealth(builder.Configuration);
...
app.MapHealth();
```

`AddHealth` arma los checks **solo, leyendo `appsettings.json`**:

| Check | Se crea si... | Endpoint |
|---|---|---|
| `proceso` | Siempre. | live |
| `base-de-datos` | Hay `ConnectionStrings:DefaultConnection`. Abre la base y ejecuta `SELECT 1`. | ready |
| Uno por dependencia (`OrdersApi`, `UsersApi`...) | Hay una URL en la sección `Services`. Llama a `/health/live` de ese servicio con un timeout de 2 segundos. | ready |

Cada check tiene una etiqueta (`live` o `ready`), y cada endpoint filtra por etiqueta. `MapHealth` usa un `ResponseWriter` propio para responder JSON en vez del texto plano que trae ASP.NET Core por defecto.

Las dependencias se verifican contra **`/health/live`** del otro servicio, no contra `/ready`: si se usara ready, cada servicio consultaría las dependencias de sus dependencias y una sola caída se propagaría en cadena.

## Cómo replicarlo en otro servicio

1. Copiar `Extensions/HealthExtensions.cs` y cambiar el `namespace`.
2. Agregar las dos líneas en `Program.cs`.
3. No hace falta configurar nada más: toma la base y las dependencias de `appsettings.json`.

## Cómo probarlo

1. Levantar solo Products y abrir `http://localhost:5001/health/ready`: `Degraded`, porque Orders está apagado.
2. Abrir `http://localhost:5001/health/live`: `Healthy`, porque solo mira el proceso.
3. Levantar también Orders y volver a abrir `/health/ready`: `Healthy`.
