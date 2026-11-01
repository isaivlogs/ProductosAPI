# ============================================================================
#  Dockerfile  —  La receta para empaquetar la API en una imagen de Docker
# ============================================================================
#
#  Una "imagen" es como una caja cerrada que lleva dentro la API ya compilada
#  y todo lo que necesita para funcionar. Con ella puedes ejecutar la API en
#  cualquier computadora o servidor que tenga Docker, sin instalar .NET.
#
#  Usamos una "compilación en varias etapas" (multi-stage build):
#      1. BUILD    una imagen grande con el SDK de .NET compila el código.
#      2. FINAL    una imagen pequeña, solo con lo necesario para EJECUTAR,
#                  recibe el resultado. El SDK y el código fuente se quedan
#                  fuera, así la imagen final pesa mucho menos.
#
# ============================================================================


# ────────────────────────────────────────────────────────────────────────────
#  ETAPA 1 — Compilar
# ────────────────────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Primero copiamos SOLO el .csproj y descargamos las librerías (NuGet).
# Docker guarda cada paso en caché: mientras no cambies el .csproj, este paso
# no se repite y las siguientes compilaciones son mucho más rápidas.
COPY ProductosAPI.csproj .
RUN dotnet restore

# Ahora sí, copiamos el resto del código y generamos la versión publicable.
COPY . .
RUN dotnet publish -c Release -o /app/publish --no-restore


# ────────────────────────────────────────────────────────────────────────────
#  ETAPA 2 — Imagen final (solo lo necesario para ejecutar)
# ────────────────────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# Dentro del contenedor la API escucha en el puerto 8080 (el valor por
# defecto de las imágenes oficiales de .NET).
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

# Traemos de la etapa anterior únicamente los archivos compilados.
COPY --from=build /app/publish .

# Por seguridad, no ejecutamos la API como administrador (root) sino con el
# usuario sin privilegios "app" que ya viene en la imagen oficial.
USER app

ENTRYPOINT ["dotnet", "ProductosAPI.dll"]
