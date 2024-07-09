using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using CefSharp;
using Celery.Utils;

namespace Celery.Controls;

public class MonacoEditor : Editor
{
    public bool IsTextLoaded { get; set; }

    public MonacoEditor(string text)
    {
        AllowDrop = true;
        IsTextLoaded = false;
        PreviewDragOver += (_, e) =>
        {
            e.Effects = DragDropEffects.Move;
            e.Handled = true;
        };

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
            await SetText(text);
        };

        JavascriptMessageReceived += (_, e) =>
        {
            TextChanged(e.Message.ToString(), !IsTextLoaded);
            IsTextLoaded = true;
        };

        Load($"http://localhost:{App.MonacoPort}");
    }

    public async override Task SetText(string text)
    {
        if (!CanExecuteJavascriptInMainFrame)
        {
            Console.WriteLine("Unable to set text");
            return;
        }

        await this.EvaluateScriptAsync("window.editor.setValue(\"" + HttpUtils.JavaScriptStringEncode(text) + "\")");
    }

    public async override Task<string> GetText()
    {
        if (!CanExecuteJavascriptInMainFrame)
        {
            Console.WriteLine("Unable to get text");
            return "";
        }

        JavascriptResponse response = await this.EvaluateScriptAsync("window.editor.getValue()");
        return response.Result.ToString();
    }
}
