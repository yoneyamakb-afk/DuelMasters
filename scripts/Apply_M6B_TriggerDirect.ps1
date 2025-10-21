param(
  [Parameter(Mandatory=$false)]
  [string]$RepoRoot = "C:\Users\米山\DuelMasters"
)
$ErrorActionPreference = "Stop"

Write-Host "=== M6-B v6b2: Trigger direct hook patcher (fixed regex) ==="
$engineDir = Join-Path $RepoRoot "src\DMRules.Engine"
if (-not (Test-Path $engineDir)) { throw "Not found: $engineDir" }

$stamp = (Get-Date).ToString("yyyyMMdd_HHmmss")
$bakDir = Join-Path $RepoRoot "scripts\_backup_M6B_$stamp"
New-Item -ItemType Directory -Force -Path $bakDir | Out-Null

# Candidate files
$candidates = Get-ChildItem -Recurse -Path $engineDir -Include *Trigger*.cs,*Resolver*.cs,*APNAP*.cs -ErrorAction SilentlyContinue
if (-not $candidates) {
  Write-Warning "No candidate files matched (*Trigger*.cs,*Resolver*.cs,*APNAP*.cs). Exiting without changes."
  exit 0
}

function Ensure-UsingTracing([string]$content) {
  if ($content -match 'using\s+DMRules\.Engine\.Tracing\s*;') { return $content }
  $regex = [regex]'(?s)^(?<head>(?:\s*using\s+[^\r\n]+;\s*)+)(?<rest>.*)$'
  $m = $regex.Match($content)
  if ($m.Success) {
    return ($m.Groups['head'].Value + "using DMRules.Engine.Tracing;`r`n" + $m.Groups['rest'].Value)
  } else {
    return ("using DMRules.Engine.Tracing;`r`n" + $content)
  }
}

# Method signature pattern: Resolve*Trigger* / Apply*Trigger*
$methodPattern = '(?<sig>\b(?:public|internal|private|protected)\s+(?:static\s+)?(?:async\s+)?[^\(\r\n]+\s+(?<name>\w*(?:Resolve|Apply)\w*?Trigger\w*)\s*\([^\)]*\)\s*)\{'

$injectedStartTpl = 'EngineTrace.Event("trigger_resolving", details: new() { ["method"] = "{{NAME}}"});'
$injectedEndTpl   = 'EngineTrace.Event("trigger_resolved",  details: new() { ["method"] = "{{NAME}}"});'

foreach ($f in $candidates) {
  $src = Get-Content $f.FullName -Raw -Encoding UTF8
  $orig = $src
  $src = Ensure-UsingTracing $src

  # Idempotency: skip if already injected
  if ($src -match 'EngineTrace\.Event\("trigger_resolving"') {
    Write-Host "Already patched (skipped): $($f.FullName)"
    continue
  }

  $m = [System.Text.RegularExpressions.Regex]::Matches($src, $methodPattern, 'IgnoreCase, Singleline')
  if ($m.Count -eq 0) {
    Write-Host "No trigger-like methods in: $($f.FullName)"
    continue
  }

  $sb = New-Object System.Text.StringBuilder
  $lastPos = 0
  foreach ($mm in $m) {
    $name = $mm.Groups['name'].Value
    $openBraceIndex = $mm.Index + $mm.Length - 1  # position of '{'

    # append up to '{'
    $null = $sb.Append($src.Substring($lastPos, $openBraceIndex - $lastPos + 1))

    # inject start
    $startLine = $injectedStartTpl.Replace("{{NAME}}", $name)
    $null = $sb.Append("`r`n            " + $startLine + "`r`n")

    $lastPos = $openBraceIndex + 1
  }
  # append the rest of the file
  $null = $sb.Append($src.Substring($lastPos))
  $tmp = $sb.ToString()

  # inject end-event before each 'return ' inside target methods (heuristic, safe)
  $endLine = $injectedEndTpl.Replace("{{NAME}}", "?")
  $tmp2 = [System.Text.RegularExpressions.Regex]::Replace($tmp, '(\breturn\b)', $endLine + "`r`n            `$1", 'IgnoreCase')

  # also try to inject at file end if no explicit return was found
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
