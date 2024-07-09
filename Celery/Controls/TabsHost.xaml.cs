using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace Celery.Controls;

public partial class TabsHost : UserControl
{
    public TabsHost()
    {
        InitializeComponent();
        RowGrid.Children.Add(new Tabs(RootGrid));
    }

    public Tab FindSelectedTab(Grid grid = null)
    {
        if (grid == null)
            grid = RootGrid;

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

    public Editor FindSelectedEditor()
    {
        Tab tab = FindSelectedTab();
        if (tab?.Content is not Editor editor)
            return null;

        return editor;
    }

    public Tabs FindFirstTabControl(Grid grid = null)
    {
        if (grid == null)
            grid = RootGrid;

        foreach (UIElement element in grid.Children)
        {
            if (element is Grid gridElement)
            {
                Tabs tabs = FindFirstTabControl(gridElement);
                if (tabs != null)
                    return tabs;
            }
            else if (element is Tabs tabs)
            {
                return tabs;
            }
        }

        return null;
    }

    public Tabs FindSelectedTabControl(Grid grid = null)
    {
        if (grid == null)
            grid = RootGrid;

        foreach (UIElement element in grid.Children)
        {
            if (element is Grid gridElement)
            {
                Tabs tabs = FindSelectedTabControl(gridElement);
                if (tabs != null)
                    return tabs;
            }
            else if (element is Tabs tabs)
            {
                foreach (Tab tab in tabs.Items)
                {
                    if (tab.IsHighlighted)
                        return tabs;
                }
            }
        }

        return grid == RootGrid ? FindFirstTabControl() : null;
    }

    public Tab MakeTab(string content, string header)
    {
        Tabs tabs = FindSelectedTabControl();
        if (tabs == null)
        {
            Console.WriteLine("Tabs was null");
            return null;
        }

        Editor editor = Editor.GetEditor(content);
        Tab tab = tabs.MakeTab(editor, header);
        editor.TextChangedEvent += (_, e) =>
        {
            Dispatcher.Invoke(() => { tab.IsUnsaved = e.Text != "" && !e.IsInitialization; });
        };
        return tab;
    }

    public Tab MakeTab(object content, string header)
    {
        Tabs tabs = FindSelectedTabControl();
        if (tabs == null)
        {
            Console.WriteLine("Tabs was null");
            return null;
        }

        return tabs.MakeTab(content, header);
    }

    public List<Tab> GetTabs(Grid grid = null)
    {
        if (grid == null)
            grid = RootGrid;

        List<Tab> allTabs = [];
        foreach (UIElement element in grid.Children)
        {
            if (element is Grid gridElement)
            {
                List<Tab> subTabs = GetTabs(gridElement);
                allTabs.AddRange(subTabs);
            }
            else if (element is Tabs tabs)
            {
                return tabs.Items.OfType<Tab>().ToList();
            }
        }

        return allTabs;
    }
}