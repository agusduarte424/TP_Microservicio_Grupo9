# Persistencia

Responsable: Agus (paquete F7).

La cátedra no entregó una librería de persistencia, así que el grupo usa una
propia: **SQLite + Dapper**, con **una base por servicio** detrás de **una
interfaz de repositorio por servicio**. Products.API es la implementación de
referencia; los demás servicios copian el mismo patrón.

## Idea en una frase

El Service nunca habla con la base: le pide cosas a una interfaz
(`IProductRepository`) y la implementación (`ProductRepository`) es la única que
sabe que hay SQLite atrás.

```
Controller ──► Service ──► IProductRepository ──► ProductRepository ──► products.db
                              (interfaz)              (Dapper + SQLite)
```

## Archivos (Products.API)

| Archivo | Qué hace | Por qué existe |
|---|---|---|
| `Models/Product.cs` | Entidad que se guarda y se devuelve. | Es el tipo que viaja entre Service y repositorio. |
| `Data/IProductRepository.cs` | Contrato: `ObtenerTodos`, `ObtenerPorId`, `ExistePorNombreYCategoria`, `Guardar`, `Actualizar`, `Eliminar`. | El Service depende de la interfaz y no de SQLite; se puede cambiar la base o usar un repositorio falso para probar. |
| `Data/ProductRepository.cs` | Implementa la interfaz con Dapper. | Único lugar con SQL. Convierte las filas de la base (texto, REAL) al modelo (`Guid`, `decimal`, `DateTime`). |
| `Data/DatabaseInitializer.cs` | `CREATE TABLE IF NOT EXISTS products`. | La base se crea sola al arrancar; no hay scripts manuales ni archivos `.db` en el repo. |
| `Extensions/PersistenceExtensions.cs` | `AddPersistence()` registra todo en DI; `InitializeDatabase()` crea las tablas. | `Program.cs` solo compone, con una línea por bloque. |

En `Program.cs`:

```csharp
builder.Services.AddPersistence();   // registra IProductRepository → ProductRepository
...
var app = builder.Build();
app.InitializeDatabase();            // crea las tablas si no existen
```

La cadena de conexión sale de `appsettings.json`:

```json
"ConnectionStrings": {
  "DefaultConnection": "Data Source=products.db"
}
```

Si falta, la app no arranca y avisa `Falta ConnectionStrings:DefaultConnection`.
Es preferible a que cree una base en otro lugar sin que nadie se dé cuenta.

## Cómo lo usa un Service

El Service recibe la interfaz por constructor (inyección de dependencias) y
nunca crea conexiones ni escribe SQL:

```csharp
public class ProductService
{
    private readonly IProductRepository _repository;

    public ProductService(IProductRepository repository)
    {
        _repository = repository;
    }

    public async Task<Product> ObtenerPorId(Guid id)
    {
        var product = await _repository.ObtenerPorId(id);
        if (product is null)
            throw new NotFoundException(...);   // PRD-001, lo define F4
        return product;
    }
}
```

Reglas:

1. **Las reglas de negocio van en el Service, no en el repositorio.** El
   repositorio responde preguntas (`¿existe?`, `dame este`) y guarda; decidir que
   un duplicado es `PRD-003` es trabajo del Service.
2. **Ningún repositorio hace llamadas HTTP.** Si hay que consultar otro servicio
   (por ejemplo Cart → Products), lo hace el Service con un `HttpClient`.
3. **Siempre parámetros, nunca concatenar valores en el SQL** (`@Id`,
   `@Nombre`). Evita inyección SQL.
4. **Una conexión por operación** (`using var conn = CreateConnection();`).
   SQLite es un archivo local: abrir es barato y no se comparte entre requests.

## Cómo replicarlo en otro servicio

Ejemplo para Users.API (igual para Orders, Cart y Notifications):

1. **Modelo:** `Models/User.cs` con las propiedades de la consigna.
2. **Interfaz:** `Data/IUserRepository.cs` con solo las operaciones que necesita
   el `UserService` (por ejemplo `ObtenerPorEmail`, `Guardar`, `Actualizar`).
3. **Implementación:** `Data/UserRepository.cs`, copiando la estructura de
   `ProductRepository`: constructor que lee `DefaultConnection`,
   `CreateConnection()`, una clase privada `UserRow` con los tipos de SQLite y
   un `ToUser(row)` que convierte al modelo.
4. **Inicializador:** `Data/DatabaseInitializer.cs` con el `CREATE TABLE IF NOT
   EXISTS` de sus tablas. Si el servicio tiene tablas hijas (por ejemplo los ítems
   de una orden o de un carrito), van en el mismo `Initialize()`.
5. **Extensión:** `Extensions/PersistenceExtensions.cs` cambiando el registro a
   `services.AddScoped<IUserRepository, UserRepository>();`.
6. **Program.cs:** las mismas dos líneas, `AddPersistence()` e
   `InitializeDatabase()`.
7. **appsettings.json:** ya tiene su `DefaultConnection` (`users.db`,
   `orders.db`, `cart.db`, `notifications.db`), definido en F1.

### Tipos en SQLite

SQLite tiene pocos tipos, así que se guarda así:

| En C# | En SQLite | Cómo |
|---|---|---|
| `Guid` | `TEXT` | `id.ToString()` al guardar, `Guid.Parse` al leer |
| `DateTime` | `TEXT` | formato ISO `ToString("o")`, se lee con `DateTimeStyles.RoundtripKind` |
| `decimal` | `REAL` | se guarda como `double` y se convierte a `decimal` al leer |
| `int` | `INTEGER` | Dapper lo lee como `long` en la fila y se convierte a `int` |
| `bool` | `INTEGER` | 0 / 1 |
| `enum` (estado) | `TEXT` | el nombre del valor, para que la base se lea sin código |

## Dónde queda la base

Con `dotnet run --project src/Products.API`, el archivo `products.db` se crea
dentro de `src/Products.API/`. El `.gitignore` ignora `*.db`, así que **nunca se
commitea**: cada integrante tiene su propia base local y se crea vacía la
primera vez. Para empezar de cero alcanza con borrar el archivo y volver a
levantar el servicio.

## Decisiones de diseño

- **SQLite + Dapper y no Entity Framework:** Dapper es SQL explícito, fácil de
  leer y de defender; no hay migraciones ni magia de mapeo. SQLite no necesita
  instalar un servidor.
- **Una base por servicio:** cada microservicio es dueño de sus datos; ningún
  servicio lee la base de otro, se consultan por HTTP.
- **Interfaz por servicio y no un repositorio genérico:** cada servicio expone
  solo las consultas que necesita, con nombres del negocio
  (`ExistePorNombreYCategoria`) en lugar de un CRUD genérico.
- **Diferencia con G17:** en G17 un repositorio (Cart) hacía llamadas HTTP y
  los Services dependían de la clase concreta. Acá los Services dependen de la
  interfaz y los repositorios solo hablan con su base.

## Preguntas de defensa

1. **¿Por qué el Service usa `IProductRepository` y no `ProductRepository`?**
   Para no depender de SQLite: el Service solo conoce el contrato. Se puede
   cambiar la base o usar un repositorio falso en pruebas sin tocar el Service.
2. **¿Qué pasa si borro `products.db`?** Al arrancar, `InitializeDatabase()`
   ejecuta `CREATE TABLE IF NOT EXISTS` y la base se crea vacía. Por eso no se
   versiona.
3. **¿Cómo se evita la inyección SQL?** Todos los valores van como parámetros de
   Dapper (`@Nombre`, `@Id`); el texto del usuario nunca se concatena en el SQL.
