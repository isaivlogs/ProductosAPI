using ProductosAPI.Models;
using ProductosAPI.Repositories;

namespace ProductosAPI.Endpoints;

// Aquí se definen las rutas HTTP. Cada endpoint:
//   1. recibe la petición (id en la URL, filtros en el query o el JSON del body),
//   2. valida lo necesario,
//   3. llama al repositorio (que es quien habla con MySQL),
//   4. traduce el resultado a una respuesta HTTP (200, 201, 204, 400, 404).
public static class ProductosEndpoints
{
    public static void MapProductoEndpoints(this WebApplication app)
    {
        var grupo = app.MapGroup("/productos").WithTags("Productos");

        // GET /productos  -> Consultas: todos
        grupo.MapGet("/", (IProductoRepository repo) => Results.Ok(repo.ObtenerTodos()))
            .WithSummary("Lista todos los productos")
            .Produces<List<Producto>>();

        // GET /productos/buscar?nombre=...&categoria=...&precioMin=...&precioMax=...&activo=...
        // Va antes que /{id} solo por orden de lectura; `{id:int}` ya evita que
        // "buscar" se confunda con un id.
        grupo.MapGet("/buscar", (string? nombre, string? categoria, decimal? precioMin,
                                 decimal? precioMax, bool? activo, IProductoBusqueda busqueda) =>
            {
                if (precioMin is not null && precioMax is not null && precioMin > precioMax)
                {
                    return Results.ValidationProblem(new Dictionary<string, string[]>
                    {
                        ["precioMin"] = ["precioMin no puede ser mayor que precioMax."]
                    });
                }

                return Results.Ok(busqueda.Buscar(nombre, categoria, precioMin, precioMax, activo));
            })
            .WithSummary("Busca productos por nombre (parcial), categoría, rango de precio o estado")
            .Produces<List<Producto>>()
            .ProducesValidationProblem();

        // GET /productos/{id}  -> Consultas: uno
        grupo.MapGet("/{id:int}", (int id, IProductoRepository repo) =>
            {
                var producto = repo.ObtenerPorId(id);
                return producto is null
                    ? Results.NotFound(new { mensaje = $"Producto {id} no encontrado" })
                    : Results.Ok(producto);
            })
            .WithSummary("Obtiene un producto por su Id")
            .Produces<Producto>()
            .Produces(StatusCodes.Status404NotFound);

        // POST /productos  -> Insert
        grupo.MapPost("/", (Producto nuevo, IProductoRepository repo) =>
            {
                var errores = Validar(nuevo);
                if (errores.Count > 0) return Results.ValidationProblem(errores);

                var id = repo.Insertar(nuevo);

                // Volvemos a leerlo para regresar la fila completa, con la
                // fecha_registro que puso MySQL.
                var creado = repo.ObtenerPorId(id);
                return Results.Created($"/productos/{id}", creado);
            })
            .WithSummary("Crea un producto (el Id y la fecha los genera MySQL)")
            .Produces<Producto>(StatusCodes.Status201Created)
            .ProducesValidationProblem();

        // PUT /productos/{id}  -> Actualizar
        grupo.MapPut("/{id:int}", (int id, Producto producto, IProductoRepository repo) =>
            {
                var errores = Validar(producto);
                if (errores.Count > 0) return Results.ValidationProblem(errores);

                // El id manda el de la URL, no el que venga en el JSON.
                producto.Id = id;

                return repo.Actualizar(producto)
                    ? Results.NoContent()
                    : Results.NotFound(new { mensaje = $"Producto {id} no encontrado" });
            })
            .WithSummary("Reemplaza los datos de un producto")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound)
            .ProducesValidationProblem();

        // DELETE /productos/{id}  -> Eliminar
        grupo.MapDelete("/{id:int}", (int id, IProductoRepository repo) =>
                repo.Eliminar(id)
                    ? Results.NoContent()
                    : Results.NotFound(new { mensaje = $"Producto {id} no encontrado" }))
            .WithSummary("Elimina un producto")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound);
    }

    // Reglas mínimas antes de tocar la base de datos. Si algo falla se
    // responde 400 con la lista de errores por campo.
    private static Dictionary<string, string[]> Validar(Producto p)
    {
        var errores = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(p.Nombre))
            errores["nombre"] = ["El nombre es obligatorio."];
        else if (p.Nombre.Trim().Length > 100)
            errores["nombre"] = ["El nombre no puede pasar de 100 caracteres."];

        if ((p.Descripcion?.Length ?? 0) > 500)
            errores["descripcion"] = ["La descripción no puede pasar de 500 caracteres."];

        if ((p.Categoria?.Length ?? 0) > 50)
            errores["categoria"] = ["La categoría no puede pasar de 50 caracteres."];

        if (p.Precio < 0 || p.Precio > 99_999_999.99m)
            errores["precio"] = ["El precio debe estar entre 0 y 99,999,999.99."];

        if (p.Stock < 0)
            errores["stock"] = ["El stock no puede ser negativo."];

        return errores;
    }
}
