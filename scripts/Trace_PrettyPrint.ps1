param(
  [Parameter(Mandatory=$false)]
  [string]$Path = "C:\Users\米山\DuelMasters\.trace"
)
if (-not (Test-Path $Path)) { Write-Host "Not found: $Path"; exit 0 }
Get-ChildItem $Path -Filter *.jsonl | Sort-Object LastWriteTime | ForEach-Object {
  Write-Host "=== $($_.FullName) ==="
  Get-Content $_.FullName | ForEach-Object {
    try { ($_ | ConvertFrom-Json) | Format-List * } catch { Write-Host $_ }
  }
}
