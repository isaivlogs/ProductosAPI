using System.Text;
using MySqlConnector;
using ProductosAPI.Data;
using ProductosAPI.Models;

namespace ProductosAPI.Repositories;

// Implementación de IProductoRepository con ADO.NET "puro" (sin Entity
// Framework). Todos los métodos siguen la misma receta:
//
//   1. Pedir una conexión a IConexionDb y abrirla      -> conexion.Open()
//   2. Armar un MySqlCommand con el SQL                -> new MySqlCommand(sql, conexion)
//   3. Pasar los valores como PARÁMETROS (@nombre...)  -> comando.Parameters.AddWithValue
//   4. Ejecutar:
//        - ExecuteReader()   cuando esperamos filas (SELECT)
//        - ExecuteScalar()   cuando esperamos UN valor (el id nuevo)
//        - ExecuteNonQuery() cuando solo importa cuántas filas cambió
//   5. `using` cierra el reader, el comando y la conexión al terminar.
//
// Nunca se concatenan valores del usuario dentro del SQL: siempre van como
// parámetros. Eso evita la inyección SQL.
public class ProductoRepository : IProductoRepository, IProductoBusqueda
{
    private readonly IConexionDb _conexionDb;

    // Columnas que leemos siempre, en el orden en que las espera Mapear().
    private const string Columnas =
        "id, nombre, descripcion, categoria, precio, stock, activo, fecha_registro";

    public ProductoRepository(IConexionDb conexionDb)
    {
        _conexionDb = conexionDb;
    }

    // ------------------------------------------------------------------ CONSULTAS
    public List<Producto> ObtenerTodos()
    {
        var productos = new List<Producto>();

        using var conexion = _conexionDb.CrearConexion();
        conexion.Open();

        using var comando = new MySqlCommand($"SELECT {Columnas} FROM productos ORDER BY id;", conexion);
        using var lector = comando.ExecuteReader();

        // Read() avanza fila por fila; devuelve false cuando ya no hay más.
        while (lector.Read())
        {
            productos.Add(Mapear(lector));
        }

        return productos;
    }

    public Producto? ObtenerPorId(int id)
    {
        using var conexion = _conexionDb.CrearConexion();
        conexion.Open();

        using var comando = new MySqlCommand($"SELECT {Columnas} FROM productos WHERE id = @id;", conexion);
        comando.Parameters.AddWithValue("@id", id);

        using var lector = comando.ExecuteReader();
        return lector.Read() ? Mapear(lector) : null;
    }

    // --------------------------------------------------------------------- INSERT
    public int Insertar(Producto producto)
    {
        using var conexion = _conexionDb.CrearConexion();
        conexion.Open();

        // Dos sentencias en un solo comando: el INSERT y luego LAST_INSERT_ID(),
        // que devuelve el id que MySQL le asignó a la fila en ESTA conexión.
        const string sql = """
            INSERT INTO productos (nombre, descripcion, categoria, precio, stock, activo)
            VALUES (@nombre, @descripcion, @categoria, @precio, @stock, @activo);
            SELECT LAST_INSERT_ID();
            """;

        using var comando = new MySqlCommand(sql, conexion);
        AgregarParametros(comando, producto);

        // ExecuteScalar devuelve la primera columna de la primera fila del
        // último resultado: el id nuevo.
        var idNuevo = Convert.ToInt32(comando.ExecuteScalar());
        producto.Id = idNuevo;
        return idNuevo;
    }

    // ----------------------------------------------------------------- ACTUALIZAR
    public bool Actualizar(Producto producto)
    {
        using var conexion = _conexionDb.CrearConexion();
        conexion.Open();

        const string sql = """
            UPDATE productos
               SET nombre      = @nombre,
                   descripcion = @descripcion,
                   categoria   = @categoria,
                   precio      = @precio,
                   stock       = @stock,
                   activo      = @activo
             WHERE id = @id;
            """;

        using var comando = new MySqlCommand(sql, conexion);
        AgregarParametros(comando, producto);
        comando.Parameters.AddWithValue("@id", producto.Id);

        // ExecuteNonQuery devuelve cuántas filas encontró el WHERE.
        // 0 = ese id no existe.
        return comando.ExecuteNonQuery() > 0;
    }

    // ------------------------------------------------------------------- ELIMINAR
    public bool Eliminar(int id)
    {
        using var conexion = _conexionDb.CrearConexion();
        conexion.Open();

        using var comando = new MySqlCommand("DELETE FROM productos WHERE id = @id;", conexion);
        comando.Parameters.AddWithValue("@id", id);

        return comando.ExecuteNonQuery() > 0;
    }

    // --------------------------------------------------------------------- BUSCAR
    public List<Producto> Buscar(string? nombre, string? categoria, decimal? precioMin, decimal? precioMax, bool? activo)
    {
        var productos = new List<Producto>();

        using var conexion = _conexionDb.CrearConexion();
        conexion.Open();

        using var comando = new MySqlCommand { Connection = conexion };

        // El WHERE se arma según los filtros que sí llegaron. "WHERE 1 = 1"
        // es un truco para poder ir pegando "AND ..." sin preocuparnos de cuál
        // es el primero. Los VALORES siguen yendo como parámetros.
        var sql = new StringBuilder($"SELECT {Columnas} FROM productos WHERE 1 = 1");

        if (!string.IsNullOrWhiteSpace(nombre))
        {
            sql.Append(" AND nombre LIKE CONCAT('%', @nombre, '%')");
            comando.Parameters.AddWithValue("@nombre", nombre.Trim());
        }
        if (!string.IsNullOrWhiteSpace(categoria))
        {
            sql.Append(" AND categoria = @categoria");
            comando.Parameters.AddWithValue("@categoria", categoria.Trim());
        }
        if (precioMin is not null)
        {
            sql.Append(" AND precio >= @precioMin");
            comando.Parameters.AddWithValue("@precioMin", precioMin.Value);
        }
        if (precioMax is not null)
        {
            sql.Append(" AND precio <= @precioMax");
            comando.Parameters.AddWithValue("@precioMax", precioMax.Value);
        }
        if (activo is not null)
        {
            sql.Append(" AND activo = @activo");
            comando.Parameters.AddWithValue("@activo", activo.Value);
        }

        sql.Append(" ORDER BY nombre;");
        comando.CommandText = sql.ToString();

        using var lector = comando.ExecuteReader();
        while (lector.Read())
        {
            productos.Add(Mapear(lector));
        }

        return productos;
    }

    // ------------------------------------------------------------------ AUXILIARES

    // Convierte la fila actual del lector en un objeto Producto.
    // GetOrdinal("columna") busca la posición de la columna por su nombre.
    private static Producto Mapear(MySqlDataReader lector) => new()
    {
        Id            = lector.GetInt32(lector.GetOrdinal("id")),
        Nombre        = lector.GetString(lector.GetOrdinal("nombre")),
        Descripcion   = lector.GetString(lector.GetOrdinal("descripcion")),
        Categoria     = lector.GetString(lector.GetOrdinal("categoria")),
        Precio        = lector.GetDecimal(lector.GetOrdinal("precio")),
        Stock         = lector.GetInt32(lector.GetOrdinal("stock")),
        Activo        = lector.GetBoolean(lector.GetOrdinal("activo")),
        FechaRegistro = lector.GetDateTime(lector.GetOrdinal("fecha_registro")),
    };

    // Los mismos parámetros los usan Insertar y Actualizar.
    private static void AgregarParametros(MySqlCommand comando, Producto producto)
    {
        comando.Parameters.AddWithValue("@nombre", producto.Nombre.Trim());
        comando.Parameters.AddWithValue("@descripcion", producto.Descripcion?.Trim() ?? string.Empty);
        comando.Parameters.AddWithValue("@categoria", producto.Categoria?.Trim() ?? string.Empty);
        comando.Parameters.AddWithValue("@precio", producto.Precio);
        comando.Parameters.AddWithValue("@stock", producto.Stock);
        comando.Parameters.AddWithValue("@activo", producto.Activo);
    }
}
