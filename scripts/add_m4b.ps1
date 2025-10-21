param(
  [string]$SolutionPath = "..\DuelMasters.sln"
)

$ErrorActionPreference = "Stop"

# add Domain project
dotnet sln $SolutionPath add "..\src\DMRules.Domain\DMRules.Domain.csproj"

# add Domain tests
dotnet sln $SolutionPath add "..\tests\DMRules.Domain.Tests\DMRules.Domain.Tests.csproj"
