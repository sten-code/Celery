using System;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using CefSharp;
using CefSharp.Wpf;
using Celery.Controls;
using Celery.Core;
using Celery.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Win32;

namespace Celery.ViewModel
{
    public class MainViewModel : Core.ViewModel
    {
        #region Properties

        private WindowState _windowState;

        public WindowState WindowState
        {
            get => _windowState;
            set
            {
                _windowState = value;
                OnPropertyChanged();
            }
        }

        private Brush _statusBrush;

        public Brush StatusBrush
        {
            get => _statusBrush;
            set
            {
                _statusBrush = value;
                OnPropertyChanged();
            }
        }

        private string _statusText;

        public string StatusText
        {
            get => _statusText;
            set
            {
                _statusText = value;
                OnPropertyChanged();
            }
        }

        #endregion

        #region Commands

        public ICommand ExitCommand { get; }
        public ICommand MinimizeCommand { get; }
        public ICommand ToggleFullscreenCommand { get; }
        public ICommand ExecuteCommand { get; }
        public ICommand InjectCommand { get; }
        public ICommand OpenCommand { get; }
        public ICommand SaveCommand { get; }
        public ICommand SaveAsCommand { get; }

        public ICommand UndoCommand { get; }
        public ICommand RedoCommand { get; }
        public ICommand LineCommentCommand { get; }
        public ICommand BlockCommentCommand { get; }
        public ICommand FormatScriptCommand { get; }

        #endregion

        public TabsHost TabsHost { get; }
        public ExplorerViewModel ExplorerViewModel { get; }
        public ConsoleViewModel ConsoleViewModel { get; }
        public SettingsViewModel SettingsViewModel { get; }
        public IInjectionService InjectionService { get; }
        public ILoggerService LoggerService { get; }

        public MainViewModel(TabsHost tabsHost, ExplorerViewModel explorerViewModel, ConsoleViewModel consoleViewModel,
            SettingsViewModel settingsViewModel, IInjectionService injectionService, ILoggerService loggerService)
        {
            TabsHost = tabsHost;
            ExplorerViewModel = explorerViewModel;
            ConsoleViewModel = consoleViewModel;
            SettingsViewModel = settingsViewModel;
            InjectionService = injectionService;
            LoggerService = loggerService;

            ExplorerViewModel.ExecuteScript += (_, e) =>
            {
                InjectionService.Execute(e.Content);
            };
            
            InjectionService.SetStatusCallback((injected) =>
            {
                App.ServiceProvider.GetRequiredService<MainWindow>().Dispatcher.Invoke(() =>
                {
                    if (injected)
                    {
                        StatusBrush = new SolidColorBrush(Colors.LimeGreen);
                        StatusText = "Injected";
                    }
                    else
                    {
                        StatusBrush = new SolidColorBrush(Colors.Red);
                        StatusText = "Not Injected";
                    }
                });
            });

            StatusBrush = new SolidColorBrush(Colors.Red);
            StatusText = "Not Injected";

            ExitCommand = new RelayCommand(o => App.Exit(), o => true);
            MinimizeCommand = new RelayCommand(o => { WindowState = WindowState.Minimized; }, o => true);
            ToggleFullscreenCommand = new RelayCommand(o =>
            {
                if (WindowState == WindowState.Maximized)
                    WindowState = WindowState.Normal;
                else if (WindowState == WindowState.Normal)
                    WindowState = WindowState.Maximized;
            }, o => true);

            ExecuteCommand = new RelayCommand(async o =>
            {
                string script = await TabsHost.FindSelectedEditor().GetText();
                InjectionService.Execute(script);
            }, o => true);

            InjectCommand = new RelayCommand(async o =>
            {
                InjectionResult result = await InjectionService.Inject();
                switch (result)
                {
                    case InjectionResult.FAILED:
                        LoggerService.Error("Injection failed!");
                        break;
                    case InjectionResult.ALREADY_INJECTED:
                        LoggerService.Error("Already injected!");
                        break;
                    case InjectionResult.SUCCESS:
                        LoggerService.Info("Injected successfully!");
                        break;
                    case InjectionResult.ALREADY_INJECTING:
                        LoggerService.Error("Already injecting...");
                        break;
                    case InjectionResult.ROBLOX_NOT_OPENED:
                        LoggerService.Error("Roblox isn't open!");
                        break;
                    case InjectionResult.CANCELED:
                        LoggerService.Error("Canceled the injection process...");
                        break;
                }
            }, o => true);

            OpenCommand = new RelayCommand(o => { OpenScript(); }, o => true);
            SaveCommand = new RelayCommand(o => { SaveScript(); }, o => true);
            SaveAsCommand = new RelayCommand(o =>
            {
                Tab tab = TabsHost.FindSelectedTab();
                if (tab == null)
                    return;

                SaveAs(tab);
            }, o => true);

            UndoCommand = new RelayCommand(o =>
            {
                Tab tab = TabsHost.FindSelectedTab();
                if (tab?.Content is not Editor editor)
                    return;

                editor.EvaluateScriptAsync("editor.undo()");
            }, o => true);

            RedoCommand = new RelayCommand(o =>
            {
                Tab tab = TabsHost.FindSelectedTab();
                if (tab?.Content is not Editor editor)
                    return;

                editor.EvaluateScriptAsync("editor.redo()");
            }, o => true);

            LineCommentCommand = new RelayCommand(o =>
            {
                Tab tab = TabsHost.FindSelectedTab();
                if (tab?.Content is not Editor editor)
                    return;

                editor.EvaluateScriptAsync("editor.toggleCommentLines()");
            }, o => true);

            BlockCommentCommand = new RelayCommand(o =>
            {
                Tab tab = TabsHost.FindSelectedTab();
                if (tab?.Content is not Editor editor)
                    return;

                editor.EvaluateScriptAsync("editor.toggleBlockComment()");
            }, o => true);

            FormatScriptCommand = new RelayCommand(o =>
            {
                Tab tab = TabsHost.FindSelectedTab();
                if (tab?.Content is not Editor editor)
                    return;

                editor.EvaluateScriptAsync("editor.toggleBlockComment()");
            }, o => true);
        }

        private void OpenScript()
        {
            OpenFileDialog ofd = new()
            {
                DefaultExt = ".lua",
                Filter = "Script Files|*.lua;*.txt|All Files|*.*",
                InitialDirectory = Config.ScriptsPath
            };

            if (ofd.ShowDialog() == true)
            {
                string content = File.ReadAllText(ofd.FileName);
                string header = Path.GetFileName(ofd.FileName);
                Tab tab = TabsHost.MakeTab(content, header);
                if (tab == null)
                    return;

                tab.FileName = ofd.FileName;
            }
        }

        private async void SaveScript()
        {
            Tab tab = TabsHost.FindSelectedTab();
            if (tab == null)
                return;

            if (tab.FileName == "")
            {
                SaveAs(tab);
                return;
            }

            if (!(tab.Content is Editor editor))
                return;

            File.WriteAllText(tab.FileName, await editor.GetText());
            tab.IsUnsaved = false;
        }

        private async void SaveAs(Tab tab)
        {
            if (tab.Content is not Editor editor)
                return;

            SaveFileDialog sfd = new()
            {
                FileName = tab.Header.ToString(),
                DefaultExt = ".lua",
                Filter = "Script Files|*.lua;*.txt|All Files|*.*",
                InitialDirectory = Config.ScriptsPath
            };

            if (sfd.ShowDialog() == true)
            {
                File.WriteAllText(sfd.FileName, await editor.GetText());
                tab.Header = Path.GetFileName(sfd.FileName);
                tab.FileName = sfd.FileName;
                tab.IsUnsaved = false;
            }
        }

    }
}