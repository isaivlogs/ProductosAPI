using MySqlConnector;

namespace ProductosAPI.Data;

// Se ejecuta una sola vez al arrancar la API (lo llama Program.cs).
// Crea la base de datos y la tabla si todavía no existen y mete unos productos
// de ejemplo si la tabla está vacía. Así basta con tener MySQL encendido para
// que todo funcione, sin correr scripts a mano.
// (El mismo SQL está en Database/productos.sql por si prefieres hacerlo en
// MySQL Workbench.)
public static class InicializadorDb
{
    public static void Inicializar(IConfiguration configuracion, ILogger logger)
    {
        var cadena = configuracion.GetConnectionString("MySql")
            ?? throw new InvalidOperationException("Falta 'ConnectionStrings:MySql'");

        // MySqlConnectionStringBuilder separa la cadena en partes.
        var builder = new MySqlConnectionStringBuilder(cadena);
        var nombreBd = builder.Database;

        // 1) Conectarse al SERVIDOR (sin base de datos) para poder crearla.
        builder.Database = string.Empty;
        using (var conexionServidor = new MySqlConnection(builder.ConnectionString))
        {
            conexionServidor.Open();
            // El nombre de una BD no se puede mandar como parámetro (@x), por
            // eso va entre `comillas invertidas`. Viene de nuestra config, no
            // del usuario, así que no hay riesgo de inyección SQL.
            using var crearBd = new MySqlCommand(
                $"CREATE DATABASE IF NOT EXISTS `{nombreBd}` CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;",
                conexionServidor);
            crearBd.ExecuteNonQuery();
        }

        // 2) Ya con la BD, crear la tabla y los datos de ejemplo.
        using var conexion = new MySqlConnection(cadena);
        conexion.Open();

        using (var crearTabla = new MySqlCommand(SqlCrearTabla, conexion))
        {
            crearTabla.ExecuteNonQuery();
        }

        using var contar = new MySqlCommand("SELECT COUNT(*) FROM productos;", conexion);
        var total = Convert.ToInt32(contar.ExecuteScalar());

        if (total == 0)
        {
            using var sembrar = new MySqlCommand(SqlDatosEjemplo, conexion);
            var filas = sembrar.ExecuteNonQuery();
            logger.LogInformation("Tabla 'productos' creada con {Filas} productos de ejemplo", filas);
        }
        else
        {
            logger.LogInformation("Base de datos '{Bd}' lista ({Total} productos)", nombreBd, total);
        }
    }

    private const string SqlCrearTabla = """
        CREATE TABLE IF NOT EXISTS productos (
            id              INT            NOT NULL AUTO_INCREMENT,
            nombre          VARCHAR(100)   NOT NULL,
            descripcion     VARCHAR(500)   NOT NULL DEFAULT '',
            categoria       VARCHAR(50)    NOT NULL DEFAULT '',
            precio          DECIMAL(10,2)  NOT NULL DEFAULT 0,
            stock           INT            NOT NULL DEFAULT 0,
            activo          TINYINT(1)     NOT NULL DEFAULT 1,
            fecha_registro  DATETIME       NOT NULL DEFAULT CURRENT_TIMESTAMP,
            PRIMARY KEY (id)
        );
        """;

    private const string SqlDatosEjemplo = """
        INSERT INTO productos (nombre, descripcion, categoria, precio, stock, activo) VALUES
            ('Audífonos inalámbricos', 'Bluetooth 5.3 con cancelación de ruido', 'Audio',      899.00, 25, 1),
            ('Teclado mecánico',       'Switches rojos, retroiluminado RGB',     'Periféricos', 1249.50, 12, 1),
            ('Mouse gamer',            '16000 DPI, 7 botones programables',      'Periféricos',  549.90, 40, 1),
            ('Monitor 27 pulgadas',    'IPS 2K a 165 Hz',                        'Pantallas',   5299.00,  6, 1),
            ('Cable USB-C',            'Carga rápida 100 W, 2 metros',           'Accesorios',   189.00,  0, 0);
        """;
}
