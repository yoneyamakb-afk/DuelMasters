# Apply M15.2d: replace CardTextTemplates.cs with PATCH, then build & test
# Usage: .\scripts\Apply_M15_2d.ps1
$patch = "src\DMRules.Engine\TextParsing\CardTextTemplates_PATCH.cs"
$target = "src\DMRules.Engine\TextParsing\CardTextTemplates.cs"

if (-not (Test-Path $patch)) { Write-Error "PATCH file not found: $patch"; exit 2 }
if (Test-Path $target) { Remove-Item $target -Force }
Rename-Item $patch "CardTextTemplates.cs"

dotnet test tests\DMRules.Tests -v minimal
