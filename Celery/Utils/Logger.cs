using System;

namespace Celery.Utils
{
    public class Logger
    {
        public static void Log(string message)
        {
            App.Instance.Console.Text += $"[{DateTime.Now:HH:mm:ss}] {message}\n";
            App.Instance.Console.ScrollToEnd();
        }
    }
}
