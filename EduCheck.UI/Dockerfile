# =============================================================================
# Stage 1: Build
# Uses the full Node.js image to install dependencies and build the app.
# This stage is DISCARDED in the final image — node_modules, source code,
# and build tooling never reach production.
# =============================================================================
FROM node:22-alpine AS build

WORKDIR /app

# ---------------------------------------------------------------------------
# Copy ONLY the package files first and install dependencies.
# This layer is cached by Docker as long as package.json and
# package-lock.json don't change — npm ci won't re-run on every code
# change, only when you add/remove packages.
# ---------------------------------------------------------------------------
COPY package.json package-lock.json ./

RUN npm ci

# ---------------------------------------------------------------------------
# Copy the full source code and build in production configuration.
# --configuration production enables:
#   - Tree shaking (removes unused code)
#   - Minification and mangling
#   - AOT compilation
#   - Build optimisations and budget checks
# ---------------------------------------------------------------------------
COPY . .

RUN npm run build -- --configuration production

# =============================================================================
# Stage 2: Runtime
# Uses the minimal Nginx Alpine image — NO Node.js, NO source code,
# NO node_modules. Only the compiled static files and Nginx config.
# Alpine variant keeps the final image under ~25MB.
# =============================================================================
FROM nginx:1.27-alpine AS runtime

# ---------------------------------------------------------------------------
# Security: create a dedicated non-root user and group.
# Running as root gives an attacker root-level access to everything
# the container can reach.
# ---------------------------------------------------------------------------
RUN addgroup --system appgroup \
    && adduser --system --ingroup appgroup --no-create-home appuser \
    && chown -R appuser:appgroup /var/cache/nginx /var/run /var/log/nginx

# ---------------------------------------------------------------------------
# Copy nginx.conf from the build stage (where it was part of the source
# context) rather than directly from the host. This avoids Docker cache
# key issues with files in the runtime stage's build context.
# ---------------------------------------------------------------------------
COPY --from=build /app/nginx.conf /etc/nginx/nginx.conf

# ---------------------------------------------------------------------------
# Copy only the compiled output from the build stage.
# Nothing else (source, node_modules, build tools) is included.
# ---------------------------------------------------------------------------
COPY --from=build /app/dist/EduCheck.UI/browser /usr/share/nginx/html

# ---------------------------------------------------------------------------
# Set the non-root user AFTER copying files (COPY runs as root by default).
# ---------------------------------------------------------------------------
USER appuser

# ---------------------------------------------------------------------------
# Port 8080 avoids the need for root to bind to port 80.
# Nginx is configured to listen on 8080 in nginx.conf.
# ---------------------------------------------------------------------------
EXPOSE 8080

# ---------------------------------------------------------------------------
# Health check: fetches the root page every 30s.
# Docker uses this to determine if the container is healthy before routing
# traffic to it, and to restart it automatically if it becomes unhealthy.
# ---------------------------------------------------------------------------
HEALTHCHECK --interval=30s --timeout=10s --start-period=15s --retries=3 \
    CMD wget --no-verbose --tries=1 --spider http://localhost:8080/ \
    || exit 1

CMD ["nginx", "-g", "daemon off;"]