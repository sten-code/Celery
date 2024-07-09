using System;
using System.Threading.Tasks;
using Celery.Core;
using Celery.ViewModel;
using Microsoft.Extensions.DependencyInjection;

namespace Celery.Services;

public enum ErrorLevel
{
    Info,
    Warning,
    Error
}

public interface ILoggerService
{
    void Log(string message, ErrorLevel errorLevel);
    void Info(string message);
    void Warning(string message);
    void Error(string message);
}

public class LoggerService : ObservableObject, ILoggerService
{
    private ConsoleViewModel ConsoleViewModel { get; }

    public LoggerService(ConsoleViewModel consoleViewModel)
    {
        ConsoleViewModel = consoleViewModel;
    }

    public void Log(string message, ErrorLevel errorLevel)
    {
        Task.Run(() =>
        {
            App.ServiceProvider.GetRequiredService<MainWindow>().Dispatcher.Invoke(() =>
            {
                ConsoleViewModel.AddMessage(message, errorLevel);
            });
        });
    }

    public void Info(string message)
    {
        Log(message, ErrorLevel.Info);
    }

    public void Warning(string message)
    {
        Log(message, ErrorLevel.Warning);
    }

    public void Error(string message)
    {
        Log(message, ErrorLevel.Error);
    }
}