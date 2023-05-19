using System.Threading.Tasks;
using System.Windows;

namespace Celery
{
    public partial class App : Application
    {
        public static MainWindow Instance { get; set; }

        private async void App_Startup(object sender, StartupEventArgs e)
        {
            Instance = new MainWindow();
            if (Instance.StartupAnimation)
            {
                Startup startup = new Startup();
                startup.Show();
                await Task.Delay(3500);
            }
            Instance.Show();
        }
    }
}
