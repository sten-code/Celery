using Celery.Controls;
using Celery.Utils;
using Microsoft.Win32;
using System.Collections.Generic;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using System.Reflection;
using System.IO.Compression;
using Celery.Settings;

namespace Celery
{
    public partial class MainWindow : Window
    {
        public CeleryAPI.CeleryAPI Celery;
        public WebClient WebClient;

        private bool _autoAttach = false;

        public MainWindow()
        {
            InitializeComponent();
            WebClient = new WebClient();
            WebClient.Headers["User-Agent"] = "Celery WebClient";
            Tabs.GetContent = Tabs_GetContent;
            Tabs.MakeTab();

            MessageBoxUtils.BaseGrid = BaseGrid;
            MessageBoxUtils.BlurGrid = BlurGrid;
            MessageBoxUtils.Tabs = Tabs;

            if (!Directory.Exists(Config.ScriptsPath)) 
                Directory.CreateDirectory(Config.ScriptsPath);

            ExtractZipFromResources("Ace", Properties.Resources.Ace, "\\bin");
            ExtractZipFromResources("dll", Properties.Resources.dll, "");

            new SaveManager(Config.SettingsFilePath);

            SettingsMenu.AddSettings(
                new BooleanSetting("Topmost", "topmost", false, onChange: (value) =>
                {
                    Topmost = value;
                }),
                new BooleanSetting("Auto Attach", "autoattach", false, onChange: (value) =>
                {
                    _autoAttach = value;
                })
            );

            DispatcherTimer timer = new DispatcherTimer();
            timer.Tick += (s, e) =>
            {
                if (_autoAttach)
                {
                    Celery.Inject(false);
                }
            };
            timer.Interval = new TimeSpan(0, 0, 0, 0, 3000);
            timer.Start();

            CreateFileWatcher();
            Celery = new CeleryAPI.CeleryAPI();

            CheckUpdates();
        }

        public void ExtractZipFromResources(string name, byte[] resource, string path)
        {
            if (!Directory.Exists(Config.ApplicationPath + path + "\\" + name))
            {
                Directory.CreateDirectory(Config.ApplicationPath + path + "\\" + name);
                try
                {
                    File.WriteAllBytes($"{Config.ApplicationPath}\\{name}.zip", resource);
                    ZipFile.ExtractToDirectory($"{Config.ApplicationPath}\\{name}.zip", Config.ApplicationPath + path + "\\" + name);
                    File.Delete($"{Config.ApplicationPath}\\{name}.zip");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.Message);
                }
            }
        }

        public async void CheckUpdates()
        {
            Dictionary<string, object> latest;
            Version currentVersion;
            Version latestVersion;
            try
            {
                string json = WebClient.DownloadString("https://api.github.com/repos/sten-code/Celery/releases/latest");
                latest = json.FromJson<Dictionary<string, object>>();

                currentVersion = Assembly.GetExecutingAssembly().GetName().Version;
                latestVersion = Version.Parse((string)latest["tag_name"]);
            }
            catch
            {
                await MessageBoxUtils.ShowMessage("Error", "Couldn't check for updates", true, MessageBoxButtons.Ok);
                return;
            }

            if (currentVersion.CompareTo(latestVersion) >= 0)
                return;

            if (await MessageBoxUtils.ShowMessage("Update", "A new update was detected, do you want to update?", false, MessageBoxButtons.YesNo) == Utils.MessageBoxResult.No)
                return;

            try
            {
                List<Dictionary<string, object>> assets = (List<Dictionary<string, object>>)latest["assets"];
                string downloadUrl = (string)assets[0]["browser_download_url"];

                if (!Directory.Exists(Config.SettingsPath))
                    Directory.CreateDirectory(Config.SettingsPath);

                File.WriteAllBytes(Config.UpdaterPath, Properties.Resources.CeleryUpdater);
                Process updater = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = Config.UpdaterPath,
                        Arguments = $"\"{downloadUrl}\" \"{Config.ApplicationPath}\" \"{Process.GetCurrentProcess().Id}\"",
                        Verb = "runas"
                    }
                };
                updater.Start();
            }
            catch (Exception ex)
            {
                await MessageBoxUtils.ShowMessage("Error", $"There was an error while trying to start update. Error: {ex.Message}", true, MessageBoxButtons.Ok);
            }
        }

        private ListBoxItem CreateScriptItem(string header)
        {
            MenuItem open = new MenuItem
            {
                Header = "Open"
            };
            open.Click += (s, e) =>
            {
                string name = ((ListBoxItem)((ContextMenu)open.Parent).PlacementTarget).Content.ToString();
                Tabs.MakeTab(File.ReadAllText(Path.Combine(Config.ScriptsPath, name + ".lua")), name);
            };
            ListBoxItem item = new ListBoxItem
            {
                Content = header,
                ContextMenu = new ContextMenu
                {
                    Items =
                    {
                        open
                    }
                }
            };
            item.MouseDoubleClick += ScriptItem_MouseDoubleClick;
            return item;
        }

        public void CreateFileWatcher()
        {
            ScriptList.Items.SortDescriptions.Clear();
            ScriptList.Items.SortDescriptions.Add(new SortDescription("Content", ListSortDirection.Ascending));
            foreach (string filename in Directory.GetFiles(Config.ScriptsPath))
            {
                ScriptList.Items.Add(CreateScriptItem(Path.GetFileNameWithoutExtension(filename)));
            }

            FileSystemWatcher watcher = new FileSystemWatcher(Config.ScriptsPath)
            {
                NotifyFilter = NotifyFilters.FileName,
                Filter = "*.lua",
                IncludeSubdirectories = false,
                EnableRaisingEvents = true
            };
            watcher.Created += (s, e) =>
            {
                Dispatcher.Invoke(() =>
                {
                    ScriptList.Items.Add(CreateScriptItem(Path.GetFileNameWithoutExtension(e.FullPath)));
                    ScriptList.Items.Refresh();
                });
            };
            watcher.Deleted += (s, e) =>
            {
                Dispatcher.Invoke(() =>
                {
                    string itemRemove = Path.GetFileNameWithoutExtension(e.FullPath);
                    for (int i = ScriptList.Items.Count - 1; i >= 0; --i)
                    {
                        if (((ListBoxItem)ScriptList.Items[i]).Content.ToString() == itemRemove)
                        {
                            ScriptList.Items.RemoveAt(i);
                        }
                    }
                    ScriptList.Items.Refresh();
                });
            };
            watcher.Renamed += (s, e) =>
            {
                Dispatcher.Invoke(() =>
                {
                    string itemRemove = Path.GetFileNameWithoutExtension(e.OldFullPath);
                    Debug.WriteLine(itemRemove);
                    for (int i = ScriptList.Items.Count - 1; i >= 0; --i)
                    {
                        if (((ListBoxItem)ScriptList.Items[i]).Content.ToString() == itemRemove)
                        {
                            ScriptList.Items.RemoveAt(i);
                        }
                    }
                    ScriptList.Items.Add(CreateScriptItem(Path.GetFileNameWithoutExtension(e.FullPath)));
                    ScriptList.Items.Refresh();
                });
            };
        }

        public UIElement Tabs_GetContent(string text)
        {
            return new AceEditor(text);
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private async void ExecuteButton_Click(object sender, RoutedEventArgs e)
        {
            Editor editor = (Editor)Tabs.SelectedContent;
            string script = await editor.GetText();
            Celery.Execute(script);
        }

        private void ScriptItem_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            Tabs.MakeTab(File.ReadAllText(Path.Combine(Config.ScriptsPath, ((ListBoxItem)sender).Content.ToString() + ".lua")), ((ListBoxItem)sender).Content.ToString());
        }

        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (Tabs.SelectedContent == null)
                return;

            SaveFileDialog sfd = new SaveFileDialog
            {
                FileName = "Script",
                DefaultExt = ".lua",
                Filter = "Lua Files|*.lua",
                InitialDirectory = Config.ScriptsPath
            };

            if (sfd.ShowDialog() == true)
            {
                string filename = sfd.FileName;
                File.WriteAllText(filename, await ((Editor)Tabs.SelectedContent).GetText());
            }
        }

        private void OpenButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog
            {
                DefaultExt = ".lua",
                Filter = "Lua Files|*.lua",
                InitialDirectory = Config.ScriptsPath
            };

            if (ofd.ShowDialog() == true)
            {
                string filename = ofd.FileName;
                Tabs.MakeTab(File.ReadAllText(filename), Path.GetFileNameWithoutExtension(filename));
            }
        }

        public TextBox Console
        {
            get
            {
                return OutputBox;
            }
        }

        private void InjectButton_Click(object sender, RoutedEventArgs e)
        {
            Celery.Inject();
        }

        private bool SettingsVisible = false;

        private void ToggleSettings_Click(object sender, RoutedEventArgs e)
        {
            SettingsVisible = !SettingsVisible;
            if (SettingsVisible)
                AnimationUtils.AnimateWidth(SettingsMenu, 0, 150, AnimationUtils.EaseInOut);
            else
                AnimationUtils.AnimateWidth(SettingsMenu, 150, 0, AnimationUtils.EaseInOut);
        }
    }

}
