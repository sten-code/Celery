using Celery.Controls;
using Celery.GitHub;
using Celery.Utils;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Security.Authentication;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace Celery
{
    public partial class App : Application
    {
        public static MainWindow Instance { get; set; }
        public static HttpClient HttpClient { get; private set; }
        public static List<AnnouncementBox> Anouncements { get; private set; }

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

            Anouncements = new List<AnnouncementBox>();
            new Thread(async () =>
            {
                try
                {
                    string response = await HttpClient.GetStringAsync("https://api.github.com/repos/sten-code/Celery/releases");
                    List<Release> releases = response.FromJson<List<Release>>();
                    foreach (Release release in releases)
                    {
                        Dispatcher.Invoke(() =>
                        {
                            Anouncements.Add(new AnnouncementBox(release.body, AnnouncementType.Update));
                        });
                    }
                } catch (HttpRequestException)
                {

                }
            }).Start();
        }
    }
}
