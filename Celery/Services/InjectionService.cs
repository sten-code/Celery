using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Celery.Core;

namespace Celery.Services;

public enum InjectionResult
{
    Failed,
    Canceled,
    AlreadyInjecting,
    RobloxNotOpened,
    Success
}

public interface IInjectionService
{
    Task<InjectionResult> Inject();
    void Execute(string script);
    void SetStatusCallback(Action<bool> callback);
    Action<bool> GetStatusCallback();
    bool IsInjected();
}

public class InjectionService : ObservableObject, IInjectionService
{
    private Process _injectorProc;
    private bool _isInjectingMainPlayer;
    private bool _isInjected;
    private Action<bool> _statusCallback;

    private ILoggerService LoggerService { get; }
    private ISettingsService SettingsService { get; }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

    public InjectionService(ILoggerService loggerService, ISettingsService settingsService)
    {
        LoggerService = loggerService;
        SettingsService = settingsService;
        
        Process[] processes = Process.GetProcessesByName("CeleryInject");
        if (processes.Length == 0)
            return;
        
        if (processes.Length > 1)
        {
            foreach (Process proc in processes.Skip(1))
            {
                try
                {
                    proc.Kill();
                }
                catch (Exception e)
                {
                    LoggerService.Error($"Couldn't kill injector: {e.Message}");
                }
            }
        }

        LoggerService.Info("Found existing injector");
        _injectorProc = processes[0];
        _isInjected = true;
        _isInjectingMainPlayer = false;
    }

    public async Task<InjectionResult> Inject()
    {
        if (!File.Exists(Config.InjectorPath))
        {
            LoggerService.Error("Couldn't find 'CeleryInject.exe'");
            return InjectionResult.Failed;
        }

        try
        {
            if (_injectorProc != null && !_injectorProc.HasExited)
                _injectorProc.Kill();
        }
        catch { }

        foreach (Process process in Process.GetProcessesByName("CeleryInject"))
        {
            try
            {
                process.Kill();
            }
            catch (Exception e)
            {
                LoggerService.Error($"Couldn't kill injector: {e.Message}");
            }
        }
        _statusCallback?.Invoke(false);

        if (!IsRobloxOpen())
            return InjectionResult.RobloxNotOpened;

        int tries = 1;
        while (FindWindow(null, "Roblox") == IntPtr.Zero)
        {
            LoggerService.Info($"[{tries}/30] Waiting for Roblox to start...");
            await Task.Delay(1000);
            tries++;
            if (tries > 30)
            {
                LoggerService.Error("Took too long for Roblox to start, aborting...");
                return InjectionResult.Failed;
            }
        }

        TaskCompletionSource<InjectionResult> tcs = new();

        _isInjectingMainPlayer = true;
        _injectorProc = new Process
        {
            StartInfo = new ProcessStartInfo()
            {
                FileName = Config.InjectorPath,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            }
        };

        _injectorProc.Exited += (_, _) =>
        {
            tcs.TrySetResult(InjectionResult.Canceled);
            _isInjected = false;
            _isInjectingMainPlayer = false;
            _statusCallback?.Invoke(false);
        };

        int scanningCount = 0;
        _injectorProc.OutputDataReceived += (_, e) =>
        {
            if (e.Data == null)
                return;

            string message = e.Data.Trim();
            Console.WriteLine(message);
            LoggerService.Info(message);

            switch (message)
            {
                case "READY":
                    tcs.TrySetResult(InjectionResult.Success);
                    _isInjected = true;
                    _isInjectingMainPlayer = false;
                    _statusCallback?.Invoke(true);
                    break;
                case "Scanning...":
                    if (!SettingsService.GetSetting<bool>("autofixerrors"))
                        break;

                    scanningCount++;
                    if (scanningCount >= 10)
                    {
                        LoggerService.Error("Detected an error, force closing Roblox and Celery injector...");
                        foreach (Process proc in Process.GetProcessesByName("RobloxPlayerBeta"))
                        {
                            try
                            {
                                proc.Kill();
                            }
                            catch (Exception ex)
                            {
                                LoggerService.Error($"Couldn't kill Roblox: {ex.Message}");
                            }
                        }
                        foreach (Process process in Process.GetProcessesByName("CeleryInject"))
                        {
                            try
                            {
                                process.Kill();
                            }
                            catch (Exception ex)
                            {
                                LoggerService.Error($"Couldn't kill injector: {ex.Message}");
                            }
                        }
                        tcs.TrySetResult(InjectionResult.Failed);
                        _isInjected = false;
                        _isInjectingMainPlayer = false;
                        _statusCallback?.Invoke(false);
                        _injectorProc?.Dispose();
                    }
                    break;
                case "No window":
                    tcs.TrySetResult(InjectionResult.Failed);
                    _isInjected = false;
                    _isInjectingMainPlayer = false;
                    _statusCallback?.Invoke(false);
                    break;
            }
        };

        _injectorProc.Start();
        _injectorProc.BeginOutputReadLine();

        return await tcs.Task;
    }

    public void Execute(string script)
    {
        if (!IsInjected())
        {
            LoggerService.Error("You must inject into Roblox first.");
            return;
        }

        File.WriteAllText(Config.CeleryScriptFile, script);
    }

    public void SetStatusCallback(Action<bool> callback)
    {
        _statusCallback = injected =>
        {
            if (!injected)
            {
                _isInjectingMainPlayer = false;
                _isInjected = false;
            }
            callback(injected);
        };
        
        _statusCallback?.Invoke(IsInjected());
    }

    public Action<bool> GetStatusCallback()
    {
        return _statusCallback;
    }

    public bool IsInjected()
    {
        if (_injectorProc == null)
            return false;

        if (!IsRobloxOpen())
            return false;

        return !_injectorProc.HasExited && _isInjected;
    }

    private static bool IsRobloxOpen()
    {
        return Process.GetProcessesByName("RobloxPlayerBeta").Length > 0;
    }
}