using Celery.Controls;
using Microsoft.Web.WebView2.Wpf;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Effects;

namespace Celery.Utils
{
    public static class MessageBoxUtils
    {
        public static async Task<MessageBoxResult> ShowMessage(string title, string content, bool closeButton, MessageBoxButtons buttons, int width = 300, int height = 189)
        {
            while (App.Instance == null) await Task.Delay(10);
            App.Instance.BlurGrid.Effect = new BlurEffect
            {
                Radius = 10,
                KernelType = KernelType.Gaussian
            };
            WebView2 toDisable = (WebView2)App.Instance.Tabs.SelectedContent;
            if (toDisable != null)
                toDisable.Visibility = System.Windows.Visibility.Hidden;
            MessageBox box = new MessageBox(title, content, closeButton, false)
            {
                Width = width,
                Height = height,
            };

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
                case MessageBoxButtons.None:
                    box.ButtonsBox.Visibility = System.Windows.Visibility.Hidden;
                    break;
            }
            Border border = new Border
            {
                Background = new SolidColorBrush(Colors.Transparent)
            };

            MessageBoxResult result = MessageBoxResult.None;
            box.MessageBoxClosing += async (s, e) =>
            {
                App.Instance.BlurGrid.Effect = null;
                AnimationUtils.AnimateMargin(box, new System.Windows.Thickness(), new System.Windows.Thickness(0, -height * 1.5 - App.Instance.ActualHeight, 0, 0), AnimationUtils.EaseOut, 500);
                await Task.Delay(250);
                App.Instance.BaseGrid.Children.Remove(border);
                if (toDisable != null)
                    toDisable.Visibility = System.Windows.Visibility.Visible;
                await Task.Delay(250);
                App.Instance.BaseGrid.Children.Remove(box);
                result = e.Result;
            };
            AnimationUtils.AnimateMargin(box, new System.Windows.Thickness(0, -height * 1.5 - App.Instance.ActualHeight, 0, 0), new System.Windows.Thickness(), AnimationUtils.EaseOut, 500);
            App.Instance.BaseGrid.Children.Add(border);
            App.Instance.BaseGrid.Children.Add(box);

            while (result == MessageBoxResult.None) 
                await Task.Delay(10);
            return result;
        }

        public static async Task<(string, MessageBoxResult)> ShowInputBox(string title, string content, bool closeButton, string defaultInput = "", int width = 300, int height = 189)
        {
            App.Instance.BlurGrid.Effect = new BlurEffect
            {
                Radius = 10,
                KernelType = KernelType.Gaussian
            };
            WebView2 toDisable = (WebView2)App.Instance.Tabs.SelectedContent;
            if (toDisable != null)
                toDisable.Visibility = System.Windows.Visibility.Hidden;

            MessageBox box = new MessageBox(title, content, closeButton, true)
            {
                Width = width,
                Height = height,
            };
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
            box.MessageBoxClosing += async (s, e) =>
            {
                input = box.InputBox.Text;
                App.Instance.BlurGrid.Effect = null;
                AnimationUtils.AnimateMargin(box, new System.Windows.Thickness(), new System.Windows.Thickness(0, -height * 1.5 - App.Instance.ActualHeight, 0, 0), AnimationUtils.EaseOut, 500);
                await Task.Delay(250);
                App.Instance.BaseGrid.Children.Remove(border);
                if (toDisable != null)
                    toDisable.Visibility = System.Windows.Visibility.Visible;
                await Task.Delay(250);
                App.Instance.BaseGrid.Children.Remove(box);
                result = e.Result;
            };

            AnimationUtils.AnimateMargin(box, new System.Windows.Thickness(0, -height * 1.5 - App.Instance.ActualHeight, 0, 0), new System.Windows.Thickness(), AnimationUtils.EaseOut, 500);
            App.Instance.BaseGrid.Children.Add(border);
            App.Instance.BaseGrid.Children.Add(box);

            while (result == MessageBoxResult.None)
                await Task.Delay(10);
            return (input, result);
        }

        public static async Task<MessageBoxResult> ShowMessage(string title, System.Windows.UIElement content, bool closeButton, MessageBoxButtons buttons, int width = 300, int height = 189)
        {
            App.Instance.BlurGrid.Effect = new BlurEffect
            {
                Radius = 10,
                KernelType = KernelType.Gaussian
            };
            WebView2 toDisable = (WebView2)App.Instance.Tabs.SelectedContent;
            if (toDisable != null)
                toDisable.Visibility = System.Windows.Visibility.Hidden;
            MessageBox box = new MessageBox(title, content, closeButton, false)
            {
                Width = width,
                Height = height,
                VerticalAlignment = System.Windows.VerticalAlignment.Center
            };

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
                case MessageBoxButtons.None:
                    box.MainGrid.Children.Remove(box.ButtonsBox);
                    break;
            }
            Border border = new Border
            {
                Background = new SolidColorBrush(Colors.Transparent)
            };

            MessageBoxResult result = MessageBoxResult.None;
            box.MessageBoxClosing += async (s, e) =>
            {
                App.Instance.BlurGrid.Effect = null;
                AnimationUtils.AnimateMargin(box, new System.Windows.Thickness(), new System.Windows.Thickness(0, -height * 1.5 - App.Instance.ActualHeight, 0, 0), AnimationUtils.EaseOut, 500);
                await Task.Delay(250);
                App.Instance.BaseGrid.Children.Remove(border);
                if (toDisable != null)
                    toDisable.Visibility = System.Windows.Visibility.Visible;
                await Task.Delay(250);
                App.Instance.BaseGrid.Children.Remove(box);
                result = e.Result;
            };
            App.Instance.BaseGrid.Children.Add(border);
            App.Instance.BaseGrid.Children.Add(box);
            AnimationUtils.AnimateMargin(box, new System.Windows.Thickness(0, -height * 1.5 - App.Instance.ActualHeight, 0, 0), new System.Windows.Thickness(), AnimationUtils.EaseOut, 500);

            while (result == MessageBoxResult.None)
                await Task.Delay(10);
            return result;
        }
    }

    public enum MessageBoxButtons
    {
        None,
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
