#Requires -RunAsAdministrator
<#
  Despliega NicaRunner.Api en IIS local (estilo producción), usando SQLite
  (ASPNETCORE_ENVIRONMENT=Development ya queda fijado en web.config).
  El contenido publicado vive en: publish\NicaRunnerApi (generado con dotnet publish).
#>

$ErrorActionPreference = "Stop"

$siteName   = "NicaRunnerApi"
$appPool    = "NicaRunnerApi"
$port       = 5190
$physicalPath = "E:\HILBERT\Proyectos\Claude Code\NicaRunner-API\publish\NicaRunnerApi"

# 1. Verificar ASP.NET Core Hosting Bundle (módulo ANCM)
$ancm = Test-Path "$env:ProgramFiles\IIS\Asp.Net Core Module\V2\aspnetcorev2.dll"
if (-not $ancm) {
    Write-Warning "Falta el ASP.NET Core Hosting Bundle. Descárgalo e instálalo desde:"
    Write-Warning "  https://dotnet.microsoft.com/download/dotnet/8.0  ->  'Hosting Bundle' (Windows)"
    Write-Warning "Tras instalarlo, ejecuta 'net stop was /y; net start w3svc' y vuelve a correr este script."
    exit 1
}

Import-Module WebAdministration

# 2. Liberar el puerto 5190 si lo tiene tomado el 'dotnet run' de desarrollo
$proc = Get-NetTCPConnection -LocalPort $port -State Listen -ErrorAction SilentlyContinue
if ($proc) {
    Write-Host "Puerto $port en uso por PID $($proc.OwningProcess) (probablemente 'dotnet run' de dev). Deteniéndolo..."
    Stop-Process -Id $proc.OwningProcess -Force -ErrorAction SilentlyContinue
    Start-Sleep -Seconds 2
}

# 3. App Pool sin código administrado (ANCM en proceso maneja el runtime .NET)
if (-not (Test-Path "IIS:\AppPools\$appPool")) {
    New-WebAppPool -Name $appPool | Out-Null
}
Set-ItemProperty "IIS:\AppPools\$appPool" -Name managedRuntimeVersion -Value ""
Set-ItemProperty "IIS:\AppPools\$appPool" -Name startMode -Value "AlwaysRunning"

# 4. Sitio IIS
if (Get-Website -Name $siteName -ErrorAction SilentlyContinue) {
    Remove-Website -Name $siteName
}
New-Website -Name $siteName -PhysicalPath $physicalPath -ApplicationPool $appPool -Port $port | Out-Null

# 5. Permisos: el pool de aplicaciones necesita escribir en la carpeta (SQLite + logs)
$identity = "IIS AppPool\$appPool"
icacls $physicalPath /grant "${identity}:(OI)(CI)M" /T | Out-Null

# 6. Arrancar
Start-WebAppPool -Name $appPool
Start-Website -Name $siteName

Start-Sleep -Seconds 2
Write-Host "Probando http://localhost:$port/swagger/index.html ..."
try {
    $resp = Invoke-WebRequest "http://localhost:$port/swagger/index.html" -UseBasicParsing
    Write-Host "OK -> HTTP $($resp.StatusCode)"
} catch {
    Write-Warning "No respondió. Revisa publish\NicaRunnerApi\logs\stdout*.log para el error."
}
