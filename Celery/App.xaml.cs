using System.Net.Http;
using System.Security.Authentication;
using System.Threading.Tasks;
using System.Windows;

namespace Celery
{
    public partial class App : Application
    {
        public static MainWindow Instance { get; set; }
        public static HttpClient HttpClient { get; private set; }

        private async void App_Startup(object sender, StartupEventArgs e)
        {
            HttpClient = new HttpClient(new HttpClientHandler()
            {
                SslProtocols = SslProtocols.Tls12,
                UseCookies = false,
                UseProxy = false
            });
            HttpClient.DefaultRequestHeaders.Add("User-Agent", "Celery");

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
