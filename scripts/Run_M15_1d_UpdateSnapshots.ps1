param(
    [string]$SolutionRoot = "."
)
Write-Host "Running M15.1d with SNAPSHOT_UPDATE=1 (re-approve snapshots on mismatch)"
$env:SNAPSHOT_UPDATE="1"
pushd $SolutionRoot
try {
    dotnet test tests/DMRules.Tests/ -v minimal
}
finally {
    popd
}
