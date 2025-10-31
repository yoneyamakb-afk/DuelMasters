param(
    [string]$SolutionRoot = "."
)

Write-Host "Running M15.1c + M15.2b tests..."
pushd $SolutionRoot
try {
    dotnet test tests/DMRules.Tests/ -v minimal
}
finally {
    popd
}
