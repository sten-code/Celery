using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Celery.Settings
{
    public partial class SettingsMenu : UserControl
    {
        public SettingsMenu()
        {
            InitializeComponent();
        }

        public void AddSetting(Setting setting)
        {
            Border baseComponent = new Border
            {
                Margin = new Thickness(10, 10, 10, -5),
                BorderThickness = new Thickness(1),
                BorderBrush = new SolidColorBrush(Color.FromRgb(64, 84, 89)),
                CornerRadius = new CornerRadius(4)
            };

            baseComponent.Child = setting.GetComponent();
            SettingsPanel.Children.Add(baseComponent);
        }

        public void AddSettings(params Setting[] settings)
        {
            foreach (Setting setting in settings)
            {
                AddSetting(setting);
            }
        }

    }
}
