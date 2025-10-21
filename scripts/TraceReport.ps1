param(
  [Parameter(Mandatory=$false)]
  [string]$Dir = ".\.trace",
  [Parameter(Mandatory=$false)]
  [string]$Out = "",
  [switch]$PerFile,
  [int]$Timeline = 50
)
$ErrorActionPreference = "Stop"

# PS version banner
$psMajor = $PSVersionTable.PSVersion.Major
Write-Host "TraceReport.ps1 running on PowerShell $psMajor"

if (-not (Test-Path $Dir)) {
  Write-Error "Trace directory not found: $Dir"
  exit 1
}

function Read-Events([string]$file) {
  $events = @()
  $lineNo = 0
  Get-Content -LiteralPath $file | ForEach-Object {
    $lineNo++
    $raw = $_
    if ([string]::IsNullOrWhiteSpace($raw)) { return }
    try {
      $obj = $raw | ConvertFrom-Json
      $ev = [ordered]@{
        ts      = $obj.ts
        action  = $obj.action
        phase   = $obj.phase
        player  = $obj.player
        card    = $obj.card
        details = $obj.details
        _raw    = $raw
      }
      $events += New-Object psobject -Property $ev
    } catch {
      $events += New-Object psobject -Property @{ ts = $null; action = "parse_error"; phase = $null; player = $null; card = $null; details = @{ line = $lineNo; error = $_.Exception.Message }; _raw = $raw }
    }
  }
  return ,$events
}

function Summarize-File([string]$file, [int]$TimelineCount) {
  $events = Read-Events $file
  $name = Split-Path $file -Leaf
  $lines = @()

  $lines += "### $name"
  $lines += ""

  if ($events.Count -eq 0) {
    $lines += "_No events_"
    return ($lines -join "`r`n")
  }

  # counts by action
  $counts = $events | Group-Object action | Sort-Object Count -Descending
  $lines += "**Actions summary**"
  foreach ($g in $counts) {
    $lines += ("- `{0}`: {1}" -f $g.Name, $g.Count)
  }
  $lines += ""

  # phase transitions
  $phaseStart = $events | Where-Object { $_.action -eq "phase_change_start" }
  $phaseEnd   = $events | Where-Object { $_.action -eq "phase_change_end" }
  if (($phaseStart -and $phaseStart.Count -gt 0) -or ($phaseEnd -and $phaseEnd.Count -gt 0)) {
    $lines += "**Phase changes (detected)**"
    foreach ($p in $phaseStart) {
      $ph = $null
      if ($p.details) { $ph = $p.details.phase }
      if (-not $ph -and $p.details) { $ph = $p.details.method }
      if (-not $ph) { $ph = "(unknown)" }
      $lines += ("- start: {0}" -f $ph)
    }
    foreach ($p in $phaseEnd) {
      $ph = $null
      if ($p.details) { $ph = $p.details.phase }
      if (-not $ph -and $p.details) { $ph = $p.details.method }
      if (-not $ph) { $ph = "(unknown)" }
      $lines += ("- end  : {0}" -f $ph)
    }
    $lines += ""
  }

  # trigger stats
  $trRegs = 0; $trResl = 0; $apnap = 0
  if ($events) {
    $trRegs = ($events | Where-Object { $_.action -eq "trigger_registered" }).Count
    $trResl = ($events | Where-Object { $_.action -eq "trigger_resolved" }).Count
    $apnap  = ($events | Where-Object { $_.action -eq "apnap_step" }).Count
  }
  if ($trRegs -gt 0 -or $trResl -gt 0 -or $apnap -gt 0) {
    $lines += "**Triggers/APNAP**"
    $lines += ("- registered: {0}" -f $trRegs)
    $lines += ("- resolved  : {0}" -f $trResl)
    $lines += ("- apnap     : {0}" -f $apnap)
    $lines += ""
  }

  # SBA stats
  $sbaStart = ($events | Where-Object { $_.action -eq "SBA_resolve_start" }).Count
  $sbaEnd   = ($events | Where-Object { $_.action -eq "SBA_resolve_end" }).Count
  $sbaErr   = ($events | Where-Object { $_.action -eq "SBA_invoke_error" }).Count
  if ($sbaStart -gt 0 -or $sbaEnd -gt 0 -or $sbaErr -gt 0) {
    $lines += "**SBA**"
    $lines += ("- start: {0}" -f $sbaStart)
    $lines += ("- end  : {0}" -f $sbaEnd)
    $lines += ("- error: {0}" -f $sbaErr)
    $lines += ""
  }

  # timeline
  $lines += ("**Timeline (first {0} events)**" -f $TimelineCount)
  $i = 0
  foreach ($e in ($events | Select-Object -First $TimelineCount)) {
    $i++
    $ts = $e.ts
    if ($ts -is [DateTime] -or $ts -is [DateTimeOffset]) {
      $ts = [DateTimeOffset]$ts
      $ts = $ts.ToString("HH:mm:ss.fff")
    }
    $short = ""
    if ($e.details) { try { $short = ($e.details | ConvertTo-Json -Compress -Depth 2) } catch {} }
    $lines += ("{0,3}. {1,-12} {2,-24} {3}" -f $i, $ts, $e.action, $short)
  }
  $lines += ""

  return ($lines -join "`r`n")
}

$files = Get-ChildItem -Path $Dir -Filter *.jsonl | Sort-Object LastWriteTime
if (-not $files) {
  Write-Host ("No .jsonl found in {0}" -f $Dir)
  exit 0
}

$report = @()
$report += "# Trace Report"
$report += ("Generated: {0}" -f (Get-Date))
$report += ("Directory: {0}" -f (Resolve-Path $Dir))
$report += ""

if ($PerFile) {
  foreach ($f in $files) {
    $report += (Summarize-File -file $f.FullName -TimelineCount $Timeline)
  }
} else {
  $latest = $files[-1]
  $report += (Summarize-File -file $latest.FullName -TimelineCount $Timeline)
}

$md = $report -join "`r`n"

if ([string]::IsNullOrWhiteSpace($Out)) {
  $date = (Get-Date).ToString("yyyyMMdd_HHmm")
  $Out = Join-Path $Dir ("TraceReport_{0}.md" -f $date)
}
Set-Content -LiteralPath $Out -Value $md -Encoding UTF8

Write-Host ("Report written: {0}" -f $Out)
Write-Host "`n---`n"
Write-Output $md
