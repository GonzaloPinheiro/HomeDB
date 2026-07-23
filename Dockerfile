# =============================================================================
# Etapa 1: compilar el frontend
# =============================================================================
FROM node:20-alpine AS frontend
WORKDIR /app
RUN corepack enable

# VITE_API_URL se inlinea en el bundle en build time (shared/env.ts valida con
# Zod al cargar el módulo) - a diferencia del front viejo, que la resolvía en
# runtime. Debe pasarse como build-arg (ver docker-compose.yml).
ARG VITE_API_URL
ENV VITE_API_URL=${VITE_API_URL}

COPY HomeDB_FrontEnd/package.json HomeDB_FrontEnd/pnpm-lock.yaml HomeDB_FrontEnd/pnpm-workspace.yaml ./
RUN pnpm install --frozen-lockfile
COPY HomeDB_FrontEnd/ ./
RUN pnpm run build

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