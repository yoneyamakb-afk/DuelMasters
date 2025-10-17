
# Duel Masters Simulator Skeleton

A minimal, testable skeleton to build a **rules-accurate** Duel Masters engine first, then layer AI on top.
Targets **.NET 8** (VS2022) and uses **SQLite** with the DB file name **Duelmasters.db**.

## Layout
```
/src
  DuelMasters.Engine/     # Core rules engine (immutable GameState, zones, priority/stack loop)
  DuelMasters.Cards/      # Card JSON + schema + loader (data-driven abilities)
  DuelMasters.CLI/        # Console runner (demo match Random vs Random, DB wiring)
/tests
  DuelMasters.Tests/      # xUnit + FsCheck smoke tests
Duelmasters.db            # (your DB lives next to the solution; CLI uses 'Data Source=Duelmasters.db')
```

## Quick Start
1. Open **DuelMasters.sln** in VS2022
2. `Restore` -> `Build`
3. Set **DuelMasters.CLI** as startup project and `Run`

Or with CLI:
```bash
dotnet build
dotnet test
dotnet run --project src/DuelMasters.CLI
```

## Notes
- Randomness is driven by `IRandomSource` with seed injection for reproducible sims.
- Engine exposes `ISimulator` for AI (MCTS etc.).
- Card data lives in `/src/DuelMasters.Cards/cards/` as JSON validated by `cards.schema.json`.
- The CLI uses `Microsoft.Data.Sqlite` with `Data Source=Duelmasters.db`.
