param(
  [string]$Root = ".\tests\DMRules.Tests"
)
Write-Host "==> Patching tests under $Root"

# 1) Drop-in helper
$helperSrc = Join-Path $PSScriptRoot "..\tests\DMRules.Tests\EngineCompat.cs"
$helperDst = Join-Path $Root "EngineCompat.cs"
Copy-Item $helperSrc $helperDst -Force
Write-Host "  + wrote EngineCompat.cs"

# 2) Collect cs files (exclude bin/obj and the helper itself)
$files = Get-ChildItem $Root -Recurse -Filter *.cs |
  ?{ $_.FullName -notmatch '\\(bin|obj)\\' -and $_.Name -ne 'EngineCompat.cs' }

$changed = @()
foreach ($f in $files) {
  $t = Get-Content $f.FullName -Raw
  $orig = $t

  # Backup once per file
  $bak = "$($f.FullName).compat.bak"
  if (-not (Test-Path $bak)) { Copy-Item $f.FullName $bak }

  # Replace receiver calls:   recv.Step( ... )  →  EngineCompat.Step(recv, ... )
  $t = [Regex]::Replace($t,
    '(?<recv>(?>[^;\r\n\(\)]+|\((?>[^()]+|(?<open>\()|(?<-open>\)))*(?(open)(?!))\))?)\s*\.\s*Step\s*\(',
    'DMRules.Engine.EngineCompat.Step(${recv}, ',
    'IgnoreCase, CultureInvariant, Singleline')

  # Replace ApplyReplacement receiver calls similarly
  $t = [Regex]::Replace($t,
    '(?<recv>(?>[^;\r\n\(\)]+|\((?>[^()]+|(?<open>\()|(?<-open>\)))*(?(open)(?!))\))?)\s*\.\s*ApplyReplacement\s*\(',
    'DMRules.Engine.EngineCompat.ApplyReplacement(${recv}, ',
    'IgnoreCase, CultureInvariant, Singleline')

  if ($t -ne $orig) {
    Set-Content $f.FullName $t -Encoding UTF8
    $changed += $f.FullName
    Write-Host "  ~ rewrote: $($f.FullName)"
  }
}

# 3) Summary + verify
$report = Join-Path $PSScriptRoot "compat-changed.txt"
$changed | Set-Content $report -Encoding UTF8

Write-Host "`n==> Changed $($changed.Count) files"
if ($changed.Count -eq 0) {
  Write-Warning "No files were rewritten. Check patterns or file locations."
}

# Verify: any remaining .Step(... "string" ...) 
$left = @()
foreach ($f in $files) {
  $t = Get-Content $f.FullName -Raw
  if ([Regex]::IsMatch($t, '\.Step\([^)]*"(MainPhase|Normal|[^"]+)"', 'IgnoreCase, Singleline')) {
    $left += $f.FullName
  }
}
if ($left.Count -gt 0) {
  Write-Warning "Possible remaining string-based Step calls in:"
  $left | % { Write-Host "    $_" }
} else {
  Write-Host "OK: no obvious string-based Step calls remain."
}

Write-Host "`nNext:"
Write-Host "  dotnet clean"
Write-Host "  dotnet build"
Write-Host "  dotnet test tests\DMRules.Tests\DMRules.Tests.csproj"
