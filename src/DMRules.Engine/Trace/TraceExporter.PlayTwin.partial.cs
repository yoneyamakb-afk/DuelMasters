namespace DMRules.Engine.Trace
{
    /// <summary>
    /// 既存 TraceExporter に部分クラスで "PlayTwin" 出力を追加。
    /// 既に TraceExporter が存在する前提。未存在でもこのファイル単体でコンパイル可能なように
    /// 簡易同名クラスを条件付きで定義することもできますが、意図せず二重定義になるのを避けるため
    /// ここでは partial のみを提供します。
    /// </summary>
    public static partial class TraceExporter
    {
        // 既存実装側の Emit(string,int,string) が呼ばれる前提。
        // 無い場合は以下のようなシグネチャに合わせて別途パーシャルで補完してください。
        // public static void Emit(string type, int faceId, string detail) { /* ... */ }
    }
}
