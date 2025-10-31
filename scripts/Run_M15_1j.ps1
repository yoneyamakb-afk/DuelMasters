param([string]$SolutionRoot = ".")
Write-Host "Running M15.1j keyword fix quick tests"
pushd $SolutionRoot
try { dotnet test tests/DMRules.Tests/ -v minimal } finally { popd }
