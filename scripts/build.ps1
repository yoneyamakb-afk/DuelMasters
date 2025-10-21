param(
  [string]$Configuration = "Debug"
)

$ErrorActionPreference = "Stop"
Write-Host "=== Restore ==="
dotnet restore ../DuelMasters.sln

Write-Host "=== Build ($Configuration) ==="
dotnet build ../DuelMasters.sln -c $Configuration

Write-Host "=== Test ==="
dotnet test ../DuelMasters.sln -c $Configuration -v minimal
