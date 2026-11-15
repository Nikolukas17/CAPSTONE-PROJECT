# Simulación de la oferta académica (tres escenarios) sobre una base de datos nueva y aparte.
#
# Uso (desde la raíz del proyecto, en PowerShell):
#   .\herramientas\simulacion\simular.ps1                # corre la simulación y cierra la app al terminar
#   .\herramientas\simulacion\simular.ps1 -DejarAbierta  # deja la app abierta para revisar las proyecciones
#
# Requisitos: .NET 10 SDK y Python 3 (solo librería estándar).
# Resultado: carpeta simulacion-salida\<fecha>\ con un Excel por escenario y resultados.json.
# La base real (App_Data\oferta_academica.db) no se usa ni se modifica.

param(
    [switch]$DejarAbierta,
    [int]$Puerto = 5898
)

$ErrorActionPreference = 'Stop'
$proyecto = Resolve-Path (Join-Path $PSScriptRoot '..\..')
# Si ya hay algo escuchando en el puerto (p. ej. una simulación anterior con -DejarAbierta), la simulación
# se conectaría a esa app y no a la nueva: se detiene antes de crear nada.
if (Get-NetTCPConnection -LocalPort $Puerto -State Listen -ErrorAction SilentlyContinue) {
    throw "El puerto $Puerto está ocupado (¿una simulación anterior sigue abierta?). Ciérrala o usa -Puerto <otro>."
}
$carpeta = Join-Path $proyecto ("simulacion-salida\" + (Get-Date -Format 'yyyyMMdd_HHmm'))
New-Item -ItemType Directory -Force (Join-Path $carpeta 'root'), (Join-Path $carpeta 'salida') | Out-Null

Write-Host "1/4 Compilando..."
# Se compila dentro de la carpeta de la simulación para no chocar con un 'dotnet run' abierto (que bloquea bin\).
dotnet build "$proyecto" -nologo -v q -o (Join-Path $carpeta 'app') | Out-Null
if ($LASTEXITCODE -ne 0) { throw "La compilación falló. Ejecuta 'dotnet build' para ver el error." }

Write-Host "2/4 Iniciando la app con una base nueva en $carpeta\root ..."
$env:ASPNETCORE_ENVIRONMENT = 'Development'
$dll = Join-Path $carpeta 'app\OfertaAcademica.Web.dll'
$app = Start-Process dotnet -ArgumentList "`"$dll`" --contentRoot `"$carpeta\root`" --urls http://127.0.0.1:$Puerto" `
    -PassThru -WindowStyle Hidden -RedirectStandardOutput (Join-Path $carpeta 'app.log') -RedirectStandardError (Join-Path $carpeta 'app-errores.log')

# Espera a que la app responda (la primera vez carga el catálogo del Excel).
$lista = $false
for ($i = 0; $i -lt 90 -and -not $lista; $i++) {
    Start-Sleep -Seconds 1
    try { Invoke-WebRequest "http://127.0.0.1:$Puerto/Account/Login" -UseBasicParsing -TimeoutSec 2 | Out-Null; $lista = $true } catch { }
}
if (-not $lista) { Stop-Process -Id $app.Id -Force; throw "La app no arrancó. Revisa $carpeta\app-errores.log" }

try {
    Write-Host "3/4 Generando los escenarios A, B y C (tarda unos segundos)..."
    $env:PYTHONIOENCODING = 'utf-8'
    [Console]::OutputEncoding = [Text.Encoding]::UTF8
    python -I (Join-Path $PSScriptRoot 'simular_oferta.py') "$carpeta" $Puerto
    if ($LASTEXITCODE -ne 0) { throw "La simulación falló (ver el error arriba)." }
}
finally {
    if ($DejarAbierta) {
        Write-Host ""
        Write-Host "4/4 La app sigue abierta en http://127.0.0.1:$Puerto"
        Write-Host "    Usuario: simulacion@urosario.edu.co  /  Clave: Clave-Segura-2026"
        Write-Host "    Para cerrarla: Stop-Process -Id $($app.Id)"
    } else {
        Stop-Process -Id $app.Id -Force
        Write-Host "4/4 App cerrada."
    }
}
Write-Host "Resultados: $carpeta\salida"
