param(
    [string]$From = "samples/real_logs/game_001.json",
    [int]$Seed = 123,
    [switch]$StopOnDivergence
)
$cmd = @("run", "--project", "tools/ReplayRunner", "--", "replay", "--from", $From, "--seed", $Seed)
if ($StopOnDivergence) { $cmd += "--stop-on-divergence" }
Write-Host "[run_replay] invoking: dotnet $($cmd -join ' ')"
& dotnet @cmd
$exit = $LASTEXITCODE
if ($exit -ne 0) { Write-Error "[run_replay] dotnet exited with code $exit"; exit $exit }
