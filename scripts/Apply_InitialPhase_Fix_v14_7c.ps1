<<SCRIPT START>>
Param(
  [string]$Root = (Resolve-Path "..").Path
)

$ErrorActionPreference = "Stop"

# --- 対象ディレクトリ確認 ---
$engineDir = Join-Path $Root "src\DuelMasters.Engine"
if (!(Test-Path $engineDir)) { throw "Engine dir not found: $engineDir" }

# --- バックアップ作成（フォルダ構造ごと） ---
$stamp = Get-Date -Format "yyyyMMdd_HHmmss"
$backupDir = Join-Path $engineDir ("_backup_initialphase_" + $stamp)
New-Item -ItemType Directory $backupDir -Force | Out-Null

Get-ChildItem $engineDir -Recurse -Include *.cs |
  ForEach-Object {
    $rel = $_.FullName.Substring($engineDir.Length).TrimStart('\')
    $dest = Join-Path $backupDir $rel
    $destDir = Split-Path $dest
    if (!(Test-Path $destDir)) { New-Item -ItemType Directory $destDir -Force | Out-Null }
    Copy-Item $_.FullName $dest -Force
  }

# --- ログ構築 ---
$log = New-Object System.Collections.Generic.List[string]
$replacedPhase = 0
$insertedPriority = 0
$files = Get-ChildItem $engineDir -Recurse -Include *.cs

# --- (1) Phase.Start -> Phase.Main を網羅置換 ---
foreach ($f in $files) {
  $text = Get-Content $f.FullName -Raw
  $new  = $text -replace 'Phase\s*=\s*Phase\.Start','Phase = Phase.Main'
  $new  = $new  -replace 'Phase\.Start','Phase.Main'  # 比較/初期化なども網羅
  if ($new -ne $text) {
    Set-Content $f.FullName $new -Encoding UTF8
    $replacedPhase++
    $log.Add("Phase.Start->Main : $($f.FullName)")
  }
}

# --- (2) state.TurnPlayer の直後に Priority を付与（重複防止付） ---
foreach ($f in $files) {
  $text = Get-Content $f.FullName -Raw
  if ($text -notmatch 'PriorityPlayer\s*=\s*.*TurnPlayer') {
    $new = [regex]::Replace(
      $text,
      '(state\s*\.\s*TurnPlayer\s*=\s*[^;]+;)',
      "`$1`r`n            state.PriorityPlayer = state.TurnPlayer;"
    )
    if ($new -ne $text) {
      Set-Content $f.FullName $new -Encoding UTF8
      $insertedPriority++
      $log.Add("Inserted PriorityPlayer=TurnPlayer : $($f.FullName)")
    }
  }
}

# --- ログ出力 ---
$logPath = Join-Path $engineDir ("_initialphase_fix_log_" + $stamp + ".txt")
"InitialPhase Fix v14.7d" | Set-Content $logPath -Encoding UTF8
"EngineDir: $engineDir"   | Add-Content $logPath
"Replaced Phase.Start→Main files: $replacedPhase" | Add-Content $logPath
"Inserted Priority lines : $insertedPriority"     | Add-Content $logPath
"" | Add-Content $logPath
$log | Add-Content $logPath

Write-Host "✅ InitialPhase fix applied."
Write-Host "🗂  Backup: $backupDir"
Write-Host "📝  Log:    $logPath"
Write-Host "Next: dotnet clean; dotnet build; dotnet test"
<<SCRIPT END>>
