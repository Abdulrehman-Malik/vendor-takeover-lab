# Multi-stage build. Build context = repo root (so the shared .proto files are available):
#   docker build -f src/notification-worker/Dockerfile -t localhost:5000/notification-worker:1 .

# ---- Stage 1: build with the full .NET SDK ----
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY global.json Directory.Build.props Directory.Packages.props ./
COPY src/shared-protos/ src/shared-protos/
# Restore first, in its own layer, so it is cached until the .csproj changes
COPY src/notification-worker/Notification.Worker.csproj src/notification-worker/
RUN dotnet restore src/notification-worker/Notification.Worker.csproj
COPY src/notification-worker/ src/notification-worker/
RUN dotnet publish src/notification-worker/Notification.Worker.csproj -c Release -o /app --no-restore

# ---- Stage 2: small runtime-only image ----
FROM mcr.microsoft.com/dotnet/runtime:10.0
WORKDIR /app
COPY --from=build /app .
USER $APP_UID
ENTRYPOINT ["dotnet", "Notification.Worker.dll"]
