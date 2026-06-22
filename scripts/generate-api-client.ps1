# Generate typed C# API client from the Swagger specification.
#
# Prerequisites:
#   dotnet tool install -g NSwag.ConsoleCore
#
# Usage:
#   .\scripts\generate-api-client.ps1

$ErrorActionPreference = "Stop"

$RootDir = Split-Path -Parent $PSScriptRoot

Write-Host "Building API project..." -ForegroundColor Cyan
dotnet build "$RootDir\src\GeorgiaERP.Api\GeorgiaERP.Api.csproj" --configuration Release --no-restore

Write-Host "Generating API client..." -ForegroundColor Cyan
$OutputDir = "$RootDir\generated\GeorgiaERP.Client"
if (-not (Test-Path $OutputDir)) {
    New-Item -ItemType Directory -Force $OutputDir | Out-Null
}

Push-Location "$RootDir\src\GeorgiaERP.Api"
try {
    nswag run nswag.json
}
finally {
    Pop-Location
}

Write-Host "Client generated successfully at generated\GeorgiaERP.Client\" -ForegroundColor Green
Write-Host "  - Client.g.cs      (HTTP client classes)"
Write-Host "  - Contracts.g.cs   (DTOs and model classes)"
