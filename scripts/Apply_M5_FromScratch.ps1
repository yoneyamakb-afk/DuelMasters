param(
  [Parameter(Mandatory=$false)]
  [string]$RepoRoot = "C:\Users\米山\DuelMasters"
)
$ErrorActionPreference = "Stop"
Set-Location $RepoRoot

Write-Host "=== M5 v4: apply & link & enable ==="

# 旧実装の退避（ある場合のみ）
.\scripts\Disable_Old_Trace.ps1 -RepoRoot $RepoRoot

# sln追加 & 参照付け
.\scripts\Link_Trace_Project.ps1 -RepoRoot $RepoRoot

# セッション有効化
$env:DM_TRACE = "1"
$env:DM_TRACE_DIR = Join-Path $RepoRoot ".trace"
[Environment]::SetEnvironmentVariable("DM_TRACE","1","User")
[Environment]::SetEnvironmentVariable("DM_TRACE_DIR",$env:DM_TRACE_DIR,"User")

Write-Host "Done. Run: dotnet test tests\DMRules.Tests\DMRules.Tests.csproj -v n"
