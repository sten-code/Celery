using System.Windows;

namespace Celery
{
    public partial class App : Application
    {
        public static MainWindow Instance { get; set; }

        private void App_Startup(object sender, StartupEventArgs e)
        {
            Instance = new MainWindow();
            Instance.Show();
        }
    }
}
