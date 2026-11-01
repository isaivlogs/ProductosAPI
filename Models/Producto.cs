namespace ProductosAPI.Models;

// Representa UNA fila de la tabla `productos` en MySQL.
// Cada propiedad corresponde a una columna (mismo nombre, en minúsculas allá).
public class Producto
{
    // Llave primaria. La genera MySQL con AUTO_INCREMENT, por eso al crear
    // un producto no hace falta mandarla.
    public int Id { get; set; }

    public string Nombre { get; set; } = string.Empty;

    public string Descripcion { get; set; } = string.Empty;

    public string Categoria { get; set; } = string.Empty;

    // decimal y no double: con dinero no queremos errores de redondeo.
    // En MySQL es DECIMAL(10,2).
    public decimal Precio { get; set; }

    public int Stock { get; set; }

    // Permite "dar de baja" un producto sin borrarlo.
    public bool Activo { get; set; } = true;

    // La pone MySQL sola (DEFAULT CURRENT_TIMESTAMP) al insertar.
    public DateTime FechaRegistro { get; set; }
}
