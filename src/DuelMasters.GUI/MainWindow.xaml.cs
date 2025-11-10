using System.Threading.Tasks;
using System.Windows;
using DuelMasters.GUI.ViewModels;
using DuelMasters.GUI.Game;

namespace DuelMasters.GUI
{
    public partial class MainWindow : Window
    {
        private readonly MainViewModel _viewModel;

        public MainWindow()
        {
            InitializeComponent();

            var controller = new GameController();
            _viewModel = new MainViewModel(controller);
            DataContext = _viewModel;

            Loaded += async (_, __) => await InitializeAsync();
        }

        private async Task InitializeAsync()
        {
            await _viewModel.InitializeAsync();
        }

        private void OnExitClicked(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
