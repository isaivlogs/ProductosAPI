using ProductosAPI.Data;
using ProductosAPI.Endpoints;
using ProductosAPI.Repositories;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

// ---------------------------------------------------------------------------
// Inyección de dependencias: le decimos a .NET qué clase entregar cuando
// alguien pida cada interfaz.
// ---------------------------------------------------------------------------

// Conexión: Singleton porque solo guarda la cadena de conexión (no guarda
// conexiones abiertas), así que una sola instancia sirve para toda la app.
builder.Services.AddSingleton<IConexionDb, ConexionDb>();

// Repositorio: Scoped = una instancia por petición HTTP.
// Las dos interfaces apuntan a la misma clase ProductoRepository.
builder.Services.AddScoped<ProductoRepository>();
builder.Services.AddScoped<IProductoRepository>(sp => sp.GetRequiredService<ProductoRepository>());
builder.Services.AddScoped<IProductoBusqueda>(sp => sp.GetRequiredService<ProductoRepository>());

var app = builder.Build();

// Crear base de datos / tabla / datos de ejemplo si hace falta.
// Si MySQL está apagado no tumbamos la API: avisamos en consola y /conexion
// dirá cuál es el error.
try
{
    InicializadorDb.Inicializar(app.Configuration, app.Logger);
}
catch (Exception ex)
{
    app.Logger.LogWarning("No se pudo inicializar la base de datos: {Mensaje}", ex.Message);
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "ProductosAPI v1");
    });
}

// Mensaje de bienvenida para comprobar que la API está viva
app.MapGet("/", () => Results.Ok(new { mensaje = "ProductosAPI funcionando. Documentación en /swagger" }))
    .ExcludeFromDescription();

app.MapConexionEndpoints();
app.MapProductoEndpoints();

app.Run();
