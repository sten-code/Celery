using System;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows;

namespace Celery.Settings
{
    public class NumberSetting : Setting
    {
        public double Value { get; private set; }
        public double Minimum { get; private set; }
        public double Maximum { get; private set; }
        public double StepSize { get; private set; }
        public Action<double> OnChangeEvent { get; private set; }

        public NumberSetting(string name, string identifier, double value, double minimum, double maximum, double stepSize = 0, Action<double> onChange = null) : base(name, identifier)
        {
            OnChangeEvent = onChange;
            Minimum = minimum;
            Maximum = maximum;
            StepSize = stepSize;
            Value = GetValue(value);
            if (OnChangeEvent != null)
                OnChangeEvent(Value);
        }

        public override Grid GetComponent()
        {
            Grid grid = base.GetComponent();
            grid.Height = 55;
            TextBox numberBox = new TextBox
            {
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Bottom,
                Margin = new Thickness(5),
                Width = 32,
            };
            Slider slider = new Slider
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Bottom,
                Margin = new Thickness(45,5,10,5),
                Value = Value,
                Minimum = Minimum,
                Maximum = Maximum,
                IsSnapToTickEnabled = StepSize != 0,
                TickFrequency = StepSize,
                TickPlacement = System.Windows.Controls.Primitives.TickPlacement.BottomRight
            };
            Binding b = new Binding("Value");
            b.Source = slider;
            b.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
            numberBox.SetBinding(TextBox.TextProperty, b);
            slider.ValueChanged += Slider_ValueChanged;
            grid.Children.Add(slider);
            grid.Children.Add(numberBox);
            return grid;
        }

        private void Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            Slider slider = (Slider)sender;
            Value = slider.Value;
            if (OnChangeEvent != null)
                OnChangeEvent(Value);

            if (SettingsSaveManager.Instance != null)
                SettingsSaveManager.Instance.Save(Identifier, Value);
        }
    }
}
