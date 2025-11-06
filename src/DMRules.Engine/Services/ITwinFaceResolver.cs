namespace DMRules.Engine.Services
{
    /// <summary>
    /// twin_id と side(A/B) から確定 FaceId を求める解決器。
    /// 既存のDB/Repositoryへの結線は実装側に委ねるため、このインターフェースのみ配布します。
    /// </summary>
    public interface ITwinFaceResolver
    {
        /// <summary>
        /// 例：sourceFaceId= A面FaceId、sideToPlay=1(B) なら B面の FaceId を返す。
        /// 実装は twin_id を辿る等の方法で解決する。
        /// </summary>
        int ResolveFaceId(int sourceFaceId, int sideToPlay);
    }
}
