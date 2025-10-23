using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace RegulationAdapter
{
    public static class Adapter
    {
        private static bool Verbose => (Environment.GetEnvironmentVariable("REG_ADAPTER_VERBOSE") ?? "0") == "1";

        public static void OnEvent(object gameState, object simEvent, object sourceCard)
        {
            try
            {
                var ast = TryLoadAstForCard(sourceCard);
                if (ast is null) return;
                ApplyFlagsToCard(ast, sourceCard);

                if (ast.TryGetPropertyValue("effects", out var effNode) && effNode is JsonArray effs)
                {
                    foreach (var n in effs)
                        if ((n as JsonObject)?["kind"]?.ToString()?.ToLowerInvariant() == "replacement")
                            TryApplyReplacement(n as JsonObject, gameState, simEvent, sourceCard);
                    foreach (var n in effs)
                    {
                        var kind = (n as JsonObject)?["kind"]?.ToString()?.ToLowerInvariant();
                        if (kind == "triggered" || kind == "activated")
                            TryApplyTriggered(n as JsonObject, gameState, simEvent, sourceCard);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("[RegulationAdapter] OnEvent failed: " + ex);
            }
        }

        public static void ApplyCardStaticFlags(object gameState, object card)
        {
            try
            {
                var ast = TryLoadAstForCard(card);
                if (ast is null) return;
                ApplyFlagsToCard(ast, card);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("[RegulationAdapter] ApplyCardStaticFlags failed: " + ex);
            }
        }

        private static JsonObject? TryLoadAstForCard(object card)
        {
            var cardId = TryGetCardId(card);
            if (string.IsNullOrWhiteSpace(cardId))
            {
                var name = TryGetProp(card, "Name") as string ?? TryGetProp(card, "name") as string ?? "(unknown)";
                Console.Error.WriteLine($"[RegulationAdapter] cardId not found for '{name}'");
                return null;
            }

            foreach (var baseDir in GetSearchBases())
            {
                var p1 = Path.Combine(baseDir, "cards_ast", "generated", $"{SanitizeFileName(cardId!)}.json");
                var p2 = Path.Combine(baseDir, "cards_ast", "samples",   $"{SanitizeFileName(cardId!)}.json");
                foreach (var p in new[] { p1, p2 })
                {
                    if (File.Exists(p))
                    {
                        try
                        {
                            var node = JsonNode.Parse(File.ReadAllText(p)) as JsonObject;
                            if (node is not null) return node;
                        }
                        catch (Exception ex)
                        {
                            Console.Error.WriteLine($"[RegulationAdapter] AST parse error for {p}: {ex.Message}");
                        }
                    }
                }
            }

            // FIXED scope: properly scoped using statements for embedded resource fallback
            var asm = Assembly.GetExecutingAssembly();
            var resName = asm.GetManifestResourceNames()
                             .FirstOrDefault(n => n.EndsWith($".cards_ast.samples.{cardId}.json", StringComparison.OrdinalIgnoreCase));
            if (!string.IsNullOrEmpty(resName))
            {
                try
                {
                    using (var s = asm.GetManifestResourceStream(resName))
                    {
                        if (s != null)
                        {
                            using (var sr = new StreamReader(s))
                            {
                                var text = sr.ReadToEnd();
                                var node = JsonNode.Parse(text) as JsonObject;
                                if (node is not null) return node;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"[RegulationAdapter] AST embedded parse error for {cardId}: {ex.Message}");
                }
            }

            Console.Error.WriteLine($"[RegulationAdapter] AST not found for cardId={cardId}");
            return null;
        }

        private static IEnumerable<string> GetSearchBases()
        {
            var cur = new DirectoryInfo(Directory.GetCurrentDirectory());
            for (int i = 0; i < 6 && cur != null; i++, cur = cur.Parent)
                yield return cur.FullName;
        }

        private static void ApplyFlagsToCard(JsonObject ast, object card)
        {
            var flags = new List<string>();
            if (ast.TryGetPropertyValue("flags", out var flagsNode) && flagsNode is JsonArray arr)
                foreach (var n in arr) { var s = n?.ToString(); if (!string.IsNullOrWhiteSpace(s)) flags.Add(s!); }
            if (flags.Count == 0) return;

            foreach (var flag in flags.Distinct(StringComparer.OrdinalIgnoreCase))
            {
                switch (flag)
                {
                    case "ShieldTrigger":
                        TrySetBoolean(card, new[] { "IsShieldTrigger", "ShieldTrigger" }, true);
                        TryCall(card, new[] { "AddKeyword", "GrantAbility" }, "ShieldTrigger");
                        break;
                    case "Blocker":
                        TrySetBoolean(card, new[] { "IsBlocker", "Blocker" }, true);
                        TryCall(card, new[] { "AddKeyword", "GrantAbility" }, "Blocker");
                        break;
                    case "SpeedAttacker":
                        TrySetBoolean(card, new[] { "HasSpeedAttacker", "SpeedAttacker" }, true);
                        TryCall(card, new[] { "AddKeyword", "GrantAbility" }, "SpeedAttacker");
                        break;
                }
            }
        }

        private static void TryApplyReplacement(JsonObject? effect, object gameState, object simEvent, object sourceCard)
        {
            if (effect is null) return;
            if (!IsEventMatch(effect, simEvent, sourceCard)) return;
            if (!effect.TryGetPropertyValue("replacement", out var repNode) || repNode is not JsonArray reps) return;

            foreach (var r in reps)
            {
                var robj = r as JsonObject; if (robj is null) continue;
                var replaceWhat = robj["replace"]?.ToString();
                if (string.Equals(replaceWhat, "WouldBeDestroyed", StringComparison.OrdinalIgnoreCase))
                {
                    if (TryMarkEventHandled(simEvent))
                    {
                        if (robj.TryGetPropertyValue("by", out var byNode) && byNode is JsonArray steps)
                            foreach (var s in steps) ExecOp(s as JsonObject, gameState, simEvent, sourceCard);
                        Console.Error.WriteLine("[RegulationAdapter] [Replacement] WouldBeDestroyed -> applied");
                    }
                }
            }
        }

        private static void TryApplyTriggered(JsonObject? effect, object gameState, object simEvent, object sourceCard)
        {
            if (effect is null) return;
            if (!IsEventMatch(effect, simEvent, sourceCard)) return;
            if (!effect.TryGetPropertyValue("effect", out var seq) || seq is not JsonArray steps) return;
            foreach (var s in steps) ExecOp(s as JsonObject, gameState, simEvent, sourceCard);
            Console.Error.WriteLine("[RegulationAdapter] [Triggered] executed steps: " + steps.Count);
        }

        private static bool IsEventMatch(JsonObject effect, object simEvent, object sourceCard)
        {
            var cond = effect["condition"] as JsonObject;
            if (cond is null) return true;

            var when = cond["when"]?.ToString();
            if (!string.IsNullOrWhiteSpace(when))
                if (!IsSimEvent(simEvent, when)) return false;

            if (cond.TryGetPropertyValue("if", out var ifNode) && ifNode is JsonArray arr)
            {
                foreach (var n in arr)
                {
                    var i = n as JsonObject; if (i is null) continue;
                    var t = i["type"]?.ToString();
                    if (string.Equals(t, "SourceIs", StringComparison.OrdinalIgnoreCase))
                    {
                        var v = i["value"]?.ToString();
                        if (string.Equals(v, "this", StringComparison.OrdinalIgnoreCase) && !IsSource(simEvent, sourceCard))
                            return false;
                    }
                }
            }
            return true;
        }

        private static void ExecOp(JsonObject? op, object gameState, object simEvent, object sourceCard)
        {
            if (op is null) return;
            var kind = op["op"]?.ToString();

            switch (kind)
            {
                case "Tap":
                    var targetCard = ResolveSelectorCard(op["selector"] as JsonObject, gameState, simEvent, sourceCard) ?? sourceCard;
                    if (!TryCall(targetCard, new[] { "Tap" }) &&
                        !TrySetBoolean(targetCard, new[] { "IsTapped", "Tapped" }, true))
                        Console.Error.WriteLine("[RegulationAdapter] [Exec] Tap: no-op");
                    break;

                case "Draw":
                    int n = TryInt(op["n"]) ?? 1;
                    if (!TryCall(gameState, new[] { "Draw" }, n))
                    {
                        var who = op["who"]?.ToString() ?? "you";
                        if (!TryCall(gameState, new[] { "Draw" }, who, n))
                            Console.Error.WriteLine("[RegulationAdapter] [Exec] Draw: no-op");
                    }
                    break;

                case "PowerMod":
                    int delta = TryInt(op["delta"]) ?? 0;
                    var dur = op["duration"]?.ToString() ?? "UntilEndOfTurn";
                    var tgt = ResolveSelectorCard(op["selector"] as JsonObject, gameState, simEvent, sourceCard) ?? sourceCard;
                    if (!TryCall(gameState, new[] { "ApplyPowerMod", "AddPowerMod", "BuffUntilEOT" }, tgt!, delta, dur))
                    {
                        if (!TryGetSetPower(tgt!, delta))
                            Console.Error.WriteLine("[RegulationAdapter] [Exec] PowerMod: no-op");
                    }
                    break;

                default:
                    Console.Error.WriteLine("[RegulationAdapter] [Exec] unsupported op: " + kind);
                    break;
            }
        }

        private static bool IsSimEvent(object simEvent, string when)
        {
            var a = TryGetProp(simEvent, "Action") as string ?? TryGetProp(simEvent, "Kind") as string ?? TryGetProp(simEvent, "Type") as string;
            if (string.IsNullOrWhiteSpace(a)) return false;
            a = a.Trim();
            when = when.Trim();

            if (string.Equals(when, "OnAttack", StringComparison.OrdinalIgnoreCase))
                return a.Equals("Attack", StringComparison.OrdinalIgnoreCase);
            if (string.Equals(when, "OnDestroyed", StringComparison.OrdinalIgnoreCase))
                return a.Equals("Destroyed", StringComparison.OrdinalIgnoreCase) || a.Equals("Destroy", StringComparison.OrdinalIgnoreCase);
            if (string.Equals(when, "OnStartOfTurn", StringComparison.OrdinalIgnoreCase))
                return a.Equals("StartOfTurn", StringComparison.OrdinalIgnoreCase);
            if (string.Equals(when, "OnEndOfTurn", StringComparison.OrdinalIgnoreCase))
                return a.Equals("EndOfTurn", StringComparison.OrdinalIgnoreCase);
            if (string.Equals(when, "OnBreakShield", StringComparison.OrdinalIgnoreCase))
                return a.Equals("BreakShield", StringComparison.OrdinalIgnoreCase);
            if (string.Equals(when, "OnSpellCast", StringComparison.OrdinalIgnoreCase))
                return a.Equals("SpellCast", StringComparison.OrdinalIgnoreCase);

            return false;
        }

        private static bool IsSource(object simEvent, object card)
        {
            var sid = TryGetProp(simEvent, "SourceId") as string
                   ?? TryGetProp(simEvent, "CardId") as string
                   ?? TryGetProp(simEvent, "Source") as string;
            var cid = TryGetCardId(card);
            if (!string.IsNullOrEmpty(sid) && !string.IsNullOrEmpty(cid))
                return string.Equals(sid, cid, StringComparison.OrdinalIgnoreCase);

            var scard = TryGetProp(simEvent, "SourceCard") ?? TryGetProp(simEvent, "Card");
            if (scard is not null)
            {
                var sid2 = TryGetCardId(scard);
                if (!string.IsNullOrEmpty(sid2) && !string.IsNullOrEmpty(cid))
                    return string.Equals(sid2, cid, StringComparison.OrdinalIgnoreCase);
            }
            return false;
        }

        private static object? ResolveSelectorCard(JsonObject? selector, object gameState, object simEvent, object sourceCard)
        {
            if (selector is null) return null;
            var who = selector["who"]?.ToString() ?? "source";
            if (string.Equals(who, "source", StringComparison.OrdinalIgnoreCase)) return sourceCard;
            var tgt = TryGetProp(simEvent, "TargetCard") ?? TryGetProp(simEvent, "Target");
            if (tgt is not null) return tgt;
            return null;
        }

        private static bool TryMarkEventHandled(object simEvent)
        {
            if (TrySetBoolean(simEvent, new[] { "Handled", "IsHandled", "Cancelled" }, true)) return true;
            if (TryCall(simEvent, new[] { "Cancel", "Prevent", "Replace" })) return true;
            return false;
        }

        private static bool TryGetSetPower(object card, int delta)
        {
            var p = card.GetType().GetProperty("Power", BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
            if (p is null || !p.CanRead || !p.CanWrite) return false;
            if (p.PropertyType != typeof(int)) return false;
            try
            {
                var cur = (int)(p.GetValue(card) ?? 0);
                p.SetValue(card, cur + delta);
                return true;
            }
            catch { return false; }
        }

        private static int? TryInt(JsonNode? n)
        {
            if (n is null) return null;
            if (int.TryParse(n.ToString(), out var i)) return i;
            return null;
        }

        private static object? TryGetProp(object obj, string name)
        {
            if (obj is null) return null;
            var t = obj.GetType();
            var p = t.GetProperty(name, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
            return p?.GetValue(obj);
        }

        private static string? TryGetCardId(object card)
        {
            var names = new[] { "Id", "CardId", "cardId", "FaceId", "face_id", "id" };
            foreach (var n in names)
            {
                var v = TryGetProp(card, n);
                if (v is string s && !string.IsNullOrWhiteSpace(s)) return s;
                if (v is Guid g && g != Guid.Empty) return g.ToString("N");
                if (v is int i) return i.ToString();
            }
            var m = card.GetType().GetMethod("GetCardId", BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
            if (m is not null)
            {
                var v = m.Invoke(card, Array.Empty<object>());
                if (v is string s && !string.IsNullOrWhiteSpace(s)) return s;
            }
            return null;
        }

        private static bool TrySetBoolean(object obj, IEnumerable<string> propNames, bool value)
        {
            foreach (var name in propNames)
            {
                var p = obj.GetType().GetProperty(name, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                if (p is not null && p.CanWrite && (p.PropertyType == typeof(bool) || p.PropertyType == typeof(Boolean)))
                {
                    try { p.SetValue(obj, value); return true; } catch { }
                }
            }
            return false;
        }

        private static bool TryCall(object obj, IEnumerable<string> methodCandidates, params object[] args)
        {
            foreach (var mname in methodCandidates)
            {
                var ms = obj.GetType().GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.IgnoreCase)
                            .Where(x => string.Equals(x.Name, mname, StringComparison.OrdinalIgnoreCase));
                foreach (var m in ms)
                {
                    var pars = m.GetParameters();
                    if (pars.Length == args.Length && Compatible(pars, args))
                    {
                        try { m.Invoke(obj, args); return true; } catch { }
                    }
                }
            }
            return false;
        }

        private static bool Compatible(System.Reflection.ParameterInfo[] pars, object[] args)
        {
            for (int i = 0; i < pars.Length; i++)
            {
                if (args[i] is null) continue;
                if (!pars[i].ParameterType.IsAssignableFrom(args[i].GetType())) return false;
            }
            return true;
        }

        private static string SanitizeFileName(string s)
        {
            foreach (var c in Path.GetInvalidFileNameChars())
                s = s.Replace(c, '_');
            return s;
        }

        private static void Log(string msg, bool onlyVerbose)
        {
            if (onlyVerbose && !Verbose) return;
            Console.Error.WriteLine("[RegulationAdapter] " + msg);
        }
    }
}
