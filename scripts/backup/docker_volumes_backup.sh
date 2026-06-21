#!/usr/bin/env bash
#
# Docker Volume Backup Script
# Enterprise Retail ERP Platform for Georgia
#
# Creates compressed backups of Docker named volumes used by the ERP platform:
#   - postgres_data: PostgreSQL database files
#   - rabbitmq_data: RabbitMQ persistent data
#   - Redis data (if persistent storage is configured)
#
# This is a complementary backup to pg_dump. Volume-level backups capture
# the full PostgreSQL data directory including WAL files, configuration,
# and any extensions or custom files.
#
# Usage:
#   ./docker_volumes_backup.sh                    # Backup all volumes
#   ./docker_volumes_backup.sh --volume postgres  # Backup only PostgreSQL
#   ./docker_volumes_backup.sh --volume rabbitmq  # Backup only RabbitMQ
#   ./docker_volumes_backup.sh --volume redis     # Backup only Redis
#   ./docker_volumes_backup.sh --stop-services    # Stop services before backup
#
# Environment variables (with defaults):
#   COMPOSE_PROJECT_NAME   (georgia-erp or auto-detected)
#   DOCKER_COMPOSE_DIR     (directory containing docker-compose.yml)
#   VOLUME_BACKUP_DIR      (/var/backups/georgia-erp/volumes)
#   VOLUME_RETENTION_DAYS  (14)
#
# IMPORTANT: For PostgreSQL, prefer pg_backup.sh (logical backup via pg_dump)
# for regular backups. Use this script for:
#   - Full disaster recovery preparation
#   - Before major upgrades
#   - Supplementary volume-level snapshots
#
set -euo pipefail

# =============================================================================
# Configuration
# =============================================================================

COMPOSE_PROJECT_NAME="${COMPOSE_PROJECT_NAME:-}"
DOCKER_COMPOSE_DIR="${DOCKER_COMPOSE_DIR:-}"
VOLUME_BACKUP_DIR="${VOLUME_BACKUP_DIR:-/var/backups/georgia-erp/volumes}"
VOLUME_RETENTION_DAYS="${VOLUME_RETENTION_DAYS:-14}"

TARGET_VOLUME=""
STOP_SERVICES=false

# =============================================================================
# Argument Parsing
# =============================================================================

while [[ $# -gt 0 ]]; do
    case "$1" in
        --volume)
            TARGET_VOLUME="$2"
            shift 2
            ;;
        --stop-services)
            STOP_SERVICES=true
            shift
            ;;
        --help|-h)
            head -35 "$0" | tail -32
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

detect_project_name() {
    if [[ -n "$COMPOSE_PROJECT_NAME" ]]; then
        echo "$COMPOSE_PROJECT_NAME"
        return
    fi

    # Try to detect from running containers
    local project
    project=$(docker ps --format '{{.Labels}}' 2>/dev/null | \
        grep -o 'com.docker.compose.project=[^,]*' | \
        head -1 | cut -d= -f2 || echo "")

    if [[ -z "$project" ]]; then
        # Fall back to directory-based name
        if [[ -n "$DOCKER_COMPOSE_DIR" ]]; then
            project=$(basename "$DOCKER_COMPOSE_DIR" | tr '[:upper:]' '[:lower:]' | tr -cd 'a-z0-9')
        else
            project="georgia-erp"
        fi
    fi

    echo "$project"
}

find_volume() {
    local service_name="$1"
    local project
    project=$(detect_project_name)

    # Common volume naming patterns
    local patterns=(
        "${project}_${service_name}_data"
        "${project}-${service_name}_data"
        "${project}_${service_name}data"
        "${service_name}_data"
    )

    for pattern in "${patterns[@]}"; do
        if docker volume inspect "$pattern" &>/dev/null; then
            echo "$pattern"
            return 0
        fi
    done

    # Search for volumes containing the service name
    local found
    found=$(docker volume ls --format '{{.Name}}' | grep -i "${service_name}" | head -1 || echo "")
    if [[ -n "$found" ]]; then
        echo "$found"
        return 0
    fi

    return 1
}

backup_volume() {
    local volume_name="$1"
    local label="$2"
    local timestamp_str
    timestamp_str=$(date +%Y%m%d_%H%M%S)
    local backup_file="${VOLUME_BACKUP_DIR}/${label}_volume_${timestamp_str}.tar.gz"

    # Verify volume exists
    if ! docker volume inspect "$volume_name" &>/dev/null; then
        log_error "Volume '$volume_name' does not exist"
        return 1
    fi

    # Get volume size (approximate)
    local vol_size
    vol_size=$(docker run --rm -v "${volume_name}:/data:ro" alpine sh -c "du -sh /data 2>/dev/null | cut -f1" 2>/dev/null || echo "unknown")
    log "  Volume: ${volume_name}"
    log "  Size (uncompressed): ~${vol_size}"

    # Create backup using a temporary alpine container
    log "  Creating compressed backup..."
    docker run --rm \
        -v "${volume_name}:/source:ro" \
        -v "${VOLUME_BACKUP_DIR}:/backup" \
        alpine \
        tar czf "/backup/$(basename "$backup_file")" -C /source .

    if [[ ! -f "$backup_file" ]]; then
        log_error "Backup file was not created: $backup_file"
        return 1
    fi

    local compressed_size
    compressed_size=$(du -h "$backup_file" | cut -f1)
    log "  Backup file: $(basename "$backup_file") (${compressed_size})"

    # Generate checksum
    sha256sum "$backup_file" > "${backup_file}.sha256"
    log "  Checksum saved"

    return 0
}

stop_compose_services() {
    if [[ -n "$DOCKER_COMPOSE_DIR" && -f "${DOCKER_COMPOSE_DIR}/docker-compose.yml" ]]; then
        log "Stopping Docker Compose services..."
        docker compose -f "${DOCKER_COMPOSE_DIR}/docker-compose.yml" stop
    else
        log "WARNING: DOCKER_COMPOSE_DIR not set or docker-compose.yml not found"
        log "Cannot stop services automatically. Stop them manually if needed."
    fi
}

start_compose_services() {
    if [[ -n "$DOCKER_COMPOSE_DIR" && -f "${DOCKER_COMPOSE_DIR}/docker-compose.yml" ]]; then
        log "Starting Docker Compose services..."
        docker compose -f "${DOCKER_COMPOSE_DIR}/docker-compose.yml" start
    fi
}

prune_old_backups() {
    local label="$1"
    log "Pruning ${label} volume backups older than ${VOLUME_RETENTION_DAYS} days..."

    local count=0
    while IFS= read -r old_file; do
        log "  Removing: $(basename "$old_file")"
        rm -f "$old_file" "${old_file}.sha256"
        ((count++))
    done < <(find "$VOLUME_BACKUP_DIR" -name "${label}_volume_*.tar.gz" -type f -mtime "+${VOLUME_RETENTION_DAYS}" 2>/dev/null)

    if [[ $count -gt 0 ]]; then
        log "Pruned $count old ${label} volume backup(s)"
    fi
}

# =============================================================================
# Pre-flight Checks
# =============================================================================

if ! command -v docker &>/dev/null; then
    echo "ERROR: docker command not found." >&2
    exit 1
fi

mkdir -p "$VOLUME_BACKUP_DIR"

# =============================================================================
# Main Backup Process
# =============================================================================

STARTED_AT=$(date +%s)

log "=========================================="
log "Docker Volume Backup"
log "  Backup dir: ${VOLUME_BACKUP_DIR}"
log "  Retention:  ${VOLUME_RETENTION_DAYS} days"
log "=========================================="

# Define volumes to backup
declare -A VOLUMES_TO_BACKUP

if [[ -z "$TARGET_VOLUME" || "$TARGET_VOLUME" == "postgres" ]]; then
    PG_VOLUME=$(find_volume "postgres" || echo "")
    if [[ -n "$PG_VOLUME" ]]; then
        VOLUMES_TO_BACKUP["postgres"]="$PG_VOLUME"
    else
        log "WARNING: PostgreSQL volume not found"
    fi
fi

if [[ -z "$TARGET_VOLUME" || "$TARGET_VOLUME" == "rabbitmq" ]]; then
    RMQ_VOLUME=$(find_volume "rabbitmq" || echo "")
    if [[ -n "$RMQ_VOLUME" ]]; then
        VOLUMES_TO_BACKUP["rabbitmq"]="$RMQ_VOLUME"
    else
        log "WARNING: RabbitMQ volume not found"
    fi
fi

if [[ -z "$TARGET_VOLUME" || "$TARGET_VOLUME" == "redis" ]]; then
    REDIS_VOLUME=$(find_volume "redis" || echo "")
    if [[ -n "$REDIS_VOLUME" ]]; then
        VOLUMES_TO_BACKUP["redis"]="$REDIS_VOLUME"
    else
        if [[ "$TARGET_VOLUME" == "redis" ]]; then
            log "WARNING: Redis volume not found"
        fi
    fi
fi

if [[ ${#VOLUMES_TO_BACKUP[@]} -eq 0 ]]; then
    log_error "No volumes found to backup"
    exit 1
fi

log "Volumes to backup:"
for label in "${!VOLUMES_TO_BACKUP[@]}"; do
    log "  ${label}: ${VOLUMES_TO_BACKUP[$label]}"
done

# Optionally stop services for consistent backup
if [[ "$STOP_SERVICES" == true ]]; then
    stop_compose_services
fi

# Backup each volume
FAILURES=0
for label in "${!VOLUMES_TO_BACKUP[@]}"; do
    local_volume="${VOLUMES_TO_BACKUP[$label]}"
    log ""
    log "Backing up ${label} volume..."

    if backup_volume "$local_volume" "$label"; then
        log "${label} volume backup completed"
    else
        log_error "${label} volume backup FAILED"
        ((FAILURES++))
    fi

    # Prune old backups for this volume
    prune_old_backups "$label"
done

# Restart services if we stopped them
if [[ "$STOP_SERVICES" == true ]]; then
    start_compose_services
fi

# Summary
ENDED_AT=$(date +%s)
DURATION=$(( ENDED_AT - STARTED_AT ))

log ""
log "=========================================="
log "Docker Volume Backup Summary"
log "  Volumes backed up: ${#VOLUMES_TO_BACKUP[@]}"
log "  Failures: ${FAILURES}"
log "  Duration: ${DURATION} seconds"
log "  Backup dir: ${VOLUME_BACKUP_DIR}"
log "=========================================="

if [[ $FAILURES -gt 0 ]]; then
    exit 1
fi

exit 0
