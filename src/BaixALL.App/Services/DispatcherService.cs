using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace BaixALL.App.Services;

public class DispatcherService : IDispatcherService
{
    private static bool IsDispatcherActive(Dispatcher? dispatcher)
    {
        if (dispatcher == null) return false;
        if (dispatcher.HasShutdownStarted || dispatcher.HasShutdownFinished) return false;
        try
        {
            if (!dispatcher.Thread.IsAlive) return false;
        }
        catch
        {
            return false;
        }
        return true;
    }

    public void Invoke(Action action)
    {
        var dispatcher = Application.Current?.Dispatcher;
        if (!IsDispatcherActive(dispatcher) || dispatcher!.CheckAccess())
        {
            action();
        }
        else
        {
            try
            {
                dispatcher.Invoke(action);
            }
            catch (Exception ex) when (ex is TaskCanceledException or OperationCanceledException)
            {
                action();
            }
        }
    }

    public async Task InvokeAsync(Action action)
    {
        var dispatcher = Application.Current?.Dispatcher;
        if (!IsDispatcherActive(dispatcher) || dispatcher!.CheckAccess())
        {
            action();
        }
        else
        {
            try
            {
                await dispatcher.InvokeAsync(action);
            }
            catch (Exception ex) when (ex is TaskCanceledException or OperationCanceledException)
            {
                action();
            }
        }
    }

    public async Task<T> InvokeAsync<T>(Func<T> func)
    {
        var dispatcher = Application.Current?.Dispatcher;
        if (!IsDispatcherActive(dispatcher) || dispatcher!.CheckAccess())
        {
            return func();
        }
        else
        {
            try
            {
                return await dispatcher.InvokeAsync(func);
            }
            catch (Exception ex) when (ex is TaskCanceledException or OperationCanceledException)
            {
                return func();
            }
        }
    }

    public bool CheckAccess()
    {
        var dispatcher = Application.Current?.Dispatcher;
        return !IsDispatcherActive(dispatcher) || dispatcher!.CheckAccess();
    }
}
