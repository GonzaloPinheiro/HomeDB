# =============================================================================
# Etapa 1: compilar y publicar
# =============================================================================
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["HomeDB/HomeDB.csproj",                                     "HomeDB/"]
COPY ["HomeDB.Application/HomeDB.Application.csproj",             "HomeDB.Application/"]
COPY ["HomeDB.Domain/HomeDB.Domain.csproj",                       "HomeDB.Domain/"]
COPY ["HomeDB.Infrastructure/HomeDB.Infrastructure.csproj",       "HomeDB.Infrastructure/"]

RUN dotnet restore "HomeDB/HomeDB.csproj"

COPY . .
RUN dotnet publish "HomeDB/HomeDB.csproj" \
    -c Release \
    -o /app/publish \
    --no-restore

# =============================================================================
# Etapa 2: imagen final (solo runtime, sin SDK)
# =============================================================================
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

RUN mkdir -p /storage/files

COPY --from=build /app/publish .

EXPOSE 8080

ENTRYPOINT ["dotnet", "HomeDB.dll"]