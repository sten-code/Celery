using System;
using System.Threading.Tasks;
using CefSharp.Wpf;
using Celery.Services;
using Celery.Settings;
using Microsoft.Extensions.DependencyInjection;

namespace Celery.Controls;

public abstract class Editor : ChromiumWebBrowser
{
    public event EventHandler<TextChangedEventArgs> TextChangedEvent;
    
    public abstract Task<string> GetText();

    public abstract Task SetText(string text);

    protected void TextChanged(string text, bool isInitialization)
    {
        TextChangedEvent?.Invoke(this, new TextChangedEventArgs
        {
            Text = text,
            IsInitialization = isInitialization
        });
    }

    public static Editor GetEditor(string text)
    {
        string editor = App.ServiceProvider.GetRequiredService<ISettingsService>().GetSetting<string>("editor");
        return editor switch
        {
            "Monaco" => new MonacoEditor(text),
            "Ace" => new AceEditor(text),
            _ => null
        };
    }
}

public class TextChangedEventArgs : EventArgs
{
    public string Text { get; set; }
    public bool IsInitialization { get; set; }
}