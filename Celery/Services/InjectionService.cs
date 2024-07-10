using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Celery.Core;

namespace Celery.Services;

public enum InjectionResult
{
    FAILED,
    CANCELED,
    ALREADY_INJECTING,
    ALREADY_INJECTED,
    ROBLOX_NOT_OPENED,
    SUCCESS
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

    public InjectionService(ILoggerService loggerService)
    {
        LoggerService = loggerService;
    }

    public async Task<InjectionResult> Inject()
    {
        if (!File.Exists(Config.InjectorPath))
        {
            LoggerService.Error("Couldn't find 'CeleryInject.exe'");
            return InjectionResult.FAILED;
        }
        
        if (!IsRobloxOpen())
            return InjectionResult.ROBLOX_NOT_OPENED;
        
        if (_isInjectingMainPlayer)
            return InjectionResult.ALREADY_INJECTING;

        if (IsInjected())
            return InjectionResult.ALREADY_INJECTED;

        _isInjectingMainPlayer = true;

        if (_injectorProc != null && !_injectorProc.HasExited)
            _injectorProc.Kill();

        TaskCompletionSource<InjectionResult> tcs = new();
        
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
            tcs.TrySetResult(InjectionResult.CANCELED);
            _isInjected = false;
            _isInjectingMainPlayer = false;
            _statusCallback?.Invoke(false);
        };
        
        _injectorProc.OutputDataReceived += (_, e) =>
        {
            if (e.Data == null)
                return;

            string message = e.Data.Trim();
            Console.WriteLine(message);
            LoggerService.Info(message);

            if (message == "READY")
            {
                tcs.TrySetResult(InjectionResult.SUCCESS);
                _isInjected = true;
                _isInjectingMainPlayer = false;
                _statusCallback?.Invoke(true);
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
        _statusCallback = callback;
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