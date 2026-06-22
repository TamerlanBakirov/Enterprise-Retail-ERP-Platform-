#Requires -Version 5.1
<#
.SYNOPSIS
    PostgreSQL backup for Georgia ERP Platform.
.DESCRIPTION
    Creates a compressed custom-format dump and prunes backups older than RetentionDays.
.EXAMPLE
    .\scripts\backup-db.ps1
    .\scripts\backup-db.ps1 -BackupDir D:\Backups -RetentionDays 30
.NOTES
    Requires pg_dump on PATH (PostgreSQL client tools).
    Restore: pg_restore --clean --if-exists -h localhost -U erp_user -d georgia_erp <file>
#>
param(
    [string]$PgHost = "localhost",
    [int]$PgPort = 5432,
    [string]$PgUser = "erp_user",
    [string]$PgDatabase = "georgia_erp",
    [string]$BackupDir = ".\backups",
    [int]$RetentionDays = 14
)

$ErrorActionPreference = "Stop"

if (-not $env:PGPASSWORD) {
    Write-Error "PGPASSWORD environment variable is not set."
    exit 1
}

if (-not (Get-Command pg_dump -ErrorAction SilentlyContinue)) {
    Write-Error "pg_dump not found. Install PostgreSQL client tools and add to PATH."
    exit 1
}

if (-not (Test-Path $BackupDir)) {
    New-Item -ItemType Directory -Path $BackupDir -Force | Out-Null
}

$timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
$outFile = Join-Path $BackupDir "${PgDatabase}_${timestamp}.dump"

Write-Host "Backing up $PgDatabase from ${PgHost}:${PgPort} -> $outFile"
pg_dump --host=$PgHost --port=$PgPort --username=$PgUser --dbname=$PgDatabase --format=custom --compress=9 --file=$outFile

if ($LASTEXITCODE -ne 0) {
    Write-Error "pg_dump failed with exit code $LASTEXITCODE"
    exit $LASTEXITCODE
}

$size = (Get-Item $outFile).Length / 1MB
Write-Host ("Backup complete: {0:F2} MB" -f $size)

Write-Host "Pruning backups older than $RetentionDays days..."
$cutoff = (Get-Date).AddDays(-$RetentionDays)
Get-ChildItem -Path $BackupDir -Filter "${PgDatabase}_*.dump" |
    Where-Object { $_.LastWriteTime -lt $cutoff } |
    ForEach-Object {
        Write-Host "  Removing $($_.Name)"
        Remove-Item $_.FullName -Force
    }

Write-Host "Done."
