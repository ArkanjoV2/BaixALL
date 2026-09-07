using System;
using System.Threading;
using BaixALL.App.Services;
using BaixALL.App.ViewModels;
using BaixALL.App.Views;
using Xunit;

namespace BaixALL.Tests;

public class MainWindowInstantiationRegressionTests
{
    [Fact]
    public void MainWindow_InstantiatesSuccessfully_WithEmbeddedIconAndNoXamlExceptions()
    {
        Exception? caughtException = null;
        bool instantiated = false;
        string? title = null;
        object? icon = null;

        var thread = new Thread(() =>
        {
            try
            {
                if (System.Windows.Application.Current == null)
                {
                    var app = new System.Windows.Application();
                    app.Resources.MergedDictionaries.Add(new System.Windows.ResourceDictionary
                    {
                        Source = new Uri("pack://application:,,,/BaixALL;component/Resources/Themes/DarkTheme.xaml", UriKind.Absolute)
                    });
                    app.Resources.MergedDictionaries.Add(new System.Windows.ResourceDictionary
                    {
                        Source = new Uri("pack://application:,,,/BaixALL;component/Resources/Icons.xaml", UriKind.Absolute)
                    });
                    app.Resources.MergedDictionaries.Add(new System.Windows.ResourceDictionary
                    {
                        Source = new Uri("pack://application:,,,/BaixALL;component/Resources/Styles.xaml", UriKind.Absolute)
                    });
                }

                var settings = new SettingsService();
                var history = new HistoryService();
                var depManager = new DependencyManager();
                var ytDlp = new YtDlpService(depManager);
                var formatService = new FormatSelectionService();
                var ytService = new YoutubeService(ytDlp, formatService);
                var dispatcher = new DispatcherService();
                var downloadService = new DownloadService(ytDlp, history, settings, dispatcher);
                var updateService = new UpdateService(depManager, downloadService);

                var vm = new MainViewModel(
                    ytService,
                    formatService,
                    downloadService,
                    settings,
                    depManager,
                    updateService,
                    history,
                    dispatcher);

                MainWindow? window = null;
                try
                {
                    window = new MainWindow(vm);
                    instantiated = true;
                    title = window.Title;
                    icon = window.Icon;
                }
                finally
                {
                    window?.Close();
                    System.Windows.Threading.Dispatcher.CurrentDispatcher.InvokeShutdown();
                }
            }
            catch (Exception ex)
            {
                caughtException = ex;
            }
        });

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();

        Assert.Null(caughtException);
        Assert.True(instantiated, "MainWindow should be instantiated successfully.");
        Assert.NotNull(title);
        Assert.NotNull(icon);
    }
}
