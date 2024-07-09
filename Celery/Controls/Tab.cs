using System.Windows;
using System.Windows.Controls;

namespace Celery.Controls;

public class Tab : TabItem
{
    public static readonly DependencyProperty IsHighlightedProperty = DependencyProperty.Register(
        nameof(IsHighlighted),
        typeof(bool), typeof(Tab), new PropertyMetadata(true));

    public static readonly DependencyProperty IsUnsavedProperty = DependencyProperty.Register(nameof(IsUnsaved),
        typeof(bool), typeof(Tab), new PropertyMetadata(false));

    public static readonly DependencyProperty FileNameProperty = DependencyProperty.Register(nameof(FileName),
        typeof(string), typeof(Tab), new PropertyMetadata(""));

    public bool IsHighlighted
    {
        get => (bool)GetValue(IsHighlightedProperty);
        set => SetValue(IsHighlightedProperty, value);
    }

    public bool IsUnsaved
    {
        get => (bool)GetValue(IsUnsavedProperty);
        set => SetValue(IsUnsavedProperty, value);
    }

    public string FileName
    {
        get => (string)GetValue(FileNameProperty);
        set => SetValue(FileNameProperty, value);
    }
}