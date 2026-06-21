#!/usr/bin/env bash
#
# PostgreSQL Automated Backup Script
# Enterprise Retail ERP Platform for Georgia
#
# Features:
#   - Full backup using pg_dump with custom format (compressed)
#   - Tiered retention: 7 daily, 4 weekly, 12 monthly backups
#   - Backup verification via pg_restore --list
#   - Optional S3-compatible remote storage upload
#   - Error notification via email or webhook
#   - Logging with timestamps
#
# Usage:
#   ./pg_backup.sh                          # Daily backup
#   ./pg_backup.sh --type weekly            # Weekly backup
#   ./pg_backup.sh --type monthly           # Monthly backup
#   ./pg_backup.sh --upload-s3              # Backup and upload to S3
#   ./pg_backup.sh --type weekly --upload-s3
#
# Environment variables (with defaults):
#   PGHOST              (localhost)
#   PGPORT              (5432)
#   PGUSER              (erp_user)
#   PGDATABASE          (georgia_erp)
#   PGPASSWORD          (required, no default)
#   BACKUP_DIR          (/var/backups/georgia-erp/postgres)
#   BACKUP_LOG_DIR      (/var/log/georgia-erp)
#
# Retention variables:
#   RETENTION_DAILY     (7)    - days to keep daily backups
#   RETENTION_WEEKLY    (28)   - days to keep weekly backups (4 weeks)
#   RETENTION_MONTHLY   (365)  - days to keep monthly backups (12 months)
#
# S3 variables (required if --upload-s3):
#   S3_BUCKET           - S3 bucket name
#   S3_ENDPOINT         - S3 endpoint URL (for MinIO, Wasabi, etc.)
#   S3_ACCESS_KEY       - S3 access key
#   S3_SECRET_KEY       - S3 secret key
#   S3_REGION           - S3 region (default: us-east-1)
#
# Notification variables (optional):
#   NOTIFY_EMAIL        - email address for failure notifications
#   NOTIFY_WEBHOOK_URL  - webhook URL for failure notifications (Slack, Teams)
#   SMTP_SERVER         - SMTP server for email (default: localhost)
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
BACKUP_LOG_DIR="${BACKUP_LOG_DIR:-/var/log/georgia-erp}"

RETENTION_DAILY="${RETENTION_DAILY:-7}"
RETENTION_WEEKLY="${RETENTION_WEEKLY:-28}"
RETENTION_MONTHLY="${RETENTION_MONTHLY:-365}"

S3_REGION="${S3_REGION:-us-east-1}"
SMTP_SERVER="${SMTP_SERVER:-localhost}"

BACKUP_TYPE="daily"
UPLOAD_S3=false

# =============================================================================
# Argument Parsing
# =============================================================================

while [[ $# -gt 0 ]]; do
    case "$1" in
        --type)
            BACKUP_TYPE="$2"
            shift 2
            ;;
        --upload-s3)
            UPLOAD_S3=true
            shift
            ;;
        --help|-h)
            head -45 "$0" | tail -42
            exit 0
            ;;
        *)
            echo "Unknown argument: $1" >&2
            exit 1
            ;;
    esac
done

if [[ "$BACKUP_TYPE" != "daily" && "$BACKUP_TYPE" != "weekly" && "$BACKUP_TYPE" != "monthly" ]]; then
    echo "ERROR: --type must be daily, weekly, or monthly" >&2
    exit 1
fi

# =============================================================================
# Functions
# =============================================================================

timestamp() {
    date '+%Y-%m-%d %H:%M:%S'
}

log() {
    echo "[$(timestamp)] $1" | tee -a "$LOG_FILE"
}

log_error() {
    echo "[$(timestamp)] ERROR: $1" | tee -a "$LOG_FILE" >&2
}

send_notification() {
    local subject="$1"
    local body="$2"

    # Email notification
    if [[ -n "${NOTIFY_EMAIL:-}" ]]; then
        if command -v mail &>/dev/null; then
            echo "$body" | mail -s "$subject" -S smtp="$SMTP_SERVER" "$NOTIFY_EMAIL" 2>/dev/null || true
        elif command -v sendmail &>/dev/null; then
            {
                echo "Subject: $subject"
                echo "To: $NOTIFY_EMAIL"
                echo ""
                echo "$body"
            } | sendmail "$NOTIFY_EMAIL" 2>/dev/null || true
        fi
        log "Notification email sent to $NOTIFY_EMAIL"
    fi

    # Webhook notification (Slack/Teams compatible)
    if [[ -n "${NOTIFY_WEBHOOK_URL:-}" ]]; then
        local payload
        payload=$(cat <<WEBHOOK_EOF
{
    "text": "${subject}\n\n${body}"
}
WEBHOOK_EOF
        )
        curl -sS -X POST -H "Content-Type: application/json" \
            -d "$payload" \
            "$NOTIFY_WEBHOOK_URL" 2>/dev/null || true
        log "Webhook notification sent"
    fi
}

upload_to_s3() {
    local file="$1"
    local s3_path="$2"

    if [[ -z "${S3_BUCKET:-}" ]]; then
        log_error "S3_BUCKET is not set. Cannot upload to S3."
        return 1
    fi

    local aws_cmd="aws s3 cp"
    local endpoint_flag=""

    if [[ -n "${S3_ENDPOINT:-}" ]]; then
        endpoint_flag="--endpoint-url $S3_ENDPOINT"
    fi

    if [[ -n "${S3_ACCESS_KEY:-}" && -n "${S3_SECRET_KEY:-}" ]]; then
        export AWS_ACCESS_KEY_ID="$S3_ACCESS_KEY"
        export AWS_SECRET_ACCESS_KEY="$S3_SECRET_KEY"
    fi

    export AWS_DEFAULT_REGION="$S3_REGION"

    log "Uploading $(basename "$file") to s3://${S3_BUCKET}/${s3_path}..."

    # shellcheck disable=SC2086
    $aws_cmd "$file" "s3://${S3_BUCKET}/${s3_path}" $endpoint_flag \
        --storage-class STANDARD_IA

    if [[ $? -eq 0 ]]; then
        log "S3 upload completed successfully"
    else
        log_error "S3 upload failed"
        return 1
    fi
}

verify_backup() {
    local backup_file="$1"

    log "Verifying backup integrity..."

    # Check file exists and is not empty
    if [[ ! -f "$backup_file" ]]; then
        log_error "Backup file does not exist: $backup_file"
        return 1
    fi

    local file_size
    file_size=$(stat -f%z "$backup_file" 2>/dev/null || stat -c%s "$backup_file" 2>/dev/null || echo "0")
    if [[ "$file_size" -eq 0 ]]; then
        log_error "Backup file is empty: $backup_file"
        return 1
    fi

    # Verify backup can be read by pg_restore
    if pg_restore --list "$backup_file" > /dev/null 2>&1; then
        local object_count
        object_count=$(pg_restore --list "$backup_file" 2>/dev/null | grep -c "^[0-9]" || echo "0")
        log "Backup verification passed: $object_count database objects found"
        return 0
    else
        log_error "Backup verification FAILED: pg_restore cannot read the backup"
        return 1
    fi
}

prune_backups() {
    local backup_subdir="$1"
    local retention_days="$2"
    local dir="${BACKUP_DIR}/${backup_subdir}"

    if [[ ! -d "$dir" ]]; then
        return 0
    fi

    log "Pruning ${backup_subdir} backups older than ${retention_days} days..."

    local count=0
    while IFS= read -r file; do
        log "  Removing: $(basename "$file")"
        rm -f "$file"
        ((count++))
    done < <(find "$dir" -name "${PGDATABASE}_*.dump" -type f -mtime "+${retention_days}" 2>/dev/null)

    if [[ $count -gt 0 ]]; then
        log "Pruned $count old ${backup_subdir} backup(s)"
    else
        log "No ${backup_subdir} backups to prune"
    fi
}

# =============================================================================
# Pre-flight Checks
# =============================================================================

if [[ -z "${PGPASSWORD:-}" ]]; then
    echo "ERROR: PGPASSWORD is not set." >&2
    exit 1
fi
export PGPASSWORD

# Check for pg_dump
if ! command -v pg_dump &>/dev/null; then
    echo "ERROR: pg_dump not found. Install PostgreSQL client tools." >&2
    exit 1
fi

# Create directories
mkdir -p "${BACKUP_DIR}/daily"
mkdir -p "${BACKUP_DIR}/weekly"
mkdir -p "${BACKUP_DIR}/monthly"
mkdir -p "$BACKUP_LOG_DIR"

# =============================================================================
# Main Backup Process
# =============================================================================

LOG_FILE="${BACKUP_LOG_DIR}/pg_backup_$(date +%Y%m%d).log"
STARTED_AT=$(date +%s)

log "=========================================="
log "PostgreSQL Backup Started"
log "  Type:     ${BACKUP_TYPE}"
log "  Database: ${PGDATABASE}"
log "  Host:     ${PGHOST}:${PGPORT}"
log "  User:     ${PGUSER}"
log "=========================================="

# Generate filename
TIMESTAMP=$(date +%Y%m%d_%H%M%S)
BACKUP_SUBDIR="${BACKUP_TYPE}"
BACKUP_FILE="${BACKUP_DIR}/${BACKUP_SUBDIR}/${PGDATABASE}_${BACKUP_TYPE}_${TIMESTAMP}.dump"

# Test database connectivity
log "Testing database connectivity..."
if ! pg_isready -h "$PGHOST" -p "$PGPORT" -U "$PGUSER" -d "$PGDATABASE" &>/dev/null; then
    log_error "Cannot connect to database at ${PGHOST}:${PGPORT}"
    send_notification \
        "[ALERT] Georgia ERP - PostgreSQL Backup Failed" \
        "Backup failed: Cannot connect to database.\n\nHost: ${PGHOST}:${PGPORT}\nDatabase: ${PGDATABASE}\nType: ${BACKUP_TYPE}\nTime: $(timestamp)"
    exit 1
fi
log "Database connectivity confirmed"

# Perform backup
log "Starting pg_dump..."
if pg_dump \
    --host="$PGHOST" \
    --port="$PGPORT" \
    --username="$PGUSER" \
    --dbname="$PGDATABASE" \
    --format=custom \
    --compress=9 \
    --verbose \
    --file="$BACKUP_FILE" \
    2>> "$LOG_FILE"; then

    BACKUP_SIZE=$(du -h "$BACKUP_FILE" | cut -f1)
    log "pg_dump completed successfully. File size: ${BACKUP_SIZE}"
else
    EXIT_CODE=$?
    log_error "pg_dump failed with exit code ${EXIT_CODE}"
    send_notification \
        "[ALERT] Georgia ERP - PostgreSQL Backup Failed" \
        "pg_dump failed with exit code ${EXIT_CODE}.\n\nHost: ${PGHOST}:${PGPORT}\nDatabase: ${PGDATABASE}\nType: ${BACKUP_TYPE}\nTime: $(timestamp)\n\nCheck logs: ${LOG_FILE}"
    exit "$EXIT_CODE"
fi

# Verify backup
if ! verify_backup "$BACKUP_FILE"; then
    send_notification \
        "[ALERT] Georgia ERP - Backup Verification Failed" \
        "Backup verification failed for: $(basename "$BACKUP_FILE")\n\nThe backup file may be corrupted.\nHost: ${PGHOST}:${PGPORT}\nDatabase: ${PGDATABASE}\nType: ${BACKUP_TYPE}\nTime: $(timestamp)"
    exit 1
fi

# Generate checksum
CHECKSUM_FILE="${BACKUP_FILE}.sha256"
sha256sum "$BACKUP_FILE" > "$CHECKSUM_FILE"
log "SHA256 checksum saved: $(cat "$CHECKSUM_FILE")"

# Upload to S3 if requested
if [[ "$UPLOAD_S3" == true ]]; then
    S3_PATH="backups/postgres/${BACKUP_TYPE}/$(basename "$BACKUP_FILE")"
    if upload_to_s3 "$BACKUP_FILE" "$S3_PATH"; then
        # Also upload checksum
        upload_to_s3 "$CHECKSUM_FILE" "${S3_PATH}.sha256" || true
    else
        send_notification \
            "[WARNING] Georgia ERP - S3 Upload Failed" \
            "Backup completed locally but S3 upload failed.\n\nFile: $(basename "$BACKUP_FILE")\nSize: ${BACKUP_SIZE}\nType: ${BACKUP_TYPE}\nTime: $(timestamp)"
    fi
fi

# Apply retention policy
prune_backups "daily" "$RETENTION_DAILY"
prune_backups "weekly" "$RETENTION_WEEKLY"
prune_backups "monthly" "$RETENTION_MONTHLY"

# Summary
ENDED_AT=$(date +%s)
DURATION=$(( ENDED_AT - STARTED_AT ))

log "=========================================="
log "Backup Completed Successfully"
log "  File:     $(basename "$BACKUP_FILE")"
log "  Size:     ${BACKUP_SIZE}"
log "  Type:     ${BACKUP_TYPE}"
log "  Duration: ${DURATION} seconds"
log "  S3:       ${UPLOAD_S3}"
log "=========================================="

exit 0
