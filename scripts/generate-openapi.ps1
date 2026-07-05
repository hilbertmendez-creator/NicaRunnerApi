# Genera openapi.json para preview local del sitio de documentación.
param(
    [string]$ApiServerUrl = "http://localhost:5000"
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot

Push-Location $root
try {
    dotnet tool restore
    dotnet build src/NicaRunner.Api/NicaRunner.Api.csproj --configuration Release

    $env:ASPNETCORE_ENVIRONMENT = "Development"
    $env:ConnectionStrings__SqliteConnection = "Data Source=$root/docs-site/nicarunner.docs.db"
    $env:Docs__ApiServerUrl = $ApiServerUrl

    dotnet swagger tofile `
        --output docs-site/openapi.json `
        src/NicaRunner.Api/bin/Release/net8.0/NicaRunner.Api.dll `
        v1

    Write-Host "OpenAPI spec written to docs-site/openapi.json"
}
finally {
    Pop-Location
}
