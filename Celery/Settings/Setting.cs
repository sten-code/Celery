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
            if (SaveManager.Instance == null)
            {
                returnValue = defaultValue;
            }
            else
            {
                returnValue = SaveManager.Instance.Load(Identifier, defaultValue);
            }

            if (SaveManager.Instance != null && returnValue != null)
                SaveManager.Instance.Save(Identifier, returnValue);

            return returnValue;
        }

        public virtual Grid GetComponent()
        {
            Grid grid = new Grid();
            grid.Children.Add(new TextBlock
            {
                Text = Name,
                Foreground = new SolidColorBrush(Color.FromRgb(224, 235, 230)),
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(7,5,5,5),
                FontSize = 13
            });
            return grid;
        }
    }
}
