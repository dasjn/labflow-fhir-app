# ====================
# Stage 1: Build
# ====================
# Usa la imagen oficial de .NET 8 SDK para compilar la aplicación
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

# Establece el directorio de trabajo dentro del contenedor
WORKDIR /src

# Copia SOLO el archivo .csproj primero (optimización de cache de Docker)
# Si solo cambian los archivos .cs, Docker reutiliza esta capa
COPY ["LabFlow.API/LabFlow.API.csproj", "LabFlow.API/"]

# Restaura las dependencias (NuGet packages)
# Esto se cachea si el .csproj no cambia
RUN dotnet restore "LabFlow.API/LabFlow.API.csproj"

# Ahora copia TODO el código fuente
COPY . .

# Compila la aplicación en modo Release
# --no-restore: no volver a restaurar (ya lo hicimos arriba)
# -o /app/build: output a /app/build
WORKDIR /src/LabFlow.API
RUN dotnet build "LabFlow.API.csproj" -c Release -o /app/build --no-restore

# ====================
# Stage 2: Publish
# ====================
# Publica la aplicación (crea los archivos finales optimizados)
FROM build AS publish
RUN dotnet publish "LabFlow.API.csproj" -c Release -o /app/publish /p:UseAppHost=false --no-restore

# ====================
# Stage 3: Runtime
# ====================
# Usa la imagen runtime de .NET 8 (MÁS PEQUEÑA, sin SDK)
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final

# Establece el directorio de trabajo
WORKDIR /app

# Expone el puerto 8080 (ASP.NET Core usa 8080 por defecto en contenedores)
EXPOSE 8080

# Copia los archivos publicados desde el stage 2
COPY --from=publish /app/publish .

# Configura variables de entorno para ASP.NET Core
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Development

# Comando para ejecutar la aplicación
ENTRYPOINT ["dotnet", "LabFlow.API.dll"]
