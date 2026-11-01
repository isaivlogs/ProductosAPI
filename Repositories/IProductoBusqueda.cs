using ProductosAPI.Models;

namespace ProductosAPI.Repositories;

// Punto a resolver: BUSCAR.
// La interfaz de la actividad no trae un método de búsqueda, así que en lugar
// de modificarla la agregamos en otra interfaz aparte. ProductoRepository
// implementa las dos.
public interface IProductoBusqueda
{
    // Todos los filtros son opcionales: los que lleguen en null no se aplican.
    List<Producto> Buscar(
        string? nombre,
        string? categoria,
        decimal? precioMin,
        decimal? precioMax,
        bool? activo);
}
