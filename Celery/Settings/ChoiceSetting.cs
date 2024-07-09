using System;
using System.Collections.Generic;
using System.Windows.Input;
using Celery.Core;

namespace Celery.Settings;

public class ChoiceSetting : Setting
{
    private int _selectedIndex { get; set; }

    public int SelectedIndex
    {
        get => _selectedIndex;
        set
        {
            _selectedIndex = value;
            OnPropertyChanged();
        }
    }

    private List<string> _options { get; set; }

    public List<string> Options
    {
        get => _options;
        set
        {
            _options = value;
            OnPropertyChanged();
        }
    }

    public ICommand ChangedCommand { get; }
        
    private Action<ChoiceSetting, string> OnChanged { get; }

    public ChoiceSetting(string name, string id, List<string> options, int selectedIndex, Action<ChoiceSetting, string> onChanged) : base(name, id)
    {
        OnChanged = onChanged;
        Options = options;
        SelectedIndex = selectedIndex;
        ChangedCommand = new RelayCommand(o =>
        {
            if (OnChanged != null)
                OnChanged(this, Options[SelectedIndex]);
        }, o => true);
    }

    public override object GetValue()
    {
        return Options[SelectedIndex];
    }

    public override void SetValue(object value)
    {
        if (!(value is string str))
            return;
            
        int index = Options.IndexOf(str);
        if (index >= 0)
            SelectedIndex = index;
    }
}