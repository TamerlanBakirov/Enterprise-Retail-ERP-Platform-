#!/usr/bin/env bash
#
# RabbitMQ Definitions Backup Script
# Enterprise Retail ERP Platform for Georgia
#
# Exports RabbitMQ definitions (queues, exchanges, bindings, users, vhosts,
# policies, and parameters) via the Management HTTP API.
#
# Usage:
#   ./rabbitmq_backup.sh
#   ./rabbitmq_backup.sh --container georgia-erp-mq
#
# Environment variables (with defaults):
#   RABBITMQ_HOST       (localhost)
#   RABBITMQ_PORT       (15672) - Management API port
#   RABBITMQ_USER       (erp_user)
#   RABBITMQ_PASS       (required, no default)
#   RABBITMQ_BACKUP_DIR (/var/backups/georgia-erp/rabbitmq)
#   RABBITMQ_RETENTION  (30)  - days to keep backups
#
# Backup includes:
#   - All queue definitions
#   - Exchange declarations
#   - Bindings
#   - Users and permissions
#   - Virtual hosts
#   - Policies and parameters
#
# Restore:
#   curl -u user:pass -X POST -H "Content-Type: application/json" \
#     -d @definitions.json http://localhost:15672/api/definitions
#
set -euo pipefail

# =============================================================================
# Configuration
# =============================================================================

RABBITMQ_HOST="${RABBITMQ_HOST:-localhost}"
RABBITMQ_PORT="${RABBITMQ_PORT:-15672}"
RABBITMQ_USER="${RABBITMQ_USER:-erp_user}"
RABBITMQ_PASS="${RABBITMQ_PASS:-}"
RABBITMQ_BACKUP_DIR="${RABBITMQ_BACKUP_DIR:-/var/backups/georgia-erp/rabbitmq}"
RABBITMQ_RETENTION="${RABBITMQ_RETENTION:-30}"

USE_CONTAINER=""

# =============================================================================
# Argument Parsing
# =============================================================================

while [[ $# -gt 0 ]]; do
    case "$1" in
        --container)
            USE_CONTAINER="$2"
            shift 2
            ;;
        --help|-h)
            head -34 "$0" | tail -31
            exit 0
            ;;
        *)
            echo "Unknown argument: $1" >&2
            exit 1
            ;;
    esac
done

# =============================================================================
# Functions
# =============================================================================

timestamp() {
    date '+%Y-%m-%d %H:%M:%S'
}

log() {
    echo "[$(timestamp)] $1"
}

log_error() {
    echo "[$(timestamp)] ERROR: $1" >&2
}

export_via_api() {
    local url="http://${RABBITMQ_HOST}:${RABBITMQ_PORT}/api/definitions"
    local outfile="$1"

    log "Exporting definitions via Management API: $url"

    local http_code
    http_code=$(curl -sS -w "%{http_code}" -o "$outfile" \
        -u "${RABBITMQ_USER}:${RABBITMQ_PASS}" \
        "$url")

    if [[ "$http_code" -ne 200 ]]; then
        log_error "API request failed with HTTP status: $http_code"
        rm -f "$outfile"
        return 1
    fi

    # Validate JSON
    if command -v python3 &>/dev/null; then
        if ! python3 -m json.tool "$outfile" > /dev/null 2>&1; then
            log_error "Exported file is not valid JSON"
            return 1
        fi
    elif command -v jq &>/dev/null; then
        if ! jq empty "$outfile" 2>/dev/null; then
            log_error "Exported file is not valid JSON"
            return 1
        fi
    fi

    return 0
}

export_via_container() {
    local container="$1"
    local outfile="$2"

    log "Exporting definitions via Docker container: $container"

    # Check container is running
    if ! docker inspect --format='{{.State.Running}}' "$container" 2>/dev/null | grep -q "true"; then
        log_error "Container '$container' is not running"
        return 1
    fi

    # Export using rabbitmqctl inside the container
    docker exec "$container" rabbitmqctl export_definitions - > "$outfile" 2>/dev/null

    if [[ $? -ne 0 || ! -s "$outfile" ]]; then
        log_error "Failed to export definitions from container"
        rm -f "$outfile"
        return 1
    fi

    return 0
}

# =============================================================================
# Pre-flight Checks
# =============================================================================

if [[ -z "$USE_CONTAINER" && -z "$RABBITMQ_PASS" ]]; then
    echo "ERROR: RABBITMQ_PASS is not set and --container not specified." >&2
    echo "Set RABBITMQ_PASS or use --container <name>." >&2
    exit 1
fi

mkdir -p "$RABBITMQ_BACKUP_DIR"

# =============================================================================
# Main Backup Process
# =============================================================================

TIMESTAMP=$(date +%Y%m%d_%H%M%S)
DEFINITIONS_FILE="${RABBITMQ_BACKUP_DIR}/rabbitmq_definitions_${TIMESTAMP}.json"
COMPRESSED_FILE="${DEFINITIONS_FILE}.gz"

log "=========================================="
log "RabbitMQ Definitions Backup"
log "  Host:      ${RABBITMQ_HOST}:${RABBITMQ_PORT}"
log "  User:      ${RABBITMQ_USER}"
log "  Output:    ${DEFINITIONS_FILE}"
log "  Retention: ${RABBITMQ_RETENTION} days"
log "=========================================="

# Export definitions
if [[ -n "$USE_CONTAINER" ]]; then
    if ! export_via_container "$USE_CONTAINER" "$DEFINITIONS_FILE"; then
        exit 1
    fi
else
    if ! export_via_api "$DEFINITIONS_FILE"; then
        exit 1
    fi
fi

# Show summary of exported definitions
if command -v python3 &>/dev/null; then
    log "Definitions summary:"
    python3 -c "
import json, sys
with open('${DEFINITIONS_FILE}') as f:
    d = json.load(f)
for key in ['queues', 'exchanges', 'bindings', 'users', 'vhosts', 'policies', 'parameters']:
    if key in d:
        print(f'  {key}: {len(d[key])}')
" 2>/dev/null || true
elif command -v jq &>/dev/null; then
    log "Definitions summary:"
    for key in queues exchanges bindings users vhosts policies parameters; do
        count=$(jq ".$key | length" "$DEFINITIONS_FILE" 2>/dev/null || echo "N/A")
        echo "  ${key}: ${count}"
    done
fi

# Compress
log "Compressing backup..."
gzip -f "$DEFINITIONS_FILE"
BACKUP_SIZE=$(du -h "$COMPRESSED_FILE" | cut -f1)
log "Compressed backup: ${COMPRESSED_FILE} (${BACKUP_SIZE})"

# Generate checksum
sha256sum "$COMPRESSED_FILE" > "${COMPRESSED_FILE}.sha256"

# Prune old backups
log "Pruning backups older than ${RABBITMQ_RETENTION} days..."
PRUNED=0
while IFS= read -r old_file; do
    log "  Removing: $(basename "$old_file")"
    rm -f "$old_file" "${old_file}.sha256"
    ((PRUNED++))
done < <(find "$RABBITMQ_BACKUP_DIR" -name "rabbitmq_definitions_*.json.gz" -type f -mtime "+${RABBITMQ_RETENTION}" 2>/dev/null)

if [[ $PRUNED -gt 0 ]]; then
    log "Pruned $PRUNED old backup(s)"
fi

log "=========================================="
log "RabbitMQ Backup Completed"
log "  File: $(basename "$COMPRESSED_FILE")"
log "  Size: ${BACKUP_SIZE}"
log "=========================================="

exit 0
