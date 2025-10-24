
namespace DMRules.Effects.Text
{
    /// <summary>
    /// 効果テキストの取得契約。カードIDやカード名をキーに、効果テキスト（そのまま Parser に渡す文字列）を返す。
    /// 実装は DB、メモリ辞書、外部サービスなど何でも可。
    /// </summary>
    public interface IEffectTextProvider
    {
        /// <returns>null なら効果なし扱い（No-Op）</returns>
        string? GetEffectTextByFaceId(int faceId);

        /// <summary>必要に応じてカード名から取得したいときに使用。</summary>
        string? GetEffectTextByName(string cardName);
    }
}
