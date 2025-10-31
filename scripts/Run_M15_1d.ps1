param(
    [string]$SolutionRoot = "."
)
Write-Host "Running M15.1d tests (no snapshot updates)"
$env:SNAPSHOT_UPDATE="0"
pushd $SolutionRoot
try {
    dotnet test tests/DMRules.Tests/ -v minimal
}
finally {
    popd
}
