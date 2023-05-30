using Celery.Utils;
using System;
using System.Windows.Controls;
using System.Windows.Markup;

namespace Celery.Controls
{
    public partial class MessageBox : UserControl
    {
        public event EventHandler<MessageBoxClosingEventArgs> MessageBoxClosing;

        public MessageBox(string title, string content, bool closeButton, bool inputBox)
        {
            InitializeComponent();
            TitleBox.Text = title;
            ContentBox.Text = content;
            ButtonsBox.Children.Clear();
            if (!closeButton)
                CloseButton.Visibility = System.Windows.Visibility.Hidden;

            if (!inputBox)
            {
                InputBox.Visibility = System.Windows.Visibility.Hidden;
                ContentGrid.Margin = new System.Windows.Thickness(20,50,20,50);
            }
        }

        public MessageBox(string title, System.Windows.UIElement content, bool closeButton, bool inputBox)
        {
            InitializeComponent();
            TitleBox.Text = title;
            ContentGrid.Children.Clear();
            ContentGrid.Children.Add(content);
            ButtonsBox.Children.Clear();
            if (!closeButton)
                CloseButton.Visibility = System.Windows.Visibility.Hidden;

            if (!inputBox)
            {
                InputBox.Visibility = System.Windows.Visibility.Hidden;
                ContentGrid.Margin = new System.Windows.Thickness(20, 50, 20, 50);
            }
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
