using MySqlConnector;

namespace ProductosAPI.Data;

// Punto a resolver: CONEXIÓN.
// Quien necesite hablar con la base de datos pide esta interfaz y obtiene una
// conexión nueva (todavía cerrada). Así la cadena de conexión vive en un solo
// lugar y no se repite en cada consulta.
public interface IConexionDb
{
    // Crea un objeto MySqlConnection listo para abrirse con .Open().
    MySqlConnection CrearConexion();

    // Abre y cierra una conexión para comprobar que MySQL responde.
    // Devuelve (true, versión del servidor) o (false, mensaje de error).
    (bool Exito, string Mensaje) ProbarConexion();
}
