using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using DuelMasters.GUI.Game;

namespace DuelMasters.GUI.ViewModels
{
    /// <summary>
    /// メイン画面用ViewModel。
    /// 現時点ではGameControllerのスタブ実装と接続しています。
    /// 実エンジン(GameState)に接続する場合はGameController側を差し替えるだけで済む構造にしています。
    /// </summary>
    public sealed class MainViewModel : INotifyPropertyChanged
    {
        private readonly GameController _controller;

        private int _turn;
        private string _phase = "";
        private string _activePlayer = "";
        private string _logText = "";

        public PlayerViewModel PlayerA { get; } = new PlayerViewModel();
        public PlayerViewModel PlayerB { get; } = new PlayerViewModel();

        public ICommand NextStepCommand { get; }

        public MainViewModel(GameController controller)
        {
            _controller = controller;
            NextStepCommand = new RelayCommand(async _ => await NextStepAsync(), _ => true);
        }

        public async Task InitializeAsync()
        {
            var (snapshot, message) = await _controller.InitializeAsync();
            ApplySnapshot(snapshot);
            AppendLogLine(message);
        }

        private async Task NextStepAsync()
        {
            var (snapshot, message) = await _controller.StepAsync();
            ApplySnapshot(snapshot);
            AppendLogLine(message);
        }

        private void ApplySnapshot(GameSnapshot snapshot)
        {
            Turn = snapshot.Turn;
            Phase = snapshot.Phase;
            ActivePlayer = snapshot.ActivePlayer;

            PlayerA.HandCount = snapshot.PlayerA.HandCount;
            PlayerA.ManaCount = snapshot.PlayerA.ManaCount;
            PlayerA.ShieldCount = snapshot.PlayerA.ShieldCount;

            PlayerB.HandCount = snapshot.PlayerB.HandCount;
            PlayerB.ManaCount = snapshot.PlayerB.ManaCount;
            PlayerB.ShieldCount = snapshot.PlayerB.ShieldCount;

            PlayerA.BattleZoneCards.Clear();
            foreach (var name in snapshot.PlayerA.BattleZoneCards)
            {
                PlayerA.BattleZoneCards.Add(name);
            }

            PlayerB.BattleZoneCards.Clear();
            foreach (var name in snapshot.PlayerB.BattleZoneCards)
            {
                PlayerB.BattleZoneCards.Add(name);
            }
        }

        private void AppendLogLine(string message)
        {
            if (string.IsNullOrWhiteSpace(message)) return;

            if (string.IsNullOrEmpty(_logText))
            {
                LogText = message;
            }
            else
            {
                var sb = new StringBuilder(_logText.Length + message.Length + 2);
                sb.AppendLine(_logText);
                sb.Append(message);
                LogText = sb.ToString();
            }
        }

        public int Turn
        {
            get => _turn;
            private set
            {
                if (_turn != value)
                {
                    _turn = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(TurnDisplay));
                }
            }
        }

        public string Phase
        {
            get => _phase;
            private set
            {
                if (_phase != value)
                {
                    _phase = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(PhaseDisplay));
                }
            }
        }

        public string ActivePlayer
        {
            get => _activePlayer;
            private set
            {
                if (_activePlayer != value)
                {
                    _activePlayer = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(ActivePlayerDisplay));
                }
            }
        }

        public string TurnDisplay => $"ターン: {Turn}";
        public string PhaseDisplay => string.IsNullOrWhiteSpace(Phase) ? "" : $"フェイズ: {Phase}";
        public string ActivePlayerDisplay => string.IsNullOrWhiteSpace(ActivePlayer) ? "" : $"手番: {ActivePlayer}";

        public string LogText
        {
            get => _logText;
            private set
            {
                if (_logText != value)
                {
                    _logText = value;
                    OnPropertyChanged();
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
