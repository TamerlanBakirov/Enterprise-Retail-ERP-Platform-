#!/usr/bin/env bash
#
# PostgreSQL Restore Script
# Enterprise Retail ERP Platform for Georgia
#
# Restores a database from a pg_dump custom-format backup file.
# Includes safety checks, validation, and pre-restore confirmation.
#
# Usage:
#   ./pg_restore.sh <backup_file>
#   ./pg_restore.sh <backup_file> --target-db georgia_erp_restored
#   ./pg_restore.sh <backup_file> --no-confirm
#   ./pg_restore.sh <backup_file> --download-s3 s3://bucket/path/to/backup.dump
#   ./pg_restore.sh --list-backups
#   ./pg_restore.sh --list-backups --type weekly
#
# Environment variables (with defaults):
#   PGHOST              (localhost)
#   PGPORT              (5432)
#   PGUSER              (erp_user)
#   PGDATABASE          (georgia_erp)
#   PGPASSWORD          (required, no default)
#   BACKUP_DIR          (/var/backups/georgia-erp/postgres)
#
# S3 variables (required for --download-s3):
#   S3_BUCKET           - S3 bucket name
#   S3_ENDPOINT         - S3 endpoint URL
#   S3_ACCESS_KEY       - S3 access key
#   S3_SECRET_KEY       - S3 secret key
#   S3_REGION           - S3 region (default: us-east-1)
#
set -euo pipefail

# =============================================================================
# Configuration
# =============================================================================

PGHOST="${PGHOST:-localhost}"
PGPORT="${PGPORT:-5432}"
PGUSER="${PGUSER:-erp_user}"
PGDATABASE="${PGDATABASE:-georgia_erp}"
BACKUP_DIR="${BACKUP_DIR:-/var/backups/georgia-erp/postgres}"
S3_REGION="${S3_REGION:-us-east-1}"

TARGET_DB=""
NO_CONFIRM=false
DOWNLOAD_S3=""
LIST_BACKUPS=false
LIST_TYPE=""
BACKUP_FILE=""

# =============================================================================
# Argument Parsing
# =============================================================================

while [[ $# -gt 0 ]]; do
    case "$1" in
        --target-db)
            TARGET_DB="$2"
            shift 2
            ;;
        --no-confirm)
            NO_CONFIRM=true
            shift
            ;;
        --download-s3)
            DOWNLOAD_S3="$2"
            shift 2
            ;;
        --list-backups)
            LIST_BACKUPS=true
            shift
            ;;
        --type)
            LIST_TYPE="$2"
            shift 2
            ;;
        --help|-h)
            head -32 "$0" | tail -29
            exit 0
            ;;
        -*)
            echo "Unknown option: $1" >&2
            exit 1
            ;;
        *)
            BACKUP_FILE="$1"
            shift
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

list_available_backups() {
    local search_dir="$BACKUP_DIR"
    if [[ -n "$LIST_TYPE" ]]; then
        search_dir="${BACKUP_DIR}/${LIST_TYPE}"
    fi

    if [[ ! -d "$search_dir" ]]; then
        echo "No backups found in: $search_dir"
        exit 0
    fi

    echo "Available backups in: $search_dir"
    echo "============================================"
    echo ""
    printf "%-50s  %10s  %s\n" "Filename" "Size" "Modified"
    printf "%-50s  %10s  %s\n" "--------" "----" "--------"

    find "$search_dir" -name "*.dump" -type f -print0 2>/dev/null | \
        xargs -0 ls -lht 2>/dev/null | \
        awk '{printf "%-50s  %10s  %s %s %s\n", $NF, $5, $6, $7, $8}' || \
        echo "No backup files found."

    echo ""
    echo "Total backups:"
    for subdir in daily weekly monthly; do
        local dir="${BACKUP_DIR}/${subdir}"
        if [[ -d "$dir" ]]; then
            local count
            count=$(find "$dir" -name "*.dump" -type f 2>/dev/null | wc -l)
            echo "  ${subdir}: ${count}"
        fi
    done
}

download_from_s3() {
    local s3_path="$1"
    local local_path="$2"

    if [[ -n "${S3_ACCESS_KEY:-}" && -n "${S3_SECRET_KEY:-}" ]]; then
        export AWS_ACCESS_KEY_ID="$S3_ACCESS_KEY"
        export AWS_SECRET_ACCESS_KEY="$S3_SECRET_KEY"
    fi
    export AWS_DEFAULT_REGION="$S3_REGION"

    local endpoint_flag=""
    if [[ -n "${S3_ENDPOINT:-}" ]]; then
        endpoint_flag="--endpoint-url $S3_ENDPOINT"
    fi

    log "Downloading backup from S3: $s3_path"
    # shellcheck disable=SC2086
    aws s3 cp "$s3_path" "$local_path" $endpoint_flag
    log "Download complete: $local_path"
}

validate_backup_file() {
    local file="$1"

    # Check file exists
    if [[ ! -f "$file" ]]; then
        log_error "Backup file not found: $file"
        return 1
    fi

    # Check file is not empty
    local file_size
    file_size=$(stat -f%z "$file" 2>/dev/null || stat -c%s "$file" 2>/dev/null || echo "0")
    if [[ "$file_size" -eq 0 ]]; then
        log_error "Backup file is empty: $file"
        return 1
    fi

    # Check checksum if available
    if [[ -f "${file}.sha256" ]]; then
        log "Verifying SHA256 checksum..."
        if sha256sum --check "${file}.sha256" &>/dev/null; then
            log "Checksum verification passed"
        else
            log_error "Checksum verification FAILED - backup may be corrupted"
            return 1
        fi
    else
        log "No checksum file found, skipping checksum verification"
    fi

    # Verify pg_restore can read it
    log "Verifying backup file integrity..."
    if pg_restore --list "$file" > /dev/null 2>&1; then
        local object_count
        object_count=$(pg_restore --list "$file" 2>/dev/null | grep -c "^[0-9]" || echo "0")
        log "Backup validation passed: $object_count database objects found"
        log "Backup file size: $(du -h "$file" | cut -f1)"
        return 0
    else
        log_error "pg_restore cannot read the backup file - it may be corrupted"
        return 1
    fi
}

get_db_info() {
    local db="$1"

    log "Current database info for '${db}':"

    # Check if database exists
    local db_exists
    db_exists=$(psql -h "$PGHOST" -p "$PGPORT" -U "$PGUSER" -d postgres \
        -tAc "SELECT 1 FROM pg_database WHERE datname='${db}'" 2>/dev/null || echo "")

    if [[ "$db_exists" != "1" ]]; then
        log "  Database '${db}' does not exist (will be created)"
        return 0
    fi

    # Get table count and approximate size
    local table_count
    table_count=$(psql -h "$PGHOST" -p "$PGPORT" -U "$PGUSER" -d "$db" \
        -tAc "SELECT count(*) FROM information_schema.tables WHERE table_schema = 'public'" 2>/dev/null || echo "unknown")

    local db_size
    db_size=$(psql -h "$PGHOST" -p "$PGPORT" -U "$PGUSER" -d "$db" \
        -tAc "SELECT pg_size_pretty(pg_database_size('${db}'))" 2>/dev/null || echo "unknown")

    echo "  Tables:        ${table_count}"
    echo "  Database size: ${db_size}"
    echo "  Active connections:"
    psql -h "$PGHOST" -p "$PGPORT" -U "$PGUSER" -d postgres \
        -tAc "SELECT count(*) || ' active connections' FROM pg_stat_activity WHERE datname='${db}' AND state='active'" 2>/dev/null || echo "  (unable to check)"
}

confirm_restore() {
    local db="$1"
    local file="$2"

    echo ""
    echo "============================================"
    echo "  RESTORE CONFIRMATION"
    echo "============================================"
    echo ""
    echo "  Backup file:    $(basename "$file")"
    echo "  Target database: $db"
    echo "  Target host:     ${PGHOST}:${PGPORT}"
    echo "  Target user:     ${PGUSER}"
    echo ""
    echo "  WARNING: This will OVERWRITE data in '${db}'!"
    echo "  All existing data in the target database will be replaced."
    echo ""
    echo "============================================"
    echo ""

    if [[ "$NO_CONFIRM" == true ]]; then
        log "Skipping confirmation (--no-confirm flag set)"
        return 0
    fi

    read -r -p "Type the database name '${db}' to confirm restore: " confirmation
    if [[ "$confirmation" != "$db" ]]; then
        log_error "Confirmation failed. Aborting restore."
        exit 1
    fi

    read -r -p "Are you ABSOLUTELY sure? (yes/no): " final_confirm
    if [[ "$final_confirm" != "yes" ]]; then
        log_error "Restore cancelled by user."
        exit 1
    fi
}

# =============================================================================
# List Backups Mode
# =============================================================================

if [[ "$LIST_BACKUPS" == true ]]; then
    list_available_backups
    exit 0
fi

# =============================================================================
# Pre-flight Checks
# =============================================================================

if [[ -z "$BACKUP_FILE" && -z "$DOWNLOAD_S3" ]]; then
    echo "ERROR: No backup file specified." >&2
    echo "Usage: $0 <backup_file> [options]" >&2
    echo "       $0 --list-backups" >&2
    exit 1
fi

if [[ -z "${PGPASSWORD:-}" ]]; then
    echo "ERROR: PGPASSWORD is not set." >&2
    exit 1
fi
export PGPASSWORD

if ! command -v pg_restore &>/dev/null; then
    echo "ERROR: pg_restore not found. Install PostgreSQL client tools." >&2
    exit 1
fi

# Set target database
RESTORE_DB="${TARGET_DB:-$PGDATABASE}"

# Download from S3 if requested
if [[ -n "$DOWNLOAD_S3" ]]; then
    BACKUP_FILE="/tmp/$(basename "$DOWNLOAD_S3")"
    download_from_s3 "$DOWNLOAD_S3" "$BACKUP_FILE"
fi

# =============================================================================
# Main Restore Process
# =============================================================================

STARTED_AT=$(date +%s)

log "=========================================="
log "PostgreSQL Restore Process"
log "  Backup:   $(basename "$BACKUP_FILE")"
log "  Target:   ${RESTORE_DB} @ ${PGHOST}:${PGPORT}"
log "=========================================="

# Step 1: Validate backup file
if ! validate_backup_file "$BACKUP_FILE"; then
    log_error "Backup validation failed. Aborting restore."
    exit 1
fi

# Step 2: Show backup contents summary
log "Backup contents summary:"
pg_restore --list "$BACKUP_FILE" 2>/dev/null | head -20
echo "  ... (truncated)"

# Step 3: Show current database state
get_db_info "$RESTORE_DB"

# Step 4: Confirm
confirm_restore "$RESTORE_DB" "$BACKUP_FILE"

# Step 5: Create pre-restore backup of current database (safety net)
log "Creating pre-restore safety backup of '${RESTORE_DB}'..."
PRE_RESTORE_BACKUP="/tmp/${RESTORE_DB}_pre_restore_$(date +%Y%m%d_%H%M%S).dump"

if pg_dump \
    --host="$PGHOST" \
    --port="$PGPORT" \
    --username="$PGUSER" \
    --dbname="$RESTORE_DB" \
    --format=custom \
    --compress=9 \
    --file="$PRE_RESTORE_BACKUP" 2>/dev/null; then
    log "Pre-restore backup saved: $PRE_RESTORE_BACKUP"
else
    log "WARNING: Could not create pre-restore backup (database may not exist yet)"
fi

# Step 6: Terminate active connections to target database
log "Terminating active connections to '${RESTORE_DB}'..."
psql -h "$PGHOST" -p "$PGPORT" -U "$PGUSER" -d postgres -c \
    "SELECT pg_terminate_backend(pid) FROM pg_stat_activity WHERE datname = '${RESTORE_DB}' AND pid <> pg_backend_pid();" \
    2>/dev/null || true

# Step 7: Perform restore
log "Starting pg_restore..."
if pg_restore \
    --host="$PGHOST" \
    --port="$PGPORT" \
    --username="$PGUSER" \
    --dbname="$RESTORE_DB" \
    --clean \
    --if-exists \
    --verbose \
    --exit-on-error \
    "$BACKUP_FILE" 2>&1 | tee -a "/tmp/pg_restore_${RESTORE_DB}.log"; then

    log "pg_restore completed successfully"
else
    EXIT_CODE=$?
    log_error "pg_restore failed with exit code ${EXIT_CODE}"
    log "Pre-restore backup available at: $PRE_RESTORE_BACKUP"
    log "Restore log: /tmp/pg_restore_${RESTORE_DB}.log"
    exit "$EXIT_CODE"
fi

# Step 8: Post-restore validation
log "Running post-restore validation..."

TABLE_COUNT=$(psql -h "$PGHOST" -p "$PGPORT" -U "$PGUSER" -d "$RESTORE_DB" \
    -tAc "SELECT count(*) FROM information_schema.tables WHERE table_schema = 'public'" 2>/dev/null || echo "0")

DB_SIZE=$(psql -h "$PGHOST" -p "$PGPORT" -U "$PGUSER" -d "$RESTORE_DB" \
    -tAc "SELECT pg_size_pretty(pg_database_size('${RESTORE_DB}'))" 2>/dev/null || echo "unknown")

log "Post-restore database state:"
log "  Tables:        ${TABLE_COUNT}"
log "  Database size: ${DB_SIZE}"

# Check for critical ERP tables
log "Checking critical ERP tables..."
CRITICAL_TABLES=(
    "products"
    "customers"
    "sales_orders"
    "invoices"
    "inventory"
    "audit_logs"
)

MISSING_TABLES=0
for table in "${CRITICAL_TABLES[@]}"; do
    exists=$(psql -h "$PGHOST" -p "$PGPORT" -U "$PGUSER" -d "$RESTORE_DB" \
        -tAc "SELECT EXISTS (SELECT 1 FROM information_schema.tables WHERE table_schema='public' AND table_name='${table}')" 2>/dev/null || echo "f")
    if [[ "$exists" == "t" ]]; then
        log "  [OK] $table"
    else
        log "  [MISSING] $table (may not be part of this backup)"
        ((MISSING_TABLES++))
    fi
done

# Step 9: Analyze tables to update statistics
log "Running ANALYZE to update database statistics..."
psql -h "$PGHOST" -p "$PGPORT" -U "$PGUSER" -d "$RESTORE_DB" -c "ANALYZE;" 2>/dev/null || true

# Summary
ENDED_AT=$(date +%s)
DURATION=$(( ENDED_AT - STARTED_AT ))

log "=========================================="
log "Restore Completed Successfully"
log "  Database: ${RESTORE_DB}"
log "  Tables:   ${TABLE_COUNT}"
log "  Size:     ${DB_SIZE}"
log "  Duration: ${DURATION} seconds"
if [[ -f "$PRE_RESTORE_BACKUP" ]]; then
    log "  Pre-restore backup: ${PRE_RESTORE_BACKUP}"
fi
log "=========================================="

exit 0
