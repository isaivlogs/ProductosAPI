using MySqlConnector;

namespace ProductosAPI.Data;

public class ConexionDb : IConexionDb
{
    private readonly string _cadenaConexion;

    // IConfiguration lee appsettings.json (y variables de entorno).
    // Buscamos la sección "ConnectionStrings" -> "MySql".
    public ConexionDb(IConfiguration configuracion)
    {
        _cadenaConexion = configuracion.GetConnectionString("MySql")
            ?? throw new InvalidOperationException(
                "Falta la cadena de conexión 'ConnectionStrings:MySql' en appsettings.json");
    }

    public MySqlConnection CrearConexion()
    {
        // Solo se crea el objeto; NO se abre aquí. Quien la usa la abre y la
        // cierra con `using`. ADO.NET guarda las conexiones en un "pool", así
        // que abrir/cerrar seguido es barato: en realidad se reutilizan.
        return new MySqlConnection(_cadenaConexion);
    }

    public (bool Exito, string Mensaje) ProbarConexion()
    {
        try
        {
            using var conexion = CrearConexion();
            conexion.Open();
            return (true, $"Conectado a MySQL {conexion.ServerVersion}, base de datos '{conexion.Database}'");
        }
        catch (MySqlException ex)
        {
            return (false, ex.Message);
        }
    }
}
