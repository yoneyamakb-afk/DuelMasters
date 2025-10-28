// M13 fix: correct global using alias syntax for CLI.
// Place at: src/DuelMasters.CLI/Usings.Compat.cs
global using DMEI = DMRules.Engine.Integration;
// If you want to replace legacy calls, you can do:
// using static DMEI.EngineWireup;
// using static DMEI.EngineHooks;
