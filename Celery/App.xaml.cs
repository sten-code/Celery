using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading;
using Celery.ViewModel;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using CefSharp;
using CefSharp.Wpf;
using Celery.Controls;
using Celery.Services;
using Celery.Settings;

namespace Celery;

public partial class App
{
    public static ServiceProvider ServiceProvider;
    public static int MonacoPort;
    public static Process LspProcess;

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);
        
    static Process GetProcess(string exePath)
    {
        string exeName = Path.GetFileNameWithoutExtension(exePath);
        foreach (Process process in Process.GetProcessesByName(exeName))
        {
            try
            {
                if (process.MainModule != null && process.MainModule.FileName.Equals(exePath, StringComparison.OrdinalIgnoreCase))
                    return process;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error accessing process: {ex.Message}");
            }
        }

        return null;
    }

    private string GetContentType(string filename)
    {
        string extension = Path.GetExtension(filename).ToLowerInvariant();
        return extension switch
        {
            ".html" => "text/html",
            ".htm" => "text/html",
            ".css" => "text/css",
            ".js" => "application/javascript",
            ".json" => "application/json",
            ".png" => "image/png",
            ".jpg" => "image/jpeg",
            ".jpeg" => "image/jpeg",
            ".gif" => "image/gif",
            ".svg" => "image/svg+xml",
            ".ico" => "image/x-icon",
            _ => "application/octet-stream",
        };
    }

    public int FindFreePort()
    {
        TcpListener l = new(IPAddress.Loopback, 0);
        l.Start();
        int port = ((IPEndPoint)l.LocalEndpoint).Port;
        l.Stop();
        return port;
    }

    public void ExtractZip(byte[] zip, string path)
    {
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);

            using MemoryStream stream = new(zip);
            using ZipArchive archive = new(stream);
            foreach (ZipArchiveEntry entry in archive.Entries)
            {
                string fileName = Path.Combine(path, entry.FullName);
                if (entry.Name == "")
                {
                    Directory.CreateDirectory(fileName);
                    continue;
                }

                try
                {
                    entry.ExtractToFile(fileName, true);
                }
                catch { }
            }
        }
    }

    public App()
    {
        Cef.Initialize(new CefSettings
        {
            CachePath = Path.Combine(Config.ApplicationPath, "cache")
        });

        IServiceCollection services = new ServiceCollection();

        // View Models
        services.AddSingleton<ExplorerViewModel>();
        services.AddSingleton<ConsoleViewModel>();
        services.AddSingleton<MainViewModel>();
        services.AddSingleton<SettingsViewModel>();

        // Services
        services.AddSingleton<IInjectionService, InjectionService>();
        services.AddSingleton<ILoggerService, LoggerService>();
        services.AddSingleton<ISettingsService, SettingsService>();
        services.AddSingleton<ITabSavingService, TabSavingService>();
        services.AddSingleton<IThemeService, ThemeService>();
        services.AddSingleton<IUpdateService, UpdateService>();

        // Views
        services.AddSingleton(provider => new MainWindow(
            provider.GetRequiredService<ExplorerViewModel>(),
            provider.GetRequiredService<ConsoleViewModel>(),
            provider.GetRequiredService<SettingsViewModel>(),
            provider.GetRequiredService<ISettingsService>())
        {
            DataContext = provider.GetRequiredService<MainViewModel>()
        });
        services.AddSingleton<TabsHost>();

        // Other
        services.AddSingleton<ObservableCollection<Setting>>();

        ServiceProvider = services.BuildServiceProvider();
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // AppData path
        if (!Directory.Exists(Config.CeleryAppDataPath))
            Directory.CreateDirectory(Config.CeleryAppDataPath);
        if (!Directory.Exists(Config.ThemesPath))
            Directory.CreateDirectory(Config.ThemesPath);
        if (!Directory.Exists(Config.TabsPath))
            Directory.CreateDirectory(Config.TabsPath);

        // Local path
        if (!Directory.Exists(Config.ScriptsPath))
            Directory.CreateDirectory(Config.ScriptsPath);
        if (!Directory.Exists(Config.BinPath))
            Directory.CreateDirectory(Config.BinPath);
        ExtractZip(Config.Ace, Config.AcePath);
        ExtractZip(Config.Monaco, Config.MonacoPath);
        ExtractZip(Config.Lsp, Config.LspPath);
        
        // Temp path
        if (!Directory.Exists(Config.CeleryTempPath))
            Directory.CreateDirectory(Config.CeleryTempPath);
        File.WriteAllText(Config.CeleryHomeFile, Config.ApplicationPath);
        
        // Start the web server
        ThreadPool.QueueUserWorkItem(_ =>
        {
            HttpListener listener = new();
            MonacoPort = FindFreePort();
            listener.Prefixes.Add($"http://localhost:{MonacoPort}/");
            listener.Start();
            Console.WriteLine($"Listening on port {MonacoPort}");

            while (true)
            {
                HttpListenerContext context = listener.GetContext();
                HttpListenerResponse response = context.Response;

                string[] segments =
                [
                    Config.MonacoPath
                ];
                segments = segments.Concat(context.Request.Url.Segments.Skip(1)).ToArray();
                string path = Path.Combine(segments);
                if (Directory.Exists(path))
                    path = Path.Combine(path, "index.html");

                if (File.Exists(path))
                {
                    byte[] html = File.ReadAllBytes(path);
                    response.ContentType = GetContentType(path);
                    response.ContentLength64 = html.Length;
                    response.OutputStream.Write(html, 0, html.Length);
                }
                else
                {
                    response.StatusCode = (int)HttpStatusCode.NotFound;
                }

                response.Close();
            }
        });

        IInjectionService injectionService = ServiceProvider.GetRequiredService<IInjectionService>();
        ISettingsService settingsService = ServiceProvider.GetRequiredService<ISettingsService>();

        ThreadPool.QueueUserWorkItem(_ =>
        {
            List<int> openedProcs = [];
            while (true)
            {
                foreach (Process proc in Process.GetProcessesByName("RobloxPlayerBeta"))
                {
                    if (openedProcs.Contains(proc.Id))
                        continue;
                        
                    bool autoInject = settingsService.GetSetting<bool>("autoinject");
                    if (autoInject)
                    {
                        ThreadPool.QueueUserWorkItem(_ =>
                        {
                            int tries = 0;
                            while (FindWindow(null, "Roblox") == IntPtr.Zero)
                            {
                                Thread.Sleep(1000);
                                tries++;
                                if (tries > 30)
                                    break;
                            }

                            if (tries <= 30)
                                injectionService.Inject();
                        });
                    }
                        
                    openedProcs.Add(proc.Id);
                        
                    ThreadPool.QueueUserWorkItem(_ =>
                    {
                        Process.GetProcessById(proc.Id).WaitForExit();
                        injectionService.GetStatusCallback()?.Invoke(false);
                        openedProcs.Remove(proc.Id);
                    });
                }
                Thread.Sleep(1000);
            }
        });

        // Ensure that the constructor runs immediately
        ServiceProvider.GetRequiredService<IThemeService>();

        MainWindow mainWindow = ServiceProvider.GetRequiredService<MainWindow>();

        ServiceProvider.GetRequiredService<IUpdateService>().CheckUpdate();

        // Start the lsp
        string lspPath = Path.Combine(Config.LspPath, "main.exe");
        LspProcess = GetProcess(lspPath);
        if (LspProcess == null)
        {
            LspProcess = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = lspPath,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden,
                    WorkingDirectory = Config.LspPath
                }
            };
            LspProcess.Start();
        }
        else
        {
            Console.WriteLine("LSP Instance already exists");
        }
            
        // Main startup
        mainWindow.Loaded += (_, _) =>
        {
            // Load the tabs
            ServiceProvider.GetRequiredService<ITabSavingService>().Load();
        };
        mainWindow.Show();
    }

    public async new static void Exit()
    {
        // Save the tabs before exiting
        await ServiceProvider.GetRequiredService<ITabSavingService>().Save();
        if (!LspProcess.HasExited)
            LspProcess.Kill();
        Current.Shutdown();
    }
}