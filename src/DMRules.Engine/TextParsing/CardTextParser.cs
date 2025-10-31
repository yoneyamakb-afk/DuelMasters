// M15.1k - Card Text Parser (keeps M15.1h normalization & conservative splitter)
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace DMRules.Engine.TextParsing
{
    public sealed class ParseToken
    {
        public string Text { get; }
        public TemplateKey? Template { get; }
        public bool Ignored { get; }

        public ParseToken(string text, TemplateKey? template, bool ignored=false)
        {
            Text = text;
            Template = template;
            Ignored = ignored;
        }

        public override string ToString() => Template.HasValue ? $"{Template}:{Text}" : Text;
    }

    public sealed class ParseResult
    {
        public IReadOnlyList<ParseToken> Tokens { get; }
        public IReadOnlyList<string> UnresolvedPhrases { get; }
        public bool IsFullyResolved => UnresolvedPhrases.Count == 0;

        public ParseResult(List<ParseToken> tokens, List<string> unresolved)
        {
            Tokens = tokens;
            UnresolvedPhrases = unresolved;
        }
    }

    public static class CardTextParser
    {
        private static readonly Regex Splitter = new Regex(@"[\r\n\t]+", RegexOptions.Compiled);

        public static ParseResult Parse(string text)
        {
            text = text ?? string.Empty;

            // normalize soft separators (keep punctuation intact for templates)
            text = Regex.Replace(text, @"[\r\n\t,　]+", " ");

            // strip set markers first
            text = CardTextTemplates.SetMarkerStrip.Replace(text, " ");

            if (string.IsNullOrWhiteSpace(text))
                return new ParseResult(new List<ParseToken>(), new List<string>());

            var tokens = new List<ParseToken>();
            var unresolved = new List<string>();

            var segments = Splitter.Split(text);
            foreach (var sentence in segments.Where(s => !string.IsNullOrWhiteSpace(s)))
            {
                var matches = CardTextTemplates.MatchAll(sentence).ToList();
                if (matches.Count == 0)
                {
                    tokens.Add(new ParseToken(sentence, null));
                    unresolved.Add(sentence);
                }
                else
                {
                    // choose first match for now (deterministic)
                    var (key, match, ignore) = matches.First();
                    tokens.Add(new ParseToken(sentence, key, ignore));
                }
            }

            return new ParseResult(tokens, unresolved.Where(u => u.Trim().Length > 0).ToList());
        }
    }
}
