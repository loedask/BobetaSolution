#!/usr/bin/env bash
# Publish Bobeta.Portal for Linux (framework-dependent, linux-x64).
# Output: Bobeta.Portal/bin/Release/net10.0/linux-x64/publish/
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT"

dotnet publish Bobeta.Portal/Bobeta.Portal.csproj \
  -c Release \
  -r linux-x64 \
  --self-contained false \
  -o Bobeta.Portal/bin/Release/net10.0/linux-x64/publish

echo ""
echo "Published to Bobeta.Portal/bin/Release/net10.0/linux-x64/publish"
echo "Run on Linux: ASPNETCORE_ENVIRONMENT=Production dotnet Bobeta.Portal.dll"
