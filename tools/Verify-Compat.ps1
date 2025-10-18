param(
  [string]$Root = ".\tests\DMRules.Tests"
)
Write-Host "==> Scanning for string-based Step/ApplyReplacement calls under $Root"

$files = Get-ChildItem $Root -Recurse -Filter *.cs |
  ?{ $_.FullName -notmatch '\\(bin|obj)\\' }

$hits = @()
foreach ($f in $files) {
  $t = Get-Content $f.FullName -Raw
  if ([Regex]::IsMatch($t, '\.Step\([^)]*"(MainPhase|Normal|[^"]+)"', 'IgnoreCase, Singleline') -or
      [Regex]::IsMatch($t, '\.ApplyReplacement\([^)]*Dictionary<', 'IgnoreCase, Singleline')) {
    $hits += $f.FullName
  }
}
if ($hits.Count -eq 0) {
  Write-Host "OK: no obvious string-based calls found."
} else {
  Write-Warning "Found candidates:"
  $hits | % { Write-Host "    $_" }
}
