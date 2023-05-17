using Microsoft.Web.WebView2.Wpf;
using System.Threading.Tasks;
using System.Windows;
using System;
using System.IO;
using System.Windows.Media;

namespace Celery.Controls
{
    public abstract class Editor : WebView2
    {
        public static readonly DependencyProperty TextProperty = DependencyProperty.Register(nameof(Text), typeof(string), typeof(Editor), new PropertyMetadata(default(string)));

        public string Text
        {
            set
            {
                SetText(value);
            }
        }

        public abstract void SetText(string text);

        public abstract Task<string> GetText();

        public new bool IsLoaded = false;

        public Editor(string name)
        {
            Color bg = (Color)Application.Current.MainWindow.FindResource("BackgroundColor");
            DefaultBackgroundColor = System.Drawing.Color.FromArgb(bg.R, bg.G, bg.B);
            Source = new Uri(Path.Combine(Config.BinPath, name, $"{name}.html"));
            CoreWebView2InitializationCompleted += (s, e) =>
            {
                CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;
                CoreWebView2.Settings.AreDevToolsEnabled = false;
                CoreWebView2.NewWindowRequested += (sender, args) =>
                {
                    args.Handled = true;
                };
            };
            NavigationCompleted += (s, e) =>
            {
                IsLoaded = true;
            };
            NavigationStarting += (s, e) =>
            {
                IsLoaded = false;
            };
            AllowDrop = false;
        }

    }
}
