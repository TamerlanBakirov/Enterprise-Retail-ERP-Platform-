# ---- Build stage ----
FROM mcr.microsoft.com/dotnet/sdk:9.0-alpine AS build
WORKDIR /src

# Copy project files first for better layer caching
COPY GeorgiaERP.Domain/GeorgiaERP.Domain.csproj GeorgiaERP.Domain/
COPY GeorgiaERP.Application/GeorgiaERP.Application.csproj GeorgiaERP.Application/
COPY GeorgiaERP.Infrastructure/GeorgiaERP.Infrastructure.csproj GeorgiaERP.Infrastructure/
COPY GeorgiaERP.Api/GeorgiaERP.Api.csproj GeorgiaERP.Api/
RUN dotnet restore GeorgiaERP.Api/GeorgiaERP.Api.csproj --runtime linux-musl-x64

# Copy source and publish
COPY . .
RUN dotnet publish GeorgiaERP.Api/GeorgiaERP.Api.csproj \
    -c Release \
    -o /app/publish \
    --no-restore \
    --runtime linux-musl-x64 \
    --self-contained false

# ---- Runtime stage ----
FROM mcr.microsoft.com/dotnet/aspnet:9.0-alpine AS runtime
WORKDIR /app

# Install curl for healthcheck
RUN apk add --no-cache curl

# Security: run as non-root user
RUN addgroup -S appgroup && adduser -S appuser -G appgroup
USER appuser

COPY --from=build --chown=appuser:appgroup /app/publish .

ENV ASPNETCORE_URLS=http://+:5000
EXPOSE 5000

HEALTHCHECK --interval=30s --timeout=5s --start-period=15s --retries=3 \
    CMD curl -f http://localhost:5000/health || exit 1

ENTRYPOINT ["dotnet", "GeorgiaERP.Api.dll"]
