using Celery.Utils;
using System;
using System.Windows.Controls;
using System.Windows.Markup;

namespace Celery.Controls
{
    public partial class MessageBox : UserControl
    {
        public event EventHandler<MessageBoxClosingEventArgs> MessageBoxClosing;

        public MessageBox(string title, string content, bool closeButton)
        {
            InitializeComponent();
            TitleBox.Text = title;
            ContentBox.Text = content;
            ButtonsBox.Children.Clear();
            if (!closeButton)
                CloseButton.Visibility = System.Windows.Visibility.Hidden;
        }

        public void AddButton(string content, MessageBoxResult result)
        {
            Button b = (Button)XamlReader.Parse(XamlWriter.Save(DefaultButton));
            b.Content = content;
            b.Click += (s, e) =>
            {
                MessageBoxClosing?.Invoke(this, new MessageBoxClosingEventArgs
                {
                    Result = result
                });
            };
            ButtonsBox.Children.Add(b);
        }

        private void CloseButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            MessageBoxClosing?.Invoke(this, new MessageBoxClosingEventArgs
            {
                Result = MessageBoxResult.Close
            });
        }
    }

    public class MessageBoxClosingEventArgs : EventArgs
    {
        public MessageBoxResult Result { get; set; }
    }
}
