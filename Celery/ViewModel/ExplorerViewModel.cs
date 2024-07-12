using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Celery.Controls;
using Celery.Core;

namespace Celery.ViewModel;

public class ScriptEventArgs : EventArgs
{
    public string Content;
    public string Header;
    public string FileName;
}

public class ExplorerViewModel : Core.ViewModel
{
    public event EventHandler<ScriptEventArgs> ExecuteScript;

    public ICommand CloseCommand { get; set; }
    public ICommand OpenFolderLocationCommand { get; }
    public TreeView FileTreeView { get; }
    public TabsHost TabsHost { get; }

    private FileSystemWatcher _watcher { get; }

    public ExplorerViewModel(TabsHost tabsHost)
    {
        TabsHost = tabsHost;

        OpenFolderLocationCommand = new RelayCommand(_ => Process.Start(Config.ScriptsPath), _ => true);

        FileTreeView = new TreeView();
        FileTreeView.Items.SortDescriptions.Clear();
        FileTreeView.Items.SortDescriptions.Add(new SortDescription("Tag", ListSortDirection.Ascending));
        FileTreeView.Items.SortDescriptions.Add(new SortDescription("Header", ListSortDirection.Ascending));

        foreach (string dir in Directory.GetDirectories(Config.ScriptsPath))
        {
            TreeViewItem newItem = CreateScriptItem(dir, false);
            AddFilesToTree(newItem, dir);
            FileTreeView.Items.Add(newItem);
        }

        foreach (string filename in Directory.GetFiles(Config.ScriptsPath))
            FileTreeView.Items.Add(CreateScriptItem(filename, true));

        FileTreeView.Items.Refresh();

        _watcher = new FileSystemWatcher(Config.ScriptsPath)
        {
            NotifyFilter = NotifyFilters.FileName | NotifyFilters.DirectoryName,
            IncludeSubdirectories = true,
            EnableRaisingEvents = true
        };
        _watcher.Created += Watcher_Created;
        _watcher.Deleted += Watcher_Deleted;
        _watcher.Renamed += Watcher_Renamed;
    }

    private void AddFilesToTree(ItemsControl item, string path)
    {
        foreach (string dir in Directory.GetDirectories(path))
        {
            TreeViewItem newItem = CreateScriptItem(dir, false);
            AddFilesToTree(newItem, dir);
            item.Items.Add(newItem);
        }

        foreach (string filename in Directory.GetFiles(path))
            item.Items.Add(CreateScriptItem(filename, true));
    }

    private ItemsControl GetItemFromPath(string path)
    {
        string[] dirs = path.Split(['\\'], StringSplitOptions.RemoveEmptyEntries);
        ItemsControl final = FileTreeView;
        ItemCollection collection = FileTreeView.Items;
        foreach (string name in dirs)
        {
            foreach (TreeViewItem item in collection)
            {
                if (item.Header.ToString() == name)
                {
                    final = item;
                    collection = item.Items;
                    break;
                }
            }
        }

        return final;
    }

    private void Watcher_Created(object obj, FileSystemEventArgs e)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            // This gets the parent, because the actual item with the name doesn't exist yet.
            ItemsControl final = GetItemFromPath(e.Name);
            TreeViewItem newItem = CreateScriptItem(e.FullPath, File.Exists(e.FullPath));
            final.Items.Add(newItem);
            final.Items.Refresh();
        });
    }

    private TreeViewItem CreateScriptItem(string path, bool isFile)
    {
        TreeViewItem item = new()
        {
            Header = Path.GetFileName(path),
            Uid = path,
            Tag = isFile ? "True" : "False"
        };

        item.Items.SortDescriptions.Clear();
        item.Items.SortDescriptions.Add(new SortDescription("Tag", ListSortDirection.Ascending));
        item.Items.SortDescriptions.Add(new SortDescription("Header", ListSortDirection.Ascending));

        // The open script button
        MenuItem open = new()
        {
            Header = "Open"
        };
        open.Click += (s, e) =>
        {
            Tab tab = TabsHost.MakeTab(File.ReadAllText(item.Uid), item.Header.ToString());
            if (tab == null)
                return;

            tab.FileName = item.Uid;
        };

        MenuItem execute = new()
        {
            Header = "Execute"
        };
        execute.Click += (s, e) =>
        {
            ExecuteScript?.Invoke(this, new ScriptEventArgs
            {
                Content = File.ReadAllText(item.Uid),
                Header = item.Header.ToString(),
                FileName = item.Uid
            });
        };

        if (isFile)
        {
            item.ContextMenu = new ContextMenu
            {
                Items =
                {
                    open,
                    execute
                }
            };

            item.MouseDoubleClick += (s, e) =>
            {
                Tab tab = TabsHost.MakeTab(File.ReadAllText(item.Uid), item.Header.ToString());
                if (tab == null)
                    return;

                tab.FileName = item.Uid;
            };

            item.MouseRightButtonDown += (s, e) => { item.IsSelected = true; };
        }

        return item;
    }

    private void Watcher_Deleted(object obj, FileSystemEventArgs e)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            ItemsControl final = GetItemFromPath(e.Name);
            ItemsControl parent = (ItemsControl)final.Parent;
            parent.Items.Remove(final);
            parent.Items.Refresh();
        });
    }

    private void Watcher_Renamed(object obj, RenamedEventArgs e)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            ItemsControl control = GetItemFromPath(e.OldName);
            if (control is TreeViewItem final)
            {
                final.Uid = e.FullPath;
                final.Header = Path.GetFileName(e.Name);

                ItemsControl parent = (ItemsControl)final.Parent;
                parent.Items.Refresh();
            }
            else
            {
                TreeViewItem newItem = CreateScriptItem(e.FullPath, File.Exists(e.FullPath));
                control.Items.Add(newItem);
                control.Items.Refresh();
            }
        });
    }

}