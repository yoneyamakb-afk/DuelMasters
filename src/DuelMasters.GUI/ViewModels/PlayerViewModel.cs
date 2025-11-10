using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace DuelMasters.GUI.ViewModels
{
    public sealed class PlayerViewModel : INotifyPropertyChanged
    {
        private int _handCount;
        private int _manaCount;
        private int _shieldCount;

        public ObservableCollection<string> BattleZoneCards { get; } = new ObservableCollection<string>();

        public int HandCount
        {
            get => _handCount;
            set
            {
                if (_handCount != value)
                {
                    _handCount = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(HandLabel));
                }
            }
        }

        public int ManaCount
        {
            get => _manaCount;
            set
            {
                if (_manaCount != value)
                {
                    _manaCount = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(ManaLabel));
                }
            }
        }

        public int ShieldCount
        {
            get => _shieldCount;
            set
            {
                if (_shieldCount != value)
                {
                    _shieldCount = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(ShieldLabel));
                }
            }
        }

        public string HandLabel => $"手札: {HandCount}枚";
        public string ManaLabel => $"マナ: {ManaCount}枚";
        public string ShieldLabel => $"シールド: {ShieldCount}枚";

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
