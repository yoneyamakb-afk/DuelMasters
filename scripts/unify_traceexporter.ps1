# scripts\unify_traceexporter.ps1
# Removes old compat/partial files and optional test shims, then prints tree preview.

param(
  [string]$RepoRoot = "C:\Users\米山\DuelMasters"
)

$files = @(
  "src\DMRules.Engine\Tracing\TraceExporter.Compat.Final.cs",
  "src\DMRules.Engine\Tracing\TraceExporter.JsonCompat.cs",
  "src\DMRules.Engine\Tracing\TraceExporter.FlushCompat.cs",
  "src\DMRules.Engine\Tracing\TraceExporter.TestAndDemoCompat.cs",
  "src\DMRules.Engine\Tracing\TraceExporter.CoreIO.cs",
  "src\DMRules.Engine\Tracing\TraceExporter.WriteCompat.cs",
  "tests\DMRules.Tests\TraceCompat.cs"
)

foreach ($rel in $files) {
  $p = Join-Path $RepoRoot $rel
  if (Test-Path $p) {
    Write-Host "Removing $rel"
    Remove-Item $p -Force -ErrorAction SilentlyContinue
  }
}

# Optional: remove Directory.Build.props if it only existed to include TraceCompat.cs
$props = Join-Path $RepoRoot "Directory.Build.props"
if (Test-Path $props) {
  $content = Get-Content $props -Raw -ErrorAction SilentlyContinue
  if ($content -match "TraceCompat\.cs") {
    Write-Host "Removing Directory.Build.props (TraceCompat include detected)"
    Remove-Item $props -Force -ErrorAction SilentlyContinue
  }
}

Write-Host "`nDone. Suggested next steps:"
Write-Host "  dotnet clean"
Write-Host "  dotnet build"
Write-Host "  dotnet test tests\DMRules.Tests\DMRules.Tests.csproj -v q"
