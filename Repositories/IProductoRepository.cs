using ProductosAPI.Models;

namespace ProductosAPI.Repositories;

// Interfaz que pide la actividad, tal cual.
// Un "repositorio" es la única clase que sabe hablar con la base de datos:
// los endpoints no escriben SQL, solo llaman a estos métodos.
public interface IProductoRepository
{
    List<Producto> ObtenerTodos();          // Consultas (SELECT de todo)
    Producto? ObtenerPorId(int id);         // Consultas (SELECT por id)
    int Insertar(Producto producto);        // Insert   -> devuelve el id nuevo
    bool Actualizar(Producto producto);     // Actualizar -> true si existía
    bool Eliminar(int id);                  // Eliminar  -> true si existía
}
