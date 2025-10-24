
using System.Collections.Concurrent;

namespace DMRules.Effects.Text
{
    /// <summary>
    /// デバッグ/暫定用のメモリ辞書プロバイダ。
    /// DB移行までの穴埋めとして利用可能。
    /// </summary>
    public sealed class DictionaryEffectTextProvider : IEffectTextProvider
    {
        private readonly ConcurrentDictionary<int, string> _byFaceId = new();
        private readonly ConcurrentDictionary<string, string> _byName = new();

        public DictionaryEffectTextProvider AddByFaceId(int faceId, string effectText)
        {
            _byFaceId[faceId] = effectText;
            return this;
        }

        public DictionaryEffectTextProvider AddByName(string cardName, string effectText)
        {
            _byName[cardName] = effectText;
            return this;
        }

        public string? GetEffectTextByFaceId(int faceId)
            => _byFaceId.TryGetValue(faceId, out var t) ? t : null;

        public string? GetEffectTextByName(string cardName)
            => _byName.TryGetValue(cardName, out var t) ? t : null;
    }
}
