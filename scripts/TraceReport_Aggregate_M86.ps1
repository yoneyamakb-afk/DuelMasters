param(
  [Parameter(Mandatory=$false)]
  [string]$Dir = ".\.trace",
  [Parameter(Mandatory=$false)]
  [string]$Out = "",
  [Parameter(Mandatory=$false)]
  [string]$CsvOut = "",
  [int]$SinceDays = 0,
  [string]$Start = "",
  [string]$End = "",
  [string]$FilePattern = "*.jsonl",
  [int]$TopN = 20
)
$ErrorActionPreference = "Stop"
$psMajor = $PSVersionTable.PSVersion.Major
Write-Host ("TraceReport_Aggregate (M8.6) on PS {0}" -f $psMajor)

if (-not (Test-Path $Dir)) { Write-Error ("Trace directory not found: {0}" -f $Dir); exit 1 }

function Read-Events([string]$file) {
  $events = @()
  $lineNo = 0
  Get-Content -LiteralPath $file | ForEach-Object {
    $lineNo++
    $raw = $_
    if ([string]::IsNullOrWhiteSpace($raw)) { return }
    try {
      $obj = $raw | ConvertFrom-Json
      $ev = New-Object psobject -Property ([ordered]@{
        ts      = $obj.ts
        action  = $obj.action
        phase   = $obj.phase
        player  = $obj.player
        card    = $obj.card
        details = $obj.details
        _raw    = $raw
      })
      $events += $ev
    } catch {
      $events += New-Object psobject -Property @{ ts=$null; action="parse_error"; phase=$null; player=$null; card=$null; details=@{ line=$lineNo; error=$_.Exception.Message }; _raw=$raw }
    }
  }
  return ,$events
}

$startDt = $null
$endDt = $null
if ($SinceDays -gt 0) {
  $startDt = (Get-Date).Date.AddDays(-1 * $SinceDays)
  $endDt = (Get-Date).Date.AddDays(1).AddSeconds(-1)
}
if ($Start) { try { $startDt = [datetime]::Parse($Start) } catch { Write-Error "Invalid -Start date"; exit 1 } }
if ($End)   { try { $endDt = ([datetime]::Parse($End)).Date.AddDays(1).AddSeconds(-1) } catch { Write-Error "Invalid -End date"; exit 1 } }

$files = Get-ChildItem -Path $Dir -Filter $FilePattern | Sort-Object LastWriteTime
if ($startDt) { $files = $files | Where-Object { $_.LastWriteTime -ge $startDt } }
if ($endDt)   { $files = $files | Where-Object { $_.LastWriteTime -le $endDt } }

if (-not $files) {
  Write-Host ("No files match in {0} (pattern={1})" -f $Dir, $FilePattern)
  if ($startDt -or $endDt) { Write-Host ("Timeframe: {0} .. {1}" -f $startDt, $endDt) }
  exit 0
}

$rows = @()
$totals = @{
  files = 0; events = 0;
  test_session_start = 0;
  trigger_registered = 0; trigger_resolving  = 0; trigger_resolved = 0; apnap_step = 0;
  SBA_resolve_start  = 0; SBA_resolve_end    = 0; SBA_invoke_error = 0;
  phase_change_start = 0; phase_change_end   = 0;
}
$methodFreq = @{}

foreach ($f in $files) {
  $events = Read-Events $f.FullName

  if ($startDt -or $endDt) {
    $events = $events | Where-Object {
      if (-not $_.ts) { return $true }
      try {
        $t = [datetimeoffset]$_.ts
        $ok = $true
        if ($startDt) { $ok = $ok -and ($t.UtcDateTime -ge $startDt.ToUniversalTime()) }
        if ($endDt)   { $ok = $ok -and ($t.UtcDateTime -le $endDt.ToUniversalTime()) }
        return $ok
      } catch { return $true }
    }
  }

  $totals.files++
  $totals.events += $events.Count

  $counts = ($events | Group-Object action | ForEach-Object { @{ name=$_.Name; count=$_.Count } })
  function GetCount([string]$name) {
    $c = ($counts | Where-Object { $_.name -eq $name })
    if ($c) { return $c.count } else { return 0 }
  }

  $row = New-Object psobject -Property ([ordered]@{
    file               = $f.Name
    events             = $events.Count
    trigger_registered = (GetCount "trigger_registered")
    trigger_resolving  = (GetCount "trigger_resolving")
    trigger_resolved   = (GetCount "trigger_resolved")
    apnap_step         = (GetCount "apnap_step")
    SBA_resolve_start  = (GetCount "SBA_resolve_start")
    SBA_resolve_end    = (GetCount "SBA_resolve_end")
    SBA_invoke_error   = (GetCount "SBA_invoke_error")
    phase_change_start = (GetCount "phase_change_start")
    phase_change_end   = (GetCount "phase_change_end")
    test_session_start = (GetCount "test_session_start")
  })
  $rows += $row

  foreach ($k in @("test_session_start","trigger_registered","trigger_resolving","trigger_resolved","apnap_step","SBA_resolve_start","SBA_resolve_end","SBA_invoke_error","phase_change_start","phase_change_end")) {
    $totals[$k] += $row.$k
  }

  $ev2 = $events | Where-Object { $_.action -in @("trigger_registered","trigger_resolving","trigger_resolved","apnap_step","phase_change_start","phase_change_end") }
  foreach ($e in $ev2) {
    $m = $null
    if ($e.details) { $m = $e.details.method }
    if (-not $m -and $e.details) { $m = $e.details.declaringType }
    if (-not $m) { continue }
    if (-not $methodFreq.ContainsKey($m)) { $methodFreq[$m] = 0 }
    $methodFreq[$m] += 1
  }
}

$md = @()
$md += "# Trace Aggregate Report (M8.6)"
$md += ("Generated: {0}" -f (Get-Date))
$md += ("Directory: {0}" -f (Resolve-Path $Dir))
if ($startDt -or $endDt) {
  $md += ("Timeframe: {0} .. {1}" -f $startDt, $endDt)
}
$md += ("File pattern: {0}" -f $FilePattern)
$md += ""

$md += "## Totals"
$md += ("- files              : {0}" -f $totals.files)
$md += ("- events             : {0}" -f $totals.events)
$md += ("- trigger_registered : {0}" -f $totals.trigger_registered)
$md += ("- trigger_resolving  : {0}" -f $totals.trigger_resolving)
$md += ("- trigger_resolved   : {0}" -f $totals.trigger_resolved)
$md += ("- apnap_step         : {0}" -f $totals.apnap_step)
$md += ("- SBA_resolve_start  : {0}" -f $totals.SBA_resolve_start)
$md += ("- SBA_resolve_end    : {0}" -f $totals.SBA_resolve_end)
$md += ("- SBA_invoke_error   : {0}" -f $totals.SBA_invoke_error)
$md += ("- phase_change_start : {0}" -f $totals.phase_change_start)
$md += ("- phase_change_end   : {0}" -f $totals.phase_change_end)
$md += ""

$md += "## Per-file table"
$md += ""
$md += "| file | events | trig_reg | trig_resolv | trig_resolved | apnap | SBA_start | SBA_end | SBA_err | phase_start | phase_end |"
$md += "|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|"
foreach ($r in $rows) {
  $md += ("| {0} | {1} | {2} | {3} | {4} | {5} | {6} | {7} | {8} | {9} | {10} |" -f `
    $r.file, $r.events, $r.trigger_registered, $r.trigger_resolving, $r.trigger_resolved, $r.apnap_step, $r.SBA_resolve_start, $r.SBA_resolve_end, $r.SBA_invoke_error, $r.phase_change_start, $r.phase_change_end)
}
$md += ""

$md += ("## Top methods (Top {0})" -f $TopN)
$md += ""
$md += "| method | count |"
$md += "|---|---:|"
$top = $methodFreq.GetEnumerator() | Sort-Object Value -Descending | Select-Object -First $TopN
foreach ($kv in $top) {
  $md += ("| {0} | {1} |" -f $kv.Key, $kv.Value)
}
$md += ""

if ([string]::IsNullOrWhiteSpace($Out)) {
  $date = (Get-Date).ToString("yyyyMMdd_HHmm")
  $Out = Join-Path $Dir ("TraceAggregate_{0}.md" -f $date)
}
Set-Content -LiteralPath $Out -Value ($md -join "`r`n") -Encoding UTF8
Write-Host ("Aggregate report written: {0}" -f $Out)

if ($CsvOut) {
  try {
    $rows | Export-Csv -LiteralPath $CsvOut -NoTypeInformation -Encoding UTF8
    Write-Host ("CSV written: {0}" -f $CsvOut)
  } catch {
    Write-Warning ("CSV failed: {0}" -f $_.Exception.Message)
  }
}
