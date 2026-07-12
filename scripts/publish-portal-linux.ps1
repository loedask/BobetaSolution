# Publish Bobeta.Portal for Linux (framework-dependent, linux-x64).
# Output: Bobeta.Portal\bin\Release\net10.0\linux-x64\publish\
$ErrorActionPreference = "Stop"
$Root = Split-Path -Parent $PSScriptRoot
Set-Location $Root

dotnet publish Bobeta.Portal/Bobeta.Portal.csproj `
  -c Release `
  -r linux-x64 `
  --self-contained false `
  -o Bobeta.Portal/bin/Release/net10.0/linux-x64/publish

Write-Host ""
Write-Host "Published to Bobeta.Portal/bin/Release/net10.0/linux-x64/publish"
Write-Host "On Linux: ASPNETCORE_ENVIRONMENT=Production dotnet Bobeta.Portal.dll"
