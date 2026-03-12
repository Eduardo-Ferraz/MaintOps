# ─── Stage 1: Build ────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution & project files first for layer caching.
COPY MaintOps.sln ./
COPY Industriall.MaintOps.Api/Industriall.MaintOps.Api.csproj Industriall.MaintOps.Api/

# Restore NuGet packages (cached unless .csproj changes).
RUN dotnet restore Industriall.MaintOps.Api/Industriall.MaintOps.Api.csproj

# Copy remaining source.
COPY Industriall.MaintOps.Api/ Industriall.MaintOps.Api/

# Publish in Release mode to /app/publish.
RUN dotnet publish Industriall.MaintOps.Api/Industriall.MaintOps.Api.csproj \
    -c Release \
    -o /app/publish \
    --no-restore

# ─── Stage 2: Runtime ──────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS runtime
WORKDIR /app

# Install culture data required by Npgsql (optional but avoids runtime warnings).
RUN apk add --no-cache icu-libs
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false

# Copy published output from build stage.
COPY --from=build /app/publish .

# Run as non-root for security (built-in 'app' user in the .NET Alpine image).
USER app

EXPOSE 8080

# Health check that pings the /health endpoint every 30 s.
HEALTHCHECK --interval=30s --timeout=5s --start-period=15s --retries=3 \
    CMD wget -qO- http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "Industriall.MaintOps.Api.dll"]
