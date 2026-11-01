using ProductosAPI.Data;

namespace ProductosAPI.Endpoints;

public static class ConexionEndpoints
{
    public static void MapConexionEndpoints(this WebApplication app)
    {
        // GET /conexion  -> comprueba que la API sí llega a MySQL.
        // Útil para la evidencia: si responde 200, la cadena de conexión,
        // el usuario y la contraseña están bien.
        app.MapGet("/conexion", (IConexionDb conexionDb) =>
            {
                var (exito, mensaje) = conexionDb.ProbarConexion();
                return exito
                    ? Results.Ok(new { conectado = true, mensaje })
                    : Results.Json(new { conectado = false, mensaje }, statusCode: StatusCodes.Status503ServiceUnavailable);
            })
            .WithTags("Conexión")
            .WithSummary("Prueba la conexión con MySQL");
    }
}
