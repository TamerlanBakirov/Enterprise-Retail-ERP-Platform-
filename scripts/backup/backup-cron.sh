#!/usr/bin/env bash
#
# Backup Scheduling Configuration
# Enterprise Retail ERP Platform for Georgia
#
# This script sets up cron jobs for automated backups.
#
# Usage:
#   ./backup-cron.sh install     # Install cron jobs
#   ./backup-cron.sh remove      # Remove cron jobs
#   ./backup-cron.sh show        # Show current backup cron jobs
#   ./backup-cron.sh test        # Run a test backup (daily type)
#
# Prerequisites:
#   - pg_dump available on PATH
#   - Environment file at /etc/georgia-erp/backup.env
#   - Backup scripts in the same directory as this script
#
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ENV_FILE="/etc/georgia-erp/backup.env"
CRON_TAG="# georgia-erp-backup"

# =============================================================================
# Environment File Template
# =============================================================================

create_env_template() {
    local env_dir
    env_dir="$(dirname "$ENV_FILE")"

    if [[ -f "$ENV_FILE" ]]; then
        echo "Environment file already exists: $ENV_FILE"
        return 0
    fi

    echo "Creating environment file template: $ENV_FILE"
    mkdir -p "$env_dir"

    cat > "$ENV_FILE" <<'ENVEOF'
# Georgia ERP Backup Configuration
# Created by backup-cron.sh
# Edit this file with your production values.

# PostgreSQL Connection
PGHOST=localhost
PGPORT=5432
PGUSER=erp_user
PGDATABASE=georgia_erp
PGPASSWORD=CHANGE_ME_IN_PRODUCTION

# Backup Storage
BACKUP_DIR=/var/backups/georgia-erp/postgres
BACKUP_LOG_DIR=/var/log/georgia-erp

# Retention Policy
RETENTION_DAILY=7
RETENTION_WEEKLY=28
RETENTION_MONTHLY=365

# S3 Remote Storage (optional, uncomment to enable)
# S3_BUCKET=my-erp-backups
# S3_ENDPOINT=https://s3.amazonaws.com
# S3_ACCESS_KEY=AKIA...
# S3_SECRET_KEY=...
# S3_REGION=eu-west-1

# Notifications (optional, uncomment to enable)
# NOTIFY_EMAIL=admin@company.ge
# NOTIFY_WEBHOOK_URL=https://hooks.slack.com/services/T.../B.../...
# SMTP_SERVER=smtp.company.ge

# RabbitMQ Connection
RABBITMQ_HOST=localhost
RABBITMQ_PORT=15672
RABBITMQ_USER=erp_user
RABBITMQ_PASS=CHANGE_ME_IN_PRODUCTION

# Docker (for volume backups)
DOCKER_COMPOSE_DIR=/opt/georgia-erp
VOLUME_BACKUP_DIR=/var/backups/georgia-erp/volumes
ENVEOF

    chmod 600 "$ENV_FILE"
    echo "Environment file created. Edit it with your production values:"
    echo "  sudo nano $ENV_FILE"
}

# =============================================================================
# Cron Job Definitions
# =============================================================================

# Schedule:
#   - Daily full backup:     2:00 AM every day
#   - Weekly full backup:    3:00 AM every Sunday
#   - Monthly full backup:   4:00 AM on the 1st of each month
#   - RabbitMQ definitions:  2:30 AM every day
#   - Docker volumes:        5:00 AM every Sunday

generate_cron_entries() {
    cat <<CRONEOF
# ===========================================================
# Georgia ERP Platform - Automated Backup Schedule
# Installed by: backup-cron.sh
# ===========================================================

# Daily PostgreSQL backup at 2:00 AM
0 2 * * * . ${ENV_FILE} && ${SCRIPT_DIR}/pg_backup.sh --type daily >> /var/log/georgia-erp/cron_daily.log 2>&1 ${CRON_TAG}

# Weekly PostgreSQL backup at 3:00 AM on Sundays
0 3 * * 0 . ${ENV_FILE} && ${SCRIPT_DIR}/pg_backup.sh --type weekly >> /var/log/georgia-erp/cron_weekly.log 2>&1 ${CRON_TAG}

# Monthly PostgreSQL backup at 4:00 AM on the 1st
0 4 1 * * . ${ENV_FILE} && ${SCRIPT_DIR}/pg_backup.sh --type monthly >> /var/log/georgia-erp/cron_monthly.log 2>&1 ${CRON_TAG}

# Daily PostgreSQL backup with S3 upload at 2:15 AM (uncomment to enable)
# 15 2 * * * . ${ENV_FILE} && ${SCRIPT_DIR}/pg_backup.sh --type daily --upload-s3 >> /var/log/georgia-erp/cron_daily_s3.log 2>&1 ${CRON_TAG}

# Daily RabbitMQ definitions backup at 2:30 AM
30 2 * * * . ${ENV_FILE} && ${SCRIPT_DIR}/rabbitmq_backup.sh >> /var/log/georgia-erp/cron_rabbitmq.log 2>&1 ${CRON_TAG}

# Weekly Docker volume backup at 5:00 AM on Sundays
0 5 * * 0 . ${ENV_FILE} && ${SCRIPT_DIR}/docker_volumes_backup.sh >> /var/log/georgia-erp/cron_volumes.log 2>&1 ${CRON_TAG}

CRONEOF
}

# =============================================================================
# Commands
# =============================================================================

install_cron() {
    # Create env file if it doesn't exist
    create_env_template

    # Create log directory
    mkdir -p /var/log/georgia-erp

    # Make scripts executable
    chmod +x "${SCRIPT_DIR}/pg_backup.sh" 2>/dev/null || true
    chmod +x "${SCRIPT_DIR}/pg_restore.sh" 2>/dev/null || true
    chmod +x "${SCRIPT_DIR}/rabbitmq_backup.sh" 2>/dev/null || true
    chmod +x "${SCRIPT_DIR}/docker_volumes_backup.sh" 2>/dev/null || true

    # Remove existing Georgia ERP cron entries
    local existing_cron
    existing_cron=$(crontab -l 2>/dev/null || echo "")
    local cleaned_cron
    cleaned_cron=$(echo "$existing_cron" | grep -v "$CRON_TAG" | grep -v "Georgia ERP Platform" | grep -v "backup-cron.sh" || true)

    # Add new entries
    local new_entries
    new_entries=$(generate_cron_entries)

    {
        echo "$cleaned_cron"
        echo ""
        echo "$new_entries"
    } | crontab -

    echo "Cron jobs installed successfully."
    echo ""
    echo "Schedule:"
    echo "  Daily DB backup:     2:00 AM every day"
    echo "  Weekly DB backup:    3:00 AM every Sunday"
    echo "  Monthly DB backup:   4:00 AM on the 1st"
    echo "  RabbitMQ backup:     2:30 AM every day"
    echo "  Docker volumes:      5:00 AM every Sunday"
    echo ""
    echo "IMPORTANT: Edit the environment file before the first backup runs:"
    echo "  sudo nano $ENV_FILE"
    echo ""
    echo "View installed jobs:"
    echo "  crontab -l | grep georgia-erp"
}

remove_cron() {
    local existing_cron
    existing_cron=$(crontab -l 2>/dev/null || echo "")

    if ! echo "$existing_cron" | grep -q "$CRON_TAG"; then
        echo "No Georgia ERP backup cron jobs found."
        return 0
    fi

    local cleaned_cron
    cleaned_cron=$(echo "$existing_cron" | grep -v "$CRON_TAG" | grep -v "Georgia ERP Platform" | grep -v "backup-cron.sh" | grep -v "^$" || true)

    echo "$cleaned_cron" | crontab -

    echo "Georgia ERP backup cron jobs removed."
}

show_cron() {
    echo "Current Georgia ERP backup cron jobs:"
    echo "============================================"
    crontab -l 2>/dev/null | grep -A1 "$CRON_TAG" || echo "  No backup cron jobs installed."
    echo ""
    echo "All cron jobs:"
    echo "============================================"
    crontab -l 2>/dev/null || echo "  No crontab installed."
}

test_backup() {
    echo "Running test backup (daily type)..."
    echo ""

    if [[ -f "$ENV_FILE" ]]; then
        # shellcheck disable=SC1090
        . "$ENV_FILE"
    else
        echo "WARNING: Environment file not found at $ENV_FILE"
        echo "Using environment variables from shell."
    fi

    exec "${SCRIPT_DIR}/pg_backup.sh" --type daily
}

# =============================================================================
# Systemd Timer Alternative
# =============================================================================

show_systemd_alternative() {
    cat <<'SYSTEMDEOF'

ALTERNATIVE: Systemd Timer Configuration
==========================================

If you prefer systemd timers over cron, create these files:

1. /etc/systemd/system/georgia-erp-backup-daily.service
---
[Unit]
Description=Georgia ERP Daily PostgreSQL Backup
After=network.target postgresql.service

[Service]
Type=oneshot
EnvironmentFile=/etc/georgia-erp/backup.env
ExecStart=/opt/georgia-erp/scripts/backup/pg_backup.sh --type daily
User=postgres
StandardOutput=journal
StandardError=journal
---

2. /etc/systemd/system/georgia-erp-backup-daily.timer
---
[Unit]
Description=Georgia ERP Daily Backup Timer

[Timer]
OnCalendar=*-*-* 02:00:00
Persistent=true
RandomizedDelaySec=300

[Install]
WantedBy=timers.target
---

Enable with:
  sudo systemctl enable --now georgia-erp-backup-daily.timer
  sudo systemctl list-timers | grep georgia

SYSTEMDEOF
}

# =============================================================================
# Main
# =============================================================================

ACTION="${1:-help}"

case "$ACTION" in
    install)
        install_cron
        ;;
    remove)
        remove_cron
        ;;
    show)
        show_cron
        ;;
    test)
        test_backup
        ;;
    systemd)
        show_systemd_alternative
        ;;
    help|--help|-h)
        echo "Usage: $0 {install|remove|show|test|systemd}"
        echo ""
        echo "Commands:"
        echo "  install  - Install backup cron jobs"
        echo "  remove   - Remove backup cron jobs"
        echo "  show     - Show current backup cron jobs"
        echo "  test     - Run a test backup immediately"
        echo "  systemd  - Show systemd timer alternative"
        ;;
    *)
        echo "Unknown command: $ACTION" >&2
        echo "Usage: $0 {install|remove|show|test|systemd}" >&2
        exit 1
        ;;
esac
