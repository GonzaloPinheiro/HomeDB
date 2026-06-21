# =============================================================================
# Etapa 1: compilar el frontend
# =============================================================================
FROM node:20-alpine AS frontend
WORKDIR /app
COPY HomeDB_Front/package*.json ./
RUN npm ci
COPY HomeDB_Front/ ./
RUN npm run build

# =============================================================================
# Etapa 2: compilar y publicar la API
# =============================================================================
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["HomeDB/HomeDB/HomeDB.csproj",                                     "HomeDB/"]
COPY ["HomeDB/HomeDB.Application/HomeDB.Application.csproj",             "HomeDB.Application/"]
COPY ["HomeDB/HomeDB.Domain/HomeDB.Domain.csproj",                       "HomeDB.Domain/"]
COPY ["HomeDB/HomeDB.Infrastructure/HomeDB.Infrastructure.csproj",       "HomeDB.Infrastructure/"]

RUN dotnet restore "HomeDB/HomeDB.csproj"

COPY HomeDB/ .
RUN dotnet publish "HomeDB/HomeDB.csproj" \
    -c Release \
    -o /app/publish \
    --no-restore

# =============================================================================
# Etapa 3: imagen final (solo runtime, sin SDK)
# =============================================================================
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

RUN mkdir -p /storage/files

COPY --from=build /app/publish .
COPY --from=frontend /app/dist ./wwwroot

EXPOSE 8080

ENTRYPOINT ["dotnet", "HomeDB.dll"]