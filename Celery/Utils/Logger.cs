using System;
using System.Windows.Documents;
using System.Windows.Media;

namespace Celery.Utils
{
    public enum LoggingType
    {
        Output,
        Info,
        Warning, 
        Error
    }

    public class Logger
    {
        public static void Log(string message, bool debugging = false, LoggingType type = LoggingType.Output)
        {
            if (debugging && !App.Instance.DebuggingMode)
                return;

            TextRange tr = new TextRange(App.Instance.OutputBox.Document.ContentEnd, App.Instance.OutputBox.Document.ContentEnd);
            tr.Text = $"[{DateTime.Now:HH:mm:ss}] {message.Trim()}\n";

            switch (type)
            {
                case LoggingType.Info:
                    tr.ApplyPropertyValue(TextElement.ForegroundProperty, Brushes.AliceBlue);
                    break;
                case LoggingType.Warning:
                    tr.ApplyPropertyValue(TextElement.ForegroundProperty, Brushes.Gold);
                    break;
                case LoggingType.Error:
                    tr.ApplyPropertyValue(TextElement.ForegroundProperty, Brushes.IndianRed);
                    break;
                default:
                    break;
            }

            //App.Instance.OutputBox.AppendText($"[{DateTime.Now:HH:mm:ss}] {message.Trim()}\n");
            App.Instance.OutputBox.ScrollToEnd();
        }
    }
}
