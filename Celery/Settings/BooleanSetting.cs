using System;
using System.Windows.Input;
using Celery.Core;

namespace Celery.Settings
{
    public class BooleanSetting : Setting
    {
        private bool _value { get; set; }

        public bool Value
        {
            get => _value;
            set
            {
                _value = value;
                OnPropertyChanged();
            }
        }

        public ICommand CheckedCommand { get; }

        private Action<BooleanSetting, bool> OnChanged { get; }

        public BooleanSetting(string name, string id, string description, bool value, Action<BooleanSetting, bool> onChanged) : base(name, id, description)
        {
            Value = value;
            OnChanged = onChanged;
            CheckedCommand = new RelayCommand(o =>
            {
                if (OnChanged != null)
                    OnChanged(this, Value);
            }, o => true);
        }

        public override object GetValue()
        {
            return Value;
        }

        public override void SetValue(object value)
        {
            if (value is bool b)
                Value = b;
        }
    }
}