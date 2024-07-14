using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows;
using Celery.Core;
using Celery.Services;
using Celery.Utils;
using Celery.ViewModel;

namespace Celery
{
    public partial class MainWindow
    {
        private ExplorerViewModel ExplorerViewModel { get; }
        private ConsoleViewModel ConsoleViewModel { get; }
        private SettingsViewModel SettingsViewModel { get; }
        private ISettingsService SettingsService { get; }

        public MainWindow(ExplorerViewModel explorerViewModel, ConsoleViewModel consoleViewModel, SettingsViewModel settingsViewModel, ISettingsService settingsService)
        {
            InitializeComponent();
            ExplorerViewModel = explorerViewModel;
            ConsoleViewModel = consoleViewModel;
            SettingsViewModel = settingsViewModel;
            SettingsViewModel.CloseCommand = new RelayCommand(SettingsCloseCommand, _ => true);

            SettingsService = settingsService;
            SettingsService.OnSettingChanged += (_, e) =>
            {
                if (e.Setting.Id == "topmost")
                    Topmost = (bool)e.Setting.GetValue();
            };
            Topmost = settingsService.GetSetting<bool>("topmost");

            ExplorerViewModel.CloseCommand = new RelayCommand(_ =>
            {
                ExplorerColumn.Width = new GridLength(0);
            }, _ => true);

            ConsoleViewModel.CloseCommand = new RelayCommand(_ =>
            {
                ConsoleRow.Height = new GridLength(0);
            }, _ => true);
        }

        private void SettingsButtonClick(object sender, RoutedEventArgs e)
        {
            if (SettingsService.GetSetting<bool>("background_blur"))
                BlurEffect.Radius = 10;
            HostGrid.IsEnabled = false;
            SettingsHost.Visibility = Visibility.Visible;
            AnimationUtils.AnimateMargin(SettingsHost, new Thickness(0, -ActualHeight * 2, 0, 0), new Thickness(0), AnimationUtils.EaseOut);
        }

        private async void SettingsCloseCommand(object obj)
        {
            BlurEffect.Radius = 0;
            HostGrid.IsEnabled = true;
            AnimationUtils.AnimateMargin(SettingsHost, new Thickness(0), new Thickness(0, -ActualHeight * 2, 0, 0), AnimationUtils.EaseOut);
            await Task.Delay(500);
            SettingsHost.Visibility = Visibility.Hidden;
        }

        private void ViewConsoleMenuItemClick(object sender, RoutedEventArgs e)
        {
            ConsoleRow.Height = new GridLength(120);
        }

        private void ViewExplorerMenuItemClick(object sender, RoutedEventArgs e)
        {
            ExplorerColumn.Width = new GridLength(150);
        }

        private void MainWindow_OnClosing(object sender, CancelEventArgs e)
        {
            // App.Exit() saves the tabs and exits
            e.Cancel = true;
            App.Exit();
        }
    }
}