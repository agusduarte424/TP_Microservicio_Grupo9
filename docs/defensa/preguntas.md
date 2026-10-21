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
