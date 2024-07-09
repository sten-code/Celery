using System.Threading.Tasks;
using System.Windows;
using System;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Celery.Utils;

namespace Celery.Controls;

public class Tabs : TabControl
{
    public double TabWidth { get; set; } = 100;
    public double TabSpacing { get; set; } = 4;
    public double MaxWidthSubtraction { get; set; } = 5;

    private readonly double _addTabButtonWidth = 23;
    private Tab _draggingItem;
    private Point _startPoint;
    private readonly Grid _root;
    private bool _loaded;

    private T GetTemplateItem<T>(Control elem, string name) =>
        elem.Template.FindName(name, elem) is T name1 ? name1 : default;

    public Tabs(Grid root)
    {
        _root = root;
        AllowDrop = true;

        Loaded += (_, _) =>
        {
            if (_loaded)
                return;
            _loaded = true;

            GetTemplateItem<Button>(this, "AddTabButton").Click += (t, a) =>
            {
                Editor editor = Editor.GetEditor("");
                editor.TextChangedEvent += (_, e) =>
                {
                    Dispatcher.Invoke(() => { ((Tab)editor.Parent).IsUnsaved = e.Text != "" && !e.IsInitialization; });
                };
                MakeTab(editor);
            };
        };

        Drop += (_, e) =>
        {
            _draggingItem = null;
            Point point = e.GetPosition(this);
            Thickness margin = new(point.X, point.Y, ActualWidth - point.X, ActualHeight - point.Y);
            Point barrier = new(ActualWidth / 4.0, ActualHeight / 4.0);

            Tab tab = (Tab)e.Data.GetData("Tab");
            Tabs parent = (Tabs)e.Data.GetData("Parent");

            if (tab == null || parent == null)
                return;

            // Add it back to this tab control
            if (margin.Top < 25)
            {
                parent.CloseTab(tab, false);
                MakeTab(tab.Content, tab.Header.ToString());
                return;
            }

            // Can't drop into itself if there is only 1 tab left
            if (parent == this && Items.Count <= 1)
                return;

            // Dock it right
            if (margin.Right < barrier.X)
            {
                parent.CloseTab(tab, false, false);

                // Add a new column definition to the parent grid
                Grid grid = (Grid)((Grid)Parent).Parent;
                grid.ColumnDefinitions.Add(new ColumnDefinition
                {
                    Width = new GridLength(2)
                });
                grid.ColumnDefinitions.Add(new ColumnDefinition());

                // Create a new instance of a tab control
                Tabs newTabs = new(_root);
                Grid newRowGrid = new();
                newRowGrid.RowDefinitions.Add(new RowDefinition());
                newRowGrid.Children.Add(newTabs);

                Grid newColumnGrid = new();
                newColumnGrid.ColumnDefinitions.Add(new ColumnDefinition());
                newColumnGrid.Children.Add(newRowGrid);

                grid.Children.Add(newColumnGrid);
                Grid.SetColumn(newColumnGrid, 2);

                GridSplitter splitter = new()
                {
                    Width = 2,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Stretch,
                    Background = (SolidColorBrush)Application.Current.Resources["BorderBrush"]
                };
                grid.Children.Add(splitter);
                Grid.SetColumn(splitter, 1);


                ((Grid)Parent).Children.Remove(this);
                grid.Children.RemoveAt(0);

                Grid rowGrid = new();
                rowGrid.RowDefinitions.Add(new RowDefinition());
                rowGrid.Children.Add(this);

                Grid columnGrid = new();
                columnGrid.ColumnDefinitions.Add(new ColumnDefinition());
                columnGrid.Children.Add(rowGrid);

                grid.Children.Add(columnGrid);
                Grid.SetColumn(columnGrid, 0);

                newTabs.MakeTab(tab.Content, tab.Header.ToString());
            }

            // Dock it left
            else if (margin.Left < barrier.X)
            {
                parent.CloseTab(tab, false, false);

                // Add a new column definition to the parent grid
                Grid grid = (Grid)((Grid)Parent).Parent;
                grid.ColumnDefinitions.Insert(0, new ColumnDefinition
                {
                    Width = new GridLength(2)
                });
                grid.ColumnDefinitions.Insert(0, new ColumnDefinition());

                // Shift everything after the tabs twice to the right
                foreach (UIElement element in grid.Children)
                {
                    int c = Grid.GetColumn(element);
                    Grid.SetColumn(element, c + 2);
                }

                // Create a new instance of a tab control
                Tabs newTabs = new Tabs(_root);
                Grid newRowGrid = new Grid();
                newRowGrid.Children.Add(newTabs);
                newRowGrid.RowDefinitions.Add(new RowDefinition());

                Grid newColumnGrid = new Grid();
                newColumnGrid.Children.Add(newRowGrid);
                newColumnGrid.ColumnDefinitions.Add(new ColumnDefinition());

                grid.Children.Add(newColumnGrid);
                Grid.SetColumn(newColumnGrid, 0);

                // Create a new grid splitter
                GridSplitter splitter = new GridSplitter
                {
                    Width = 2,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Stretch,
                    Background = (SolidColorBrush)Application.Current.Resources["BorderBrush"]
                };
                grid.Children.Add(splitter);
                Grid.SetColumn(splitter, 1);


                ((Grid)Parent).Children.Remove(this);
                grid.Children.RemoveAt(0);

                Grid rowGrid = new Grid();
                rowGrid.RowDefinitions.Add(new RowDefinition());
                rowGrid.Children.Add(this);

                Grid columnGrid = new Grid();
                columnGrid.ColumnDefinitions.Add(new ColumnDefinition());
                columnGrid.Children.Add(rowGrid);

                grid.Children.Add(columnGrid);
                Grid.SetColumn(columnGrid, 2);

                newTabs.MakeTab(tab.Content, tab.Header.ToString());
            }

            // Dock it at the bottom
            else if (margin.Bottom < barrier.Y)
            {
                parent.CloseTab(tab, false, false);

                // Add a new column definition to the parent grid
                Grid grid = (Grid)Parent;
                grid.RowDefinitions.Add(new RowDefinition
                {
                    Height = new GridLength(2)
                });
                grid.RowDefinitions.Add(new RowDefinition());

                // Create a new instance of a tab control
                Tabs newTabs = new Tabs(_root);
                Grid newRowGrid = new Grid();
                newRowGrid.RowDefinitions.Add(new RowDefinition());
                newRowGrid.Children.Add(newTabs);

                Grid newColumnGrid = new Grid();
                newColumnGrid.ColumnDefinitions.Add(new ColumnDefinition());
                newColumnGrid.Children.Add(newRowGrid);

                grid.Children.Add(newColumnGrid);
                Grid.SetRow(newColumnGrid, 2);

                GridSplitter splitter = new GridSplitter
                {
                    Height = 2,
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    VerticalAlignment = VerticalAlignment.Center,
                    Background = (SolidColorBrush)Application.Current.Resources["BorderBrush"]
                };
                grid.Children.Add(splitter);
                Grid.SetRow(splitter, 1);


                ((Grid)Parent).Children.Remove(this);

                Grid rowGrid = new Grid();
                rowGrid.RowDefinitions.Add(new RowDefinition());
                rowGrid.Children.Add(this);

                Grid columnGrid = new Grid();
                columnGrid.ColumnDefinitions.Add(new ColumnDefinition());
                columnGrid.Children.Add(rowGrid);

                grid.Children.Add(columnGrid);
                Grid.SetRow(columnGrid, 0);

                newTabs.MakeTab(tab.Content, tab.Header.ToString());
            }

            // Dock it at the top
            else if (margin.Top < barrier.Y)
            {
                parent.CloseTab(tab, false, false);

                // Add a new column definition to the parent grid
                Grid grid = (Grid)Parent;
                grid.RowDefinitions.Insert(0, new RowDefinition
                {
                    Height = new GridLength(2)
                });
                grid.RowDefinitions.Insert(0, new RowDefinition());

                // Shift everything after the tabs twice to the bottom
                foreach (UIElement element in grid.Children)
                {
                    int c = Grid.GetRow(element);
                    Grid.SetRow(element, c + 2);
                }

                // Create a new instance of a tab control
                Tabs newTabs = new Tabs(_root);
                Grid newRowGrid = new Grid();
                newRowGrid.RowDefinitions.Add(new RowDefinition());
                newRowGrid.Children.Add(newTabs);

                Grid newColumnGrid = new Grid();
                newColumnGrid.ColumnDefinitions.Add(new ColumnDefinition());
                newColumnGrid.Children.Add(newRowGrid);

                grid.Children.Add(newColumnGrid);
                Grid.SetRow(newColumnGrid, 0);

                GridSplitter splitter = new GridSplitter
                {
                    Height = 2,
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    VerticalAlignment = VerticalAlignment.Center,
                    Background = (SolidColorBrush)Application.Current.Resources["BorderBrush"]
                };
                grid.Children.Add(splitter);
                Grid.SetRow(splitter, 1);

                ((Grid)Parent).Children.Remove(this);

                Grid rowGrid = new Grid();
                rowGrid.RowDefinitions.Add(new RowDefinition());
                rowGrid.Children.Add(this);

                Grid columnGrid = new Grid();
                columnGrid.ColumnDefinitions.Add(new ColumnDefinition());
                columnGrid.Children.Add(rowGrid);

                grid.Children.Add(columnGrid);
                Grid.SetRow(columnGrid, 2);

                newTabs.MakeTab(tab.Content, tab.Header.ToString());
            }
        };
    }

    public Tab FindSelectedTab(Grid grid = null)
    {
        if (grid == null)
            grid = _root;

        foreach (UIElement element in grid.Children)
        {
            if (element is Grid gridElement)
            {
                Tab tab = FindSelectedTab(gridElement);
                if (tab != null)
                    return tab;
            }
            else if (element is Tabs tabs)
            {
                foreach (Tab tab in tabs.Items)
                {
                    if (tab.IsHighlighted)
                        return tab;
                }
            }
        }

        return null;
    }

    private void DeselectAll(Grid grid)
    {
        foreach (UIElement element in grid.Children)
        {
            if (element is Grid gridElement)
            {
                DeselectAll(gridElement);
            }
            else if (element is Tabs tabs)
            {
                foreach (Tab tab in tabs.Items)
                    tab.IsHighlighted = false;
            }
        }
    }

    public Tab MakeTab(object content, string title = "New Tab")
    {
        DeselectAll(_root);
        Tab tab = new()
        {
            Header = title,
            AllowDrop = true,
            IsHighlighted = true,
            Content = content,
            Uid = Guid.NewGuid().ToString()
        };

        tab.PreviewMouseLeftButtonDown += async (s, e) =>
        {
            if (e.OriginalSource is Border || e.OriginalSource is TextBlock)
            {
                _draggingItem = tab;
                _startPoint = e.GetPosition(null);
                DeselectAll(_root);
                await Task.Delay(10);
                tab.IsHighlighted = tab.IsSelected;
            }
        };
        tab.MouseDown += (s, e) =>
        {
            if (e.OriginalSource is Border || e.OriginalSource is TextBlock)
            {
                if (e.MiddleButton == MouseButtonState.Pressed)
                {
                    CloseTab(tab);
                }
            }
        };
        tab.MouseUp += (s, e) =>
        {
            _draggingItem = null;
        };
        tab.Loaded += (s, e) =>
        {
            Button closeBtn = GetTemplateItem<Button>(tab, "CloseButton");
            if (closeBtn.Tag != null)
                return;
            closeBtn.Tag = true;

            closeBtn.Click += (c, a) =>
            {
                CloseTab(tab);
            };
        };
        tab.MouseMove += (s, e) =>
        {
            if (_draggingItem == tab)
            {
                Point mousePosition = e.GetPosition(null);
                Vector diff = _startPoint - mousePosition;

                if (e.LeftButton == MouseButtonState.Pressed &&
                    (Math.Abs(diff.X) > SystemParameters.MinimumHorizontalDragDistance ||
                     Math.Abs(diff.Y) > SystemParameters.MinimumVerticalDragDistance))
                {
                    // Create a DataObject and add the Tab as its data.
                    DataObject dragData = new DataObject();
                    dragData.SetData("Tab", _draggingItem);
                    dragData.SetData("Parent", this);

                    // Perform drag-and-drop.
                    DragDrop.DoDragDrop(tab, dragData, DragDropEffects.Move);
                }
            }
        };

        double maxWidth = ActualWidth - MaxWidthSubtraction;
        double width = _addTabButtonWidth + (TabSpacing * 2) + TabWidth;
        foreach (Tab t in Items)
            width += t.ActualWidth;

        double newWidth = TabWidth;
        if (width > maxWidth)
        {
            newWidth = (maxWidth - _addTabButtonWidth) / (Items.Count + 1);
            foreach (Tab t in Items)
            {
                AnimationUtils.AnimateWidth(t, t.ActualWidth, newWidth, AnimationUtils.EaseInOut, 200);
            }
        }

        SelectedIndex = Items.Add(tab);
        AnimationUtils.AnimateWidth(tab, 0, newWidth, AnimationUtils.EaseInOut, 200);
        return tab;
    }

    private int CountTabControls(Grid grid)
    {
        int count = 0;
        foreach (UIElement element in grid.Children)
        {
            switch (element)
            {
                case Grid gridElement:
                    count += CountTabControls(gridElement);
                    break;
                case Tabs _:
                    count++;
                    break;
            }
        }
        return count;
    }

    public async void CloseTab(Tab tab, bool disposeContent = true, bool wait = true)
    {
        AnimationUtils.AnimateWidth(tab, tab.ActualWidth, 0, AnimationUtils.EaseInOut, 200);
        double maxWidth = ActualWidth - MaxWidthSubtraction;
        double width = -(TabWidth + TabSpacing);
        foreach (Tab t in Items)
            width += t.ActualWidth;

        if (width < maxWidth)
        {
            double newWidth = Math.Min((maxWidth - _addTabButtonWidth) / (Items.Count - 1), TabWidth);
            foreach (Tab t in Items)
            {
                if (t != tab)
                    AnimationUtils.AnimateWidth(t, t.ActualWidth, newWidth, AnimationUtils.EaseInOut, 200);
            }
        }

        if (wait)
            await Task.Delay(200);
        if (disposeContent && tab.Content is IDisposable disposable)
            disposable.Dispose();
        Items.Remove(tab);

        if (FindSelectedTab() == null)
            foreach (Tab t in Items)
                t.IsHighlighted = t.IsSelected;

        if (Items.Count == 0 && CountTabControls(_root) > 1)
        {
            // If this isn't the last tab control, remove it
            Grid rowGrid = (Grid)Parent;
            Grid columnGrid = (Grid)rowGrid.Parent;
            Grid parent = (Grid)columnGrid.Parent;

            rowGrid.Children.Remove(this);
            columnGrid.Children.Remove(rowGrid);
            parent.Children.Remove(columnGrid);

            GridSplitter splitter = null;
            foreach (UIElement element in parent.Children)
            {
                if (element is GridSplitter gridSplitter)
                {
                    splitter = gridSplitter;
                    break;
                }
            }

            if (splitter != null)
                parent.Children.Remove(splitter);

            if (parent.ColumnDefinitions.Count > 1)
            {
                parent.ColumnDefinitions.Clear();
                parent.ColumnDefinitions.Add(new ColumnDefinition());

                if (parent.Children.Count == 0)
                    return;

                Grid subColumnGrid = (Grid)parent.Children[0];
                if (subColumnGrid.Children.Count == 0)
                    return;

                Grid subRowGrid = (Grid)subColumnGrid.Children[0];
                subColumnGrid.Children.Remove(subRowGrid);
                parent.Children.Remove(subColumnGrid);
                parent.Children.Add(subRowGrid);

                if (subRowGrid.Children.Count > 0)
                    if (subRowGrid.Children[0] is Tabs)
                        foreach (Tab t in ((Tabs)subRowGrid.Children[0]).Items)
                            t.IsHighlighted = t.IsSelected;
            }

            if (parent.RowDefinitions.Count > 1)
            {
                parent.RowDefinitions.Clear();
                parent.RowDefinitions.Add(new RowDefinition());

                if (parent.Children.Count == 0)
                    return;

                Grid subColumnGrid = (Grid)parent.Children[0];
                if (subColumnGrid.Children.Count == 0)
                    return;

                Grid subRowGrid = (Grid)subColumnGrid.Children[0];
                if (subRowGrid.Children.Count == 0)
                    return;

                FrameworkElement subTabs = (FrameworkElement)subRowGrid.Children[0];

                subRowGrid.Children.Remove(subTabs);
                subColumnGrid.Children.Remove(subRowGrid);
                parent.Children.Remove(subColumnGrid);
                parent.Children.Add(subTabs);
                if (subTabs is Tabs tabs)
                    foreach (Tab t in tabs.Items)
                        t.IsHighlighted = t.IsSelected;
            }
        }
    }

    protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
    {
        base.OnRenderSizeChanged(sizeInfo);
        double maxWidth = ActualWidth - MaxWidthSubtraction;
        double newWidth = Math.Min((maxWidth - _addTabButtonWidth) / Items.Count, TabWidth);
        foreach (Tab t in Items)
        {
            // For some reason setting the width doesn't work so an animation with a duration of 0ms will have to do.
            AnimationUtils.AnimateWidth(t, t.ActualWidth, newWidth, AnimationUtils.EaseInOut, 0);
        }
    }

}