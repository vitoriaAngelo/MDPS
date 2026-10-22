param([switch]$Installer)

$ErrorActionPreference = 'Stop'
Set-Location $PSScriptRoot
$env:DOTNET_CLI_HOME = Join-Path $PSScriptRoot '.cli-home'
$env:NUGET_PACKAGES = Join-Path $PSScriptRoot '.nuget'
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'

$sdk = Join-Path $PSScriptRoot '.dotnet/dotnet.exe'
if (!(Test-Path $sdk)) { $sdk = (Get-Command dotnet -ErrorAction Stop).Source }

$releaseDir = Join-Path $PSScriptRoot 'release'
$appDir = Join-Path $releaseDir 'MDPS'
$zipPath = Join-Path $releaseDir 'MDPS-portatil-win-x64.zip'
if (Test-Path $releaseDir) { Remove-Item -LiteralPath $releaseDir -Recurse -Force }
New-Item -ItemType Directory -Path $appDir | Out-Null

& $sdk publish src/MDPS/MDPS.csproj -c Release -r win-x64 --self-contained true -o $appDir -m:1 -nodeReuse:false -p:UseSharedCompilation=false
if ($LASTEXITCODE -ne 0) { throw 'A publicação falhou.' }
Copy-Item LEIA-ME.md (Join-Path $appDir 'LEIA-ME.md') -Force
Compress-Archive -Path (Join-Path $appDir '*') -DestinationPath $zipPath -CompressionLevel Optimal

if ($Installer) {
    $iscc = @(
        (Get-Command ISCC.exe -ErrorAction SilentlyContinue).Source,
        (Join-Path $env:LOCALAPPDATA 'Programs\Inno Setup 6\ISCC.exe'),
        (Join-Path $PSScriptRoot 'inno\ISCC.exe'),
        'C:\Program Files (x86)\Inno Setup 6\ISCC.exe',
        'C:\Program Files\Inno Setup 6\ISCC.exe'
    ) | Where-Object { $_ -and (Test-Path $_) } | Select-Object -First 1
    if (!$iscc) { throw 'Inno Setup 6 não encontrado. Instale-o e execute novamente com -Installer.' }
    & $iscc (Join-Path $PSScriptRoot 'installer/MDPS.iss')
    if ($LASTEXITCODE -ne 0) { throw 'A criação do instalador falhou.' }
}

Write-Host "ZIP pronto: $zipPath"
if ($Installer) { Write-Host "Instalador pronto: $(Join-Path $releaseDir 'MDPS-Setup-win-x64.exe')" }
