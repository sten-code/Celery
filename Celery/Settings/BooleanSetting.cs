using System;
using System.Windows.Controls;
using System.Windows;

namespace Celery.Settings
{
    public class BooleanSetting : Setting
    {
        public bool Value { get; private set; }
        public Action<bool> OnChangeEvent { get; private set; }

        public BooleanSetting(string name, string identifier, bool value, Action<bool> onChange = null) : base(name, identifier)
        {
            OnChangeEvent = onChange;
            Value = GetValue(value);
            if (OnChangeEvent != null)
                OnChangeEvent(Value);
        }

        public override Grid GetComponent()
        {
            Grid grid = base.GetComponent();
            CheckBox checkBox = new CheckBox
            {
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Bottom,
                Margin = new Thickness(5,5,0,5),
                IsChecked = Value
            };
            checkBox.Checked += CheckBox_CheckedChanged;
            checkBox.Unchecked += CheckBox_CheckedChanged;
            grid.Children.Add(checkBox);
            return grid;
        }

        private void CheckBox_CheckedChanged(object sender, RoutedEventArgs e)
        {
            CheckBox checkBox = (CheckBox)sender;
            Value = checkBox.IsChecked.GetValueOrDefault();
            if (OnChangeEvent != null)
                OnChangeEvent(Value);

            if (SettingsSaveManager.Instance != null)
                SettingsSaveManager.Instance.Save(Identifier, Value);
        }

    }
}
