using System;
using System.Windows;
using BaixALL.App.Services;
using BaixALL.App.ViewModels;
using BaixALL.App.Views;

namespace BaixALL.App;

public partial class App : Application
{
    private ILoggerService? _logger;
    private IDownloadService? _downloadService;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Inicializa Logger e captura exceções não tratadas
        _logger = new LoggerService();
        _logger.Info("BaixALL iniciando...");

        AppDomain.CurrentDomain.UnhandledException += (s, args) =>
        {
            if (args.ExceptionObject is Exception ex)
            {
                _logger.Error("Exceção não tratada no AppDomain.", ex);
            }
        };

        DispatcherUnhandledException += (s, args) =>
        {
            _logger.Error("Exceção não tratada na UI Thread (Dispatcher).", args.Exception);
            var logPath = _logger?.GetLogFolderPath() ?? Infrastructure.AppConstants.LogsFolder;
            MessageBox.Show(
                $"Ocorreu um erro inesperado: {args.Exception.Message}\n\nPara detalhes técnicos, consulte os arquivos de log em:\n{logPath}",
                "BaixALL - Aviso",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            args.Handled = true;
        };

        try
        {
            // Composição de Serviços (Injeção de Dependência)
            var dispatcherService = new DispatcherService();
            var settingsService = new SettingsService(_logger);
            var historyService = new HistoryService(_logger);
            var dependencyManager = new DependencyManager(_logger);
            var ytDlpService = new YtDlpService(dependencyManager, _logger);
            var formatSelectionService = new FormatSelectionService();
            var youtubeService = new YoutubeService(ytDlpService, formatSelectionService, _logger);
            var downloadService = new DownloadService(ytDlpService, historyService, settingsService, dispatcherService, _logger);
            _downloadService = downloadService;
            var updateService = new UpdateService(dependencyManager, downloadService, _logger);

            var mainViewModel = new MainViewModel(
                youtubeService,
                formatSelectionService,
                downloadService,
                settingsService,
                dependencyManager,
                updateService,
                historyService,
                dispatcherService,
                _logger);

            var mainWindow = new MainWindow(mainViewModel);
            MainWindow = mainWindow;
            mainWindow.Show();
        }
        catch (Exception ex)
        {
            _logger.Error("Falha crítica ao inicializar aplicação.", ex);
            var logPath = _logger?.GetLogFolderPath() ?? Infrastructure.AppConstants.LogsFolder;
            MessageBox.Show(
                $"Não foi possível iniciar o BaixALL.\n\nDetalhes: {ex.Message}\n\nPara mais informações, consulte os arquivos de log em:\n{logPath}",
                "BaixALL - Erro Fatal",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            Shutdown(1);
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        try
        {
            _downloadService?.CancelAllDownloads();
        }
        catch { }
        base.OnExit(e);
    }
}
