using System;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using Celery.Core;
using Celery.Services;

namespace Celery.ViewModel;

public class ConsoleViewModel : Core.ViewModel
{
    public ICommand CloseCommand { get; set; }
    public ICommand ClearCommand { get; set; }
    public RichTextBox OutputBox { get; }

    public ConsoleViewModel()
    {
        OutputBox = new RichTextBox();
        ClearCommand = new RelayCommand(_ =>
        {
            OutputBox.Document.Blocks.Clear();
        }, _ => true);
    }

    public void AddMessage(string message, ErrorLevel errorLevel)
    {
        if (message == null)
            message = "";

        TextRange tr = new(OutputBox.Document.ContentEnd, OutputBox.Document.ContentEnd)
        {
            Text = $"[{DateTime.Now:HH:mm:ss}] {message.Trim()}\n"
        };

        switch (errorLevel)
        {
            case ErrorLevel.Info:
                tr.ApplyPropertyValue(TextElement.ForegroundProperty, Brushes.AliceBlue);
                break;
            case ErrorLevel.Warning:
                tr.ApplyPropertyValue(TextElement.ForegroundProperty, Brushes.Gold);
                break;
            case ErrorLevel.Error:
                tr.ApplyPropertyValue(TextElement.ForegroundProperty, Brushes.IndianRed);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(errorLevel), errorLevel, null);
        }

        OutputBox.ScrollToEnd();
    }
}