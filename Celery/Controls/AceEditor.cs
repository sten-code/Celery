using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using CefSharp;
using Celery.Utils;

namespace Celery.Controls;

public class AceEditor : Editor
{
    public bool IsTextLoaded { get; set; }
    
    public AceEditor(string text)
    {
        AllowDrop = true;
        PreviewDragOver += (_, e) =>
        {
            e.Effects = DragDropEffects.Move;
            e.Handled = true;
        };

        Load(new Uri(Path.Combine(Config.BinPath, "Ace", "ace.html")).AbsoluteUri);

        PreviewMouseWheel += (_, e) =>
        {
            if ((Keyboard.Modifiers & ModifierKeys.Control) != ModifierKeys.Control)
                return;

            if (e.Delta > 0)
                ZoomLevel += 0.1;
            else if (e.Delta < 0)
                ZoomLevel -= 0.1;
        };

        FrameLoadEnd += async (_, _) =>
        {
            Dispatcher.Invoke(() =>
            {
                ZoomLevel = 1.5;
            });
            await SetText(text);
        };

        JavascriptMessageReceived += (_, e) =>
        {
            TextChanged(e.Message.ToString(), !IsTextLoaded);
            IsTextLoaded = true;
        };
    }

    public async override Task SetText(string text)
    {
        if (!CanExecuteJavascriptInMainFrame)
        {
            Console.WriteLine("Unable to set text");
            return;
        }

        await this.EvaluateScriptAsync("editor.setValue(\"" + HttpUtils.JavaScriptStringEncode(text) + "\")");
    }

    public async override Task<string> GetText()
    {
        if (!CanExecuteJavascriptInMainFrame)
        {
            Console.WriteLine("Unable to get text");
            return "";
        }

        JavascriptResponse response = await this.EvaluateScriptAsync("editor.getValue()");
        return response.Result.ToString();
    }

}