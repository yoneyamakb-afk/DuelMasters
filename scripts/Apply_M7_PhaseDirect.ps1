param(
  [Parameter(Mandatory=$false)]
  [string]$RepoRoot = "C:\Users\米山\DuelMasters"
)
$ErrorActionPreference = "Stop"

Write-Host "=== M7 v7: Phase direct hook patcher (safe & idempotent) ==="
$engineDir = Join-Path $RepoRoot "src\DMRules.Engine"
if (-not (Test-Path $engineDir)) { throw "Not found: $engineDir" }

$stamp = (Get-Date).ToString("yyyyMMdd_HHmmss")
$bakDir = Join-Path $RepoRoot "scripts\_backup_M7_$stamp"
New-Item -ItemType Directory -Force -Path $bakDir | Out-Null

# Candidate files likely holding phase logic
$candidates = Get-ChildItem -Recurse -Path $engineDir -Include *Phase*.cs,*Turn*.cs,*Duel*.cs,*Game*.cs -ErrorAction SilentlyContinue
if (-not $candidates) {
  Write-Warning "No candidate files matched (*Phase*.cs,*Turn*.cs,*Duel*.cs,*Game*.cs). Exiting without changes."
  exit 0
}

function Ensure-UsingTracing([string]$content) {
  if ($content -match 'using\s+DMRules\.Engine\.Tracing\s*;') { return $content }
  $regex = [regex]'(?s)^(?<head>(?:\s*using\s+[^\r\n]+;\s*)+)(?<rest>.*)$'
  $m = $regex.Match($content)
  if ($m.Success) { return ($m.Groups['head'].Value + "using DMRules.Engine.Tracing;`r`n" + $m.Groups['rest'].Value) }
  return ("using DMRules.Engine.Tracing;`r`n" + $content)
}

# Methods that look like phase enter/execute
$methodPattern = '(?<sig>\b(?:public|internal|private|protected)\s+(?:static\s+)?(?:async\s+)?[^\(\r\n]+\s+(?<name>\w*(?:Enter|Begin|Start|Execute|Next|Advance)\w*(?:Phase|Main|Battle|End)\w*)\s*\([^\)]*\)\s*)\{'

$inStartTpl = 'EngineTrace.Event("phase_change_start", details: new() { ["method"] = "{{NAME}}"});'
$inEndTpl   = 'EngineTrace.Event("phase_change_end",   details: new() { ["method"] = "{{NAME}}"});'

foreach ($f in $candidates) {
  $src = Get-Content $f.FullName -Raw -Encoding UTF8
  $orig = $src
  $src = Ensure-UsingTracing $src

  if ($src -match 'EngineTrace\.Event\("phase_change_start"') {
    Write-Host "Already patched (skipped): $($f.FullName)"
    continue
  }

  $m = [System.Text.RegularExpressions.Regex]::Matches($src, $methodPattern, 'IgnoreCase, Singleline')
  if ($m.Count -eq 0) {
    Write-Host "No phase-like methods in: $($f.FullName)"
    continue
  }

  $sb = New-Object System.Text.StringBuilder
  $lastPos = 0
  foreach ($mm in $m) {
    $name = $mm.Groups['name'].Value
    $openBraceIndex = $mm.Index + $mm.Length - 1
    $null = $sb.Append($src.Substring($lastPos, $openBraceIndex - $lastPos + 1))
    $startLine = $inStartTpl.Replace("{{NAME}}", $name)
    $null = $sb.Append("`r`n            " + $startLine + "`r`n")
    $lastPos = $openBraceIndex + 1
  }
  $null = $sb.Append($src.Substring($lastPos))
  $tmp = $sb.ToString()

  $endLine = $inEndTpl.Replace("{{NAME}}", "?")
  $tmp2 = [System.Text.RegularExpressions.Regex]::Replace($tmp, '(\breturn\b)', $endLine + "`r`n            `$1", 'IgnoreCase')
  if ($tmp2 -eq $tmp) {
    $tmp2 = $tmp2 -replace '\}\s*$', '            ' + $endLine + "`r`n}"
  }

  if ($tmp2 -ne $orig) {
    Copy-Item $f.FullName (Join-Path $bakDir $f.Name)
    Set-Content -Path $f.FullName -Value $tmp2 -Encoding UTF8
    Write-Host "Patched: $($f.FullName)"
  } else {
    Write-Host "No changes applied to: $($f.FullName)"
  }
}

Write-Host "Done. Backup at $bakDir"
