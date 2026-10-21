# TP Microservicios – E-Commerce (.NET 10) · Grupo 9

Trabajo práctico de **Arquitectura y Diseño de Software**: un sistema de
E-Commerce formado por 5 REST APIs independientes en C# / .NET 10.

Integrantes: Agus, Pili y Rama.

## Estructura

```
ECommerce.slnx          solución única con los 5 proyectos
src/
  Products.API/
  Users.API/
  Orders.API/
  Cart.API/
  Notifications.API/
docs/                   arquitectura, errores, persistencia y una guía por servicio
```

## Requisitos

- .NET SDK 10

## Cómo compilar y ejecutar

```bash
git clone https://github.com/agusduarte424/TP_Microservicio_Grupo9.git
cd TP_Microservicio_Grupo9
dotnet build ECommerce.slnx
```

Cada servicio se levanta por separado, en su propia terminal:

```bash
dotnet run --project src/Products.API
dotnet run --project src/Users.API
dotnet run --project src/Orders.API
dotnet run --project src/Cart.API
dotnet run --project src/Notifications.API
```

## Puertos

Los puertos son fijos y no se cambian: las URLs entre servicios se leen de la
sección `Services` de cada `appsettings.json`.

| Servicio | URL | Llama a |
|---|---|---|
| Products.API | http://localhost:5001 | Orders.API |
| Users.API | http://localhost:5002 | — |
| Orders.API | http://localhost:5003 | Users.API, Products.API |
| Cart.API | http://localhost:5004 | Products.API |
| Notifications.API | http://localhost:5005 | Users.API |

## Persistencia

La cátedra no entregó la librería de persistencia, así que el grupo implementa
una propia: SQLite + Dapper, una base por servicio, detrás de una interfaz de
repositorio. Ver [docs/persistencia.md](docs/persistencia.md).
