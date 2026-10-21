namespace Orders.API.Models
{
    /// <summary>
    /// Estados posibles de una orden y los cambios permitidos entre ellos (máquina de estados).
    /// </summary>
    /// <remarks>
    /// Pendiente ──► Confirmada ──► Enviada ──► Entregada
    ///     │             │
    ///     └──► Cancelada ◄┘
    /// Entregada y Cancelada son estados finales: no pasan a ningún otro.
    /// </remarks>
    public static class EstadosOrden
    {
        public const string Pendiente = "Pendiente";
        public const string Confirmada = "Confirmada";
        public const string Enviada = "Enviada";
        public const string Entregada = "Entregada";
        public const string Cancelada = "Cancelada";

        /// <summary>Todos los estados válidos, en el orden en que avanza una orden.</summary>
        public static readonly IReadOnlyList<string> Todos =
            [Pendiente, Confirmada, Enviada, Entregada, Cancelada];

        /// <summary>
        /// Estados que cuentan como orden activa: Products.API no deja borrar un producto
        /// que aparece en una orden en alguno de estos estados (PRD-004).
        /// </summary>
        public static readonly IReadOnlyList<string> Activos = [Pendiente, Confirmada];

        // Para cada estado, a qué estados puede pasar.
        private static readonly Dictionary<string, string[]> Transiciones = new()
        {
            [Pendiente] = [Confirmada, Cancelada],
            [Confirmada] = [Enviada, Cancelada],
            [Enviada] = [Entregada],
            [Entregada] = [],
            [Cancelada] = []
        };

        /// <summary>
        /// Devuelve el nombre oficial del estado (ignorando mayúsculas), o null si no existe.
        /// Por ejemplo "confirmada" → "Confirmada".
        /// </summary>
        public static string? Normalizar(string? estado) =>
            Todos.FirstOrDefault(e => string.Equals(e, estado?.Trim(), StringComparison.OrdinalIgnoreCase));

        /// <summary>Indica si una orden puede pasar del estado actual al nuevo.</summary>
        public static bool PuedeCambiar(string actual, string nuevo) =>
            Transiciones.TryGetValue(actual, out var permitidos) && permitidos.Contains(nuevo);
    }
}
