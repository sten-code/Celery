using System.Windows.Controls;
using System.Windows.Media;
using System.Windows;

namespace Celery.Settings
{
    public class Setting
    {
        public string Name { get; private set; }
        public string Identifier { get; private set; }

        public Setting(string name, string identifier = "")
        {
            Name = name;
            Identifier = identifier;
        }

        public T GetValue<T>(T defaultValue)
        {
            T returnValue;
            if (SettingsSaveManager.Instance == null)
            {
                returnValue = defaultValue;
            }
            else
            {
                returnValue = SettingsSaveManager.Instance.Load(Identifier, defaultValue);
            }

            if (SettingsSaveManager.Instance != null && returnValue != null)
                SettingsSaveManager.Instance.Save(Identifier, returnValue);

            return returnValue;
        }

        public virtual Grid GetComponent()
        {
            Grid grid = new Grid();
            grid.Children.Add(new TextBlock
            {
                Text = Name,
                Foreground = (SolidColorBrush)Application.Current.Resources["ForegroundBrush"],
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(7,5,5,5),
                FontSize = 13
            });
            return grid;
        }
    }
}
