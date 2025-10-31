param([string]$SolutionRoot = ".")
Write-Host "Running M15.1i Keyword Pack 2 tests"
pushd $SolutionRoot
try { dotnet test tests/DMRules.Tests/ -v minimal } finally { popd }
