#!/bin/bash
# Generate typed C# API client from the Swagger specification.
#
# Prerequisites:
#   dotnet tool install -g NSwag.ConsoleCore
#
# Usage:
#   ./scripts/generate-api-client.sh
#
# This script:
# 1. Builds the API project
# 2. Runs NSwag to generate a typed C# client
# 3. Output goes to generated/GeorgiaERP.Client/

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ROOT_DIR="$(cd "$SCRIPT_DIR/.." && pwd)"

echo "Building API project..."
dotnet build "$ROOT_DIR/src/GeorgiaERP.Api/GeorgiaERP.Api.csproj" --configuration Release --no-restore

echo "Generating API client..."
mkdir -p "$ROOT_DIR/generated/GeorgiaERP.Client"

cd "$ROOT_DIR/src/GeorgiaERP.Api"
nswag run nswag.json

echo "Client generated successfully at generated/GeorgiaERP.Client/"
echo "  - Client.g.cs      (HTTP client classes)"
echo "  - Contracts.g.cs   (DTOs and model classes)"
