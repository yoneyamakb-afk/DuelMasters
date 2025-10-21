param()
Write-Host "[bootstrap] Restoring and building tools..."

dotnet restore tools/ReplayRunner
if ($LASTEXITCODE -ne 0) { throw "restore failed: ReplayRunner" }
dotnet restore tools/TraceDiff
if ($LASTEXITCODE -ne 0) { throw "restore failed: TraceDiff" }

dotnet build tools/ReplayRunner -c Debug -v minimal
if ($LASTEXITCODE -ne 0) { throw "build failed: ReplayRunner" }
dotnet build tools/TraceDiff -c Debug -v minimal
if ($LASTEXITCODE -ne 0) { throw "build failed: TraceDiff" }

Write-Host "[bootstrap] done."
