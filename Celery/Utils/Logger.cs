using System;

namespace Celery.Utils
{
    public class Logger
    {
        public static void Log(string message, bool debugging = false)
        {
            if (debugging && !App.Instance.DebuggingMode)
                return;

            App.Instance.Console.Text += $"[{DateTime.Now:HH:mm:ss}] {message.Trim()}\n";
            App.Instance.Console.ScrollToEnd();
        }
    }
}
