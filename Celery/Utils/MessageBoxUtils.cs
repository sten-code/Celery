using Celery.Controls;
using Microsoft.Web.WebView2.Wpf;
using System;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Effects;

namespace Celery.Utils
{
    public static class MessageBoxUtils
    {
        public static async Task<MessageBoxResult> ShowMessage(string title, string content, bool closeButton, MessageBoxButtons buttons)
        {
            App.Instance.BlurGrid.Effect = new BlurEffect
            {
                Radius = 10,
                KernelType = KernelType.Gaussian
            };
            WebView2 toDisable = (WebView2)App.Instance.Tabs.SelectedContent;
            if (toDisable != null)
                toDisable.Visibility = System.Windows.Visibility.Hidden;
            MessageBox box = new MessageBox(title, content, closeButton, false);

            switch (buttons)
            {
                case MessageBoxButtons.YesNo:
                    box.AddButton("Yes", MessageBoxResult.Yes);
                    box.AddButton("No", MessageBoxResult.No);
                    break;
                case MessageBoxButtons.YesNoCancel:
                    box.AddButton("Yes", MessageBoxResult.Yes);
                    box.AddButton("No", MessageBoxResult.No);
                    box.AddButton("Cancel", MessageBoxResult.Cancel);
                    break;
                case MessageBoxButtons.Ok:
                    box.AddButton("Ok", MessageBoxResult.Ok);
                    break;
                case MessageBoxButtons.OkCancel:
                    box.AddButton("Ok", MessageBoxResult.Ok);
                    box.AddButton("Cancel", MessageBoxResult.Cancel);
                    break;
            }
            Border border = new Border
            {
                Background = new SolidColorBrush(Colors.Transparent)
            };

            MessageBoxResult result = MessageBoxResult.None;
            box.MessageBoxClosing += (s, e) =>
            {
                result = e.Result;
                App.Instance.BlurGrid.Effect = null;
                App.Instance.BaseGrid.Children.Remove(box);
                App.Instance.BaseGrid.Children.Remove(border);
                if (toDisable != null)
                    toDisable.Visibility = System.Windows.Visibility.Visible;
            };
            App.Instance.BaseGrid.Children.Add(border);
            App.Instance.BaseGrid.Children.Add(box);
            while (result == MessageBoxResult.None) 
                await Task.Delay(10);
            return result;
        }

        public static async Task<(string, MessageBoxResult)> ShowInputBox(string title, string content, bool closeButton, string defaultInput = "")
        {
            App.Instance.BlurGrid.Effect = new BlurEffect
            {
                Radius = 10,
                KernelType = KernelType.Gaussian
            };
            WebView2 toDisable = (WebView2)App.Instance.Tabs.SelectedContent;
            if (toDisable != null)
                toDisable.Visibility = System.Windows.Visibility.Hidden;

            MessageBox box = new MessageBox(title, content, closeButton, true);
            box.InputBox.Text = defaultInput;
            box.AddButton("Ok", MessageBoxResult.Ok);
            if (closeButton)
            {
                box.AddButton("Cancel", MessageBoxResult.Cancel);
            }
            Border border = new Border
            {
                Background = new SolidColorBrush(Colors.Transparent)
            };
            MessageBoxResult result = MessageBoxResult.None;
            string input = "";
            box.MessageBoxClosing += (s, e) =>
            {
                result = e.Result;
                input = box.InputBox.Text;
                App.Instance.BlurGrid.Effect = null;
                App.Instance.BaseGrid.Children.Remove(box);
                App.Instance.BaseGrid.Children.Remove(border);
                if (toDisable != null)
                    toDisable.Visibility = System.Windows.Visibility.Visible;
            };

            App.Instance.BaseGrid.Children.Add(border);
            App.Instance.BaseGrid.Children.Add(box);
            while (result == MessageBoxResult.None)
                await Task.Delay(10);
            return (input, result);
        }
    }

    public enum MessageBoxButtons
    {
        YesNo,
        YesNoCancel,
        Ok,
        OkCancel
    }

    public enum MessageBoxResult
    {
        None,
        Yes,
        No,
        Ok,
        Cancel,
        Close
    }
}
