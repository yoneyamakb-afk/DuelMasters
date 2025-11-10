using System.Threading.Tasks;

namespace DuelMasters.GUI.Game
{
    /// <summary>
    /// GUI からゲームエンジンにアクセスするための抽象インターフェース。
    ///
    /// - 現在は GameController がスタブ実装としてこれを実装しています。
    /// - 将来、DuelMasters.Engine / GameState への本物の接続を行う場合は、
    ///   このインターフェースを実装した別クラス（例えば EngineBackedGameController）を
    ///   用意し、MainWindow 側で差し替えるだけで済みます。
    /// </summary>
    public interface IGameController
    {
        Task<(GameSnapshot snapshot, string message)> InitializeAsync();
        Task<(GameSnapshot snapshot, string message)> StepAsync();
    }
}
