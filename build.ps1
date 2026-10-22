param([switch]$Test)
$ErrorActionPreference = 'Stop'
Set-Location $PSScriptRoot
$env:DOTNET_CLI_HOME = Join-Path $PSScriptRoot '.cli-home'
$env:NUGET_PACKAGES = Join-Path $PSScriptRoot '.nuget'
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
$sdk = Join-Path $PSScriptRoot '.dotnet/dotnet.exe'
if (!(Test-Path $sdk)) { $sdk = (Get-Command dotnet -ErrorAction Stop).Source }
if ($Test) {
    & $sdk test tests/MDPS.Tests/MDPS.Tests.csproj -c Release -m:1 -nodeReuse:false -p:UseSharedCompilation=false
    if ($LASTEXITCODE -ne 0) { throw 'Os testes falharam.' }
}
& $sdk publish src/MDPS/MDPS.csproj -c Release -r win-x64 --self-contained true -o dist/MDPS -m:1 -nodeReuse:false -p:UseSharedCompilation=false
if ($LASTEXITCODE -ne 0) { throw 'A compilação falhou.' }
Copy-Item LEIA-ME.md dist/MDPS/LEIA-ME.md -Force
Write-Host 'Pronto: dist/MDPS/MDPS.exe'
