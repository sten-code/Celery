using Microsoft.Web.WebView2.Wpf;
using System.Threading.Tasks;
using System;
using System.IO;
using Microsoft.Web.WebView2.Core;
using System.Windows;
using System.Windows.Media;

namespace Celery.Controls
{
    public abstract class Editor : WebView2
    {
        public bool IsDoMLoaded { get; set; } = false;
        public event EventHandler EditorReady;

        protected string LatestRecievedText;
        protected bool NewMessage = false;

        public Editor(string name, string text)
        {
            WebViewInitialize(null, Path.GetTempPath(), null);
            Color bg = (Color)Application.Current.Resources["BackgroundColor"];
            DefaultBackgroundColor = System.Drawing.Color.FromArgb(bg.R, bg.G, bg.B);

            Source = new Uri(Path.Combine(Config.BinPath, name, $"{name}.html"));
            CoreWebView2InitializationCompleted += (s, e) =>
            {
                CoreWebView2.DOMContentLoaded += (o, a) =>
                {
                    IsDoMLoaded = true;
                    if (EditorReady != null)
                        EditorReady(this, new EventArgs());

                    SetText(text);
                };
                CoreWebView2.WebMessageReceived += (o, a) =>
                {
                    LatestRecievedText = a.TryGetWebMessageAsString();
                    NewMessage = true;
                };
                CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;
                CoreWebView2.Settings.AreDevToolsEnabled = false;
            };
        }

        public async void WebViewInitialize(string browserExecutableFolder, string userDataFolder, CoreWebView2EnvironmentOptions options)
        {
            await EnsureCoreWebView2Async(await CoreWebView2Environment.CreateAsync(browserExecutableFolder, userDataFolder, options));
        }

        public abstract void SetText(string text);

        public abstract Task<string> GetText();

    }
}
