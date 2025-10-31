param([string]$SolutionRoot = ".")
Write-Host "Running M15.2d tests" -ForegroundColor Cyan
pushd $SolutionRoot
try { dotnet test tests\DMRules.Tests -v minimal } finally { popd }
