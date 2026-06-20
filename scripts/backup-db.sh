#!/usr/bin/env bash
#
# PostgreSQL backup for the Georgia ERP Platform.
# Creates a compressed custom-format dump and prunes backups older than RETENTION_DAYS.
#
# Usage:
#   ./scripts/backup-db.sh
#
# Environment variables (with defaults):
#   PGHOST          (localhost)
#   PGPORT          (5432)
#   PGUSER          (erp_user)
#   PGDATABASE      (georgia_erp)
#   PGPASSWORD      (required, no default)
#   BACKUP_DIR      (./backups)
#   RETENTION_DAYS  (14)
#
# Restore example:
#   pg_restore --clean --if-exists -h localhost -U erp_user -d georgia_erp <file>
#
set -euo pipefail

PGHOST="${PGHOST:-localhost}"
PGPORT="${PGPORT:-5432}"
PGUSER="${PGUSER:-erp_user}"
PGDATABASE="${PGDATABASE:-georgia_erp}"
BACKUP_DIR="${BACKUP_DIR:-./backups}"
RETENTION_DAYS="${RETENTION_DAYS:-14}"

if [[ -z "${PGPASSWORD:-}" ]]; then
  echo "ERROR: PGPASSWORD is not set." >&2
  exit 1
fi
export PGPASSWORD

mkdir -p "$BACKUP_DIR"
timestamp="$(date +%Y%m%d_%H%M%S)"
outfile="${BACKUP_DIR}/${PGDATABASE}_${timestamp}.dump"

echo "Backing up ${PGDATABASE} from ${PGHOST}:${PGPORT} -> ${outfile}"
pg_dump \
  --host="$PGHOST" \
  --port="$PGPORT" \
  --username="$PGUSER" \
  --dbname="$PGDATABASE" \
  --format=custom \
  --compress=9 \
  --file="$outfile"

echo "Backup complete: $(du -h "$outfile" | cut -f1)"

echo "Pruning backups older than ${RETENTION_DAYS} days..."
find "$BACKUP_DIR" -name "${PGDATABASE}_*.dump" -type f -mtime "+${RETENTION_DAYS}" -print -delete

echo "Done."
