param(
    [string]$ExpectedRealLog = "samples/real_logs/game_001.json",
    [string]$ActualTrace = "artifacts/actual/game_001.trace.json",
    [string]$OutDir = "artifacts/diff",
    [int]$Seed = 123
)

# 1) Prepare expected trace (normalized) using the same converter as ReplayRunner
$expectedDir = "artifacts/expected"
if (!(Test-Path $expectedDir)) { New-Item -ItemType Directory -Force -Path $expectedDir | Out-Null }

$expectedTrace = Join-Path $expectedDir ("{0}.trace.json" -f [System.IO.Path]::GetFileNameWithoutExtension($ExpectedRealLog))

Write-Host "[compare_traces] preparing expected trace via ReplayRunner ..."
$prep = @("run","--project","tools/ReplayRunner","--","replay","--from",$ExpectedRealLog,"--seed",$Seed)
Write-Host "[compare_traces] invoking: dotnet $($prep -join ' ')"
& dotnet @prep
if ($LASTEXITCODE -ne 0) { Write-Error "[compare_traces] failed to build expected trace"; exit $LASTEXITCODE }

# ReplayRunner writes to artifacts/actual by default; copy to expected location for clean separation
$actualFromPrep = Join-Path "artifacts/actual" ([System.IO.Path]::GetFileName($expectedTrace))
if (Test-Path $actualFromPrep) {
    Copy-Item $actualFromPrep $expectedTrace -Force
} elseif (Test-Path $expectedTrace) {
    # already produced
} else {
    Write-Error "[compare_traces] expected trace not found after preparation: $expectedTrace"
    exit 2
}

# 2) Now run TraceDiff on two **normalized traces**
$cmd = @("run","--project","tools/TraceDiff","--","diff","--expect",$expectedTrace,"--actual",$ActualTrace,"--out",$OutDir)
Write-Host "[compare_traces] invoking: dotnet $($cmd -join ' ')"
& dotnet @cmd
$exit = $LASTEXITCODE
if ($exit -eq 3) {
    Write-Warning "[compare_traces] traces differ (exit=3). See $OutDir\diff.txt and diff.json."
} elseif ($exit -ne 0) {
    Write-Error "[compare_traces] TraceDiff failed (exit=$exit)"
}
exit $exit
