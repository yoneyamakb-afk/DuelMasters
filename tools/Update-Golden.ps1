param(
    [switch]$DryRun  # -DryRun を付けると環境変数はセットせず挙動だけ確認
)

Write-Host "[Update-Golden] Start" -ForegroundColor Cyan
$prev = $env:SNAPSHOT_UPDATE

try {
    if (-not $DryRun) {
        $env:SNAPSHOT_UPDATE = "1"
        Write-Host "[Update-Golden] SNAPSHOT_UPDATE=1" -ForegroundColor Yellow
    } else {
        Write-Host "[Update-Golden] DryRun: SNAPSHOT_UPDATE not set" -ForegroundColor DarkYellow
    }

    # Golden再生成
    dotnet test -v minimal

} finally {
    # 後始末（元に戻す）
    if (-not $DryRun) {
        if ($null -ne $prev) {
            $env:SNAPSHOT_UPDATE = $prev
            Write-Host "[Update-Golden] SNAPSHOT_UPDATE restored to previous value" -ForegroundColor Yellow
        } else {
            Remove-Item Env:\SNAPSHOT_UPDATE -ErrorAction SilentlyContinue
            Write-Host "[Update-Golden] SNAPSHOT_UPDATE removed" -ForegroundColor Yellow
        }
    }
    Write-Host "[Update-Golden] Done" -ForegroundColor Cyan
}
