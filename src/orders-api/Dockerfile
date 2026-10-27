# Multi-stage build. Build context = repo root (so the shared .proto files are available):
#   docker build -f src/orders-api/Dockerfile -t localhost:5000/orders-api:1 .

# ---- Stage 1: build with the full .NET SDK ----
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY global.json Directory.Build.props Directory.Packages.props ./
COPY src/shared-protos/ src/shared-protos/
# Restore first, in its own layer, so it is cached until the .csproj changes
COPY src/orders-api/Orders.Api.csproj src/orders-api/
RUN dotnet restore src/orders-api/Orders.Api.csproj
COPY src/orders-api/ src/orders-api/
RUN dotnet publish src/orders-api/Orders.Api.csproj -c Release -o /app --no-restore

# ---- Stage 2: small runtime-only image ----
FROM mcr.microsoft.com/dotnet/aspnet:10.0
# curl is only used by the Swarm healthcheck
RUN apt-get update && apt-get install -y --no-install-recommends curl && rm -rf /var/lib/apt/lists/*
WORKDIR /app
COPY --from=build /app .
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "Orders.Api.dll"]
