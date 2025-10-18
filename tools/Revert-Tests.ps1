param(
  [string]$Root = ".\tests\DMRules.Tests"
)
Write-Host "==> Reverting files under $Root from *.compat.bak"

$backs = Get-ChildItem $Root -Recurse -Filter *.compat.bak
$cnt = 0
foreach ($b in $backs) {
  $orig = $b.FullName -replace '\.compat\.bak$', ''
  if (Test-Path $orig) {
    Copy-Item $b.FullName $orig -Force
    Remove-Item $b.FullName -Force
    $cnt++
    Write-Host "  ~ restored: $orig"
  }
}
Write-Host "==> Restored $cnt files"
