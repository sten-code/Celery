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
        public static Grid BlurGrid { get; set; }
        public static Grid BaseGrid { get; set; }
        public static Tabs Tabs { get; set; }

        public static async Task<MessageBoxResult> ShowMessage(string title, string content, bool closeButton, MessageBoxButtons buttons)
        {
            BlurGrid.Effect = new BlurEffect
            {
                Radius = 10,
                KernelType = KernelType.Gaussian
            };
            WebView2 toDisable = (WebView2)Tabs.SelectedContent;

            if (toDisable != null)
                toDisable.Visibility = System.Windows.Visibility.Hidden;
            MessageBox box = new MessageBox(title, content, closeButton);

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
                BlurGrid.Effect = null;
                BaseGrid.Children.Remove(box);
                BaseGrid.Children.Remove(border);
                if (toDisable != null)
                    toDisable.Visibility = System.Windows.Visibility.Visible;
            };
            BaseGrid.Children.Add(border);
            BaseGrid.Children.Add(box);
            while (result == MessageBoxResult.None) 
                await Task.Delay(10);
            return result;
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
