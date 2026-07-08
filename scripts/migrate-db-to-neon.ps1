# Migra nicarunner-db desde Render PostgreSQL hacia Neon.
# Usa pg_dump/psql nativos si están en PATH; si no, Docker (postgres:18-alpine).
#
# Uso:
#   $env:RENDER_DATABASE_URL = "postgresql://user:pass@host/nicarunner"
#   $env:NEON_DATABASE_URL  = "postgresql://user:pass@ep-xxx.aws.neon.tech/neondb?sslmode=require"
#   .\scripts\migrate-db-to-neon.ps1
#
# Para importar, usar la connection string DIRECTA de Neon (no pooler).
# En Render, configurar la connection string POOLED para la API en producción.

param(
    [string]$RenderUrl = $env:RENDER_DATABASE_URL,
    [string]$NeonUrl = $env:NEON_DATABASE_URL,
    [switch]$DumpOnly,
    [switch]$RestoreOnly,
    [string]$DumpFile = ""
)

$ErrorActionPreference = "Stop"

function Find-PostgresBin {
    param([string]$Name)

    $cmd = Get-Command $Name -ErrorAction SilentlyContinue
    if ($cmd) { return $cmd.Source }

    $pgRoot = "C:\Program Files\PostgreSQL"
    if (Test-Path $pgRoot) {
        $match = Get-ChildItem $pgRoot -Recurse -Filter "$Name.exe" -ErrorAction SilentlyContinue |
            Sort-Object FullName -Descending |
            Select-Object -First 1
        if ($match) { return $match.FullName }
    }

    return $null
}

function Test-DockerRunning {
    docker info *> $null
    return $LASTEXITCODE -eq 0
}

function Resolve-PostgresBackend {
    $pgDump = Find-PostgresBin "pg_dump"
    $psql = Find-PostgresBin "psql"

    if ($pgDump -and $psql) {
        return @{
            Mode   = "native"
            PgDump = $pgDump
            Psql   = $psql
        }
    }

    if (Test-DockerRunning) {
        return @{ Mode = "docker" }
    }

    throw @"
No hay herramientas PostgreSQL disponibles. Elegí una opción:

  A) Iniciar Docker Desktop y reintentar (el script usa postgres:18-alpine).

  B) Instalar cliente PostgreSQL:
       winget install PostgreSQL.PostgreSQL.17 --accept-package-agreements
     Cerrar y reabrir PowerShell, luego reintentar.

  C) Instalar solo con Scoop (más liviano):
       scoop install postgresql
"@
}

function Invoke-PgDump {
    param(
        [hashtable]$Backend,
        [string]$SourceUrl,
        [string]$OutputFile
    )

    if ($Backend.Mode -eq "native") {
        & $Backend.PgDump $SourceUrl `
            --no-owner --no-privileges --clean --if-exists `
            -f $OutputFile
        if ($LASTEXITCODE -ne 0) { throw "pg_dump falló." }
        return
    }

    $hostPath = (Resolve-Path (Split-Path $OutputFile -Parent)).Path
    $containerDump = "/backup/$(Split-Path $OutputFile -Leaf)"

    docker run --rm `
        -v "${hostPath}:/backup" `
        -e "SOURCE_URL=$SourceUrl" `
        postgres:18-alpine `
        sh -c "pg_dump `"`$SOURCE_URL`" --no-owner --no-privileges --clean --if-exists -f $containerDump"

    if ($LASTEXITCODE -ne 0) { throw "pg_dump (Docker) falló." }
}

function Invoke-PsqlRestore {
    param(
        [hashtable]$Backend,
        [string]$TargetUrl,
        [string]$InputFile
    )

    if ($Backend.Mode -eq "native") {
        & $Backend.Psql $TargetUrl -v ON_ERROR_STOP=1 -f $InputFile
        if ($LASTEXITCODE -ne 0) { throw "psql restore falló." }
        return
    }

    $hostPath = (Resolve-Path (Split-Path $InputFile -Parent)).Path
    $containerDump = "/backup/$(Split-Path $InputFile -Leaf)"

    docker run --rm `
        -v "${hostPath}:/backup" `
        -e "TARGET_URL=$TargetUrl" `
        postgres:18-alpine `
        sh -c "psql `"`$TARGET_URL`" -v ON_ERROR_STOP=1 -f $containerDump"

    if ($LASTEXITCODE -ne 0) { throw "psql restore (Docker) falló." }
}

function Test-ConnectionString {
    param([string]$Value, [string]$Name)

    if ([string]::IsNullOrWhiteSpace($Value)) {
        throw "Falta $Name. Seteá la variable de entorno o pasala como parámetro."
    }
    if ($Value -notmatch '^(postgres|postgresql)://') {
        throw "$Name debe ser una URI postgresql://..."
    }
}

$repoRoot = Split-Path -Parent $PSScriptRoot
$backupDir = Join-Path $repoRoot ".migration"
New-Item -ItemType Directory -Force -Path $backupDir | Out-Null

if ([string]::IsNullOrWhiteSpace($DumpFile)) {
    $timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
    $DumpFile = Join-Path $backupDir "nicarunner_$timestamp.sql"
}

$backend = Resolve-PostgresBackend
Write-Host "Backend: $($backend.Mode)"

if (-not $RestoreOnly) {
    Test-ConnectionString -Value $RenderUrl -Name "RENDER_DATABASE_URL"
    Write-Host "Exportando desde Render → $DumpFile"
    Invoke-PgDump -Backend $backend -SourceUrl $RenderUrl -OutputFile $DumpFile
    $sizeKb = [math]::Round((Get-Item $DumpFile).Length / 1KB, 1)
    Write-Host "Dump listo ($sizeKb KB): $DumpFile"
}

if ($DumpOnly) { exit 0 }

if (-not (Test-Path $DumpFile)) {
    throw "No existe el dump: $DumpFile"
}

Test-ConnectionString -Value $NeonUrl -Name "NEON_DATABASE_URL"

Write-Host "Importando en Neon desde $DumpFile ..."
Invoke-PsqlRestore -Backend $backend -TargetUrl $NeonUrl -InputFile $DumpFile

Write-Host ""
Write-Host "Migración completada."
Write-Host ""
Write-Host "Siguiente paso en Render Dashboard → nicarunner-api → Environment:"
Write-Host "  ConnectionStrings__PostgresConnection = <URI pooled de Neon>"
Write-Host ""
Write-Host "Verificar:"
Write-Host "  curl https://<tu-servicio>.onrender.com/health"
