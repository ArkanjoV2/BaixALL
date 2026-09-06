using System;
using System.Threading.Tasks;
using System.Windows;

namespace BaixALL.App.Services;

public class DispatcherService : IDispatcherService
{
    public void Invoke(Action action)
    {
        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher == null || dispatcher.CheckAccess())
        {
            action();
        }
        else
        {
            dispatcher.Invoke(action);
        }
    }

    public async Task InvokeAsync(Action action)
    {
        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher == null || dispatcher.CheckAccess())
        {
            action();
        }
        else
        {
            await dispatcher.InvokeAsync(action);
        }
    }

    public async Task<T> InvokeAsync<T>(Func<T> func)
    {
        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher == null || dispatcher.CheckAccess())
        {
            return func();
        }
        else
        {
            return await dispatcher.InvokeAsync(func);
        }
    }

    public bool CheckAccess()
    {
        var dispatcher = Application.Current?.Dispatcher;
        return dispatcher == null || dispatcher.CheckAccess();
    }
}
