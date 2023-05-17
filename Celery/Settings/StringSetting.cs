using System;
using System.Windows.Controls;
using System.Windows;

namespace Celery.Settings
{
    public class StringSetting : Setting
    {
        public string Value { get; private set; }
        public Action<string> OnChangeEvent { get; private set; }
        public bool IsEnabled { get; private set; }

        public StringSetting(string name, string identifier, string defaultValue = "", bool enabled = true, Action<string> onChange = null) : base(name, identifier)
        {
            OnChangeEvent = onChange;
            IsEnabled = enabled;
            Value = GetValue(defaultValue);
            if (OnChangeEvent != null)
                OnChangeEvent(Value);
        }

        private TextBox TextBox;

        public void SetText(string value)
        {
            if (TextBox != null)
                TextBox.Text = value;
            else
                Value = value;
        }

        public void SetEnabled(bool enabled)
        {
            if (TextBox != null)
                TextBox.IsReadOnly = !enabled;
            else
                IsEnabled = enabled;
        }

        public override Grid GetComponent()
        {
            Grid grid = base.GetComponent();
            grid.Height = 60;
            TextBox = new TextBox
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Bottom,
                Margin = new Thickness(5),
                Height = 25,
                Text = Value,
                IsReadOnly = !IsEnabled
            };
            TextBox.TextChanged += (s, e) =>
            {
                Value = TextBox.Text;
                if (OnChangeEvent != null)
                    OnChangeEvent(Value);

                if (SaveManager.Instance != null)
                    SaveManager.Instance.Save(Identifier, Value);
            };
            grid.Children.Add(TextBox);
            return grid;
        }
    }
}
