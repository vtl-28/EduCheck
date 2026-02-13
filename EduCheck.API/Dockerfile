# =============================================================================
# Stage 1: Build
# Uses the full SDK image — this stage is DISCARDED in the final image.
# Source code, SDK tools, and build artifacts never reach production.
# =============================================================================
FROM mcr.microsoft.com/dotnet/sdk:9.0-alpine AS build

WORKDIR /src

# ---------------------------------------------------------------------------
# Copy ONLY the .csproj files first and restore dependencies.
# This layer is cached by Docker as long as no .csproj files change,
# meaning `dotnet restore` won't re-run on every code change — only when
# you add/remove NuGet packages.
# ---------------------------------------------------------------------------
COPY EduCheck.Domain/EduCheck.Domain.csproj             EduCheck.Domain/
COPY EduCheck.Application/EduCheck.Application.csproj   EduCheck.Application/
COPY EduCheck.Infrastructure/EduCheck.Infrastructure.csproj EduCheck.Infrastructure/
COPY EduCheck.API/EduCheck.API.csproj                   EduCheck.API/

RUN dotnet restore EduCheck.API/EduCheck.API.csproj

# ---------------------------------------------------------------------------
# Copy the full source code for the projects the API depends on.
# Tests projects are deliberately excluded — they have no place in
# a production image.
# ---------------------------------------------------------------------------
COPY EduCheck.Domain/       EduCheck.Domain/
COPY EduCheck.Application/  EduCheck.Application/
COPY EduCheck.Infrastructure/ EduCheck.Infrastructure/
COPY EduCheck.API/          EduCheck.API/

# ---------------------------------------------------------------------------
# Publish the API in Release configuration.
# --no-restore: reuses the packages restored above (faster, deterministic).
# --self-contained false: relies on the runtime image (smaller output).
# ---------------------------------------------------------------------------
RUN dotnet publish EduCheck.API/EduCheck.API.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish \
    --self-contained false

# =============================================================================
# Stage 2: Runtime
# Uses the minimal ASP.NET runtime image — NO SDK, NO source code.
# Alpine variant keeps the final image under ~120MB.
# =============================================================================
FROM mcr.microsoft.com/dotnet/aspnet:9.0-alpine AS runtime

# ---------------------------------------------------------------------------
# Security: create a dedicated non-root user and group.
# Running as root inside a container gives an attacker root-level access
# to everything the container can reach. This limits the blast radius.
# ---------------------------------------------------------------------------
RUN addgroup --system appgroup \
    && adduser --system --ingroup appgroup --no-create-home appuser

WORKDIR /app

# ---------------------------------------------------------------------------
# Copy only the published output from the build stage.
# Nothing else from the build stage (source, SDK, obj files) is included.
# ---------------------------------------------------------------------------
COPY --from=build /app/publish .

# ---------------------------------------------------------------------------
# Set the non-root user AFTER copying files (COPY runs as root by default,
# which is fine — we just don't want the process to run as root).
# ---------------------------------------------------------------------------
USER appuser

# ---------------------------------------------------------------------------
# Environment defaults.
# ASPNETCORE_ENVIRONMENT is overridden at runtime via docker-compose or
# the hosting platform — never hardcode "Production" as the only option.
# Port 8080 avoids the need for root to bind to port 80.
# ---------------------------------------------------------------------------
ENV ASPNETCORE_ENVIRONMENT=Production \
    ASPNETCORE_URLS=http://+:8080 \
    DOTNET_RUNNING_IN_CONTAINER=true \
    DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false

EXPOSE 8080

# ---------------------------------------------------------------------------
# Health check: pings the /health endpoint every 30s.
# Docker uses this to determine if the container is healthy before routing
# traffic to it, and to restart it automatically if it becomes unhealthy.
# wget is available on Alpine without installing anything extra.
# ---------------------------------------------------------------------------
HEALTHCHECK --interval=30s --timeout=10s --start-period=20s --retries=3 \
    CMD wget --no-verbose --tries=1 --spider http://localhost:8080/health \
    || exit 1

ENTRYPOINT ["dotnet", "EduCheck.API.dll"]