using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BaixALL.App.Models;
using BaixALL.App.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace BaixALL.App.ViewModels;

public partial class DependenciesViewModel : ObservableObject
{
    private readonly IDependencyManager _dependencyManager;
    private readonly IUpdateService? _updateService;
    private readonly IDownloadService? _downloadService;
    private readonly ILoggerService? _logger;

    [ObservableProperty]
    private IReadOnlyList<DependencyItem> _dependencies = new List<DependencyItem>();

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private double _overallProgress;

    [ObservableProperty]
    private string _statusText = "Verificando componentes necessários...";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(MainActionText))]
    private bool _areAllInstalled;

    [ObservableProperty]
    private bool _hasError;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    public string MainActionText => AreAllInstalled
        ? "Continuar para o BaixALL"
        : "Instalar componentes necessários";

    public event EventHandler? AllReady;
    public event EventHandler? CloseRequested;

    public DependenciesViewModel(
        IDependencyManager dependencyManager,
        IUpdateService? updateService = null,
        IDownloadService? downloadService = null,
        ILoggerService? logger = null)
    {
        _dependencyManager = dependencyManager;
        _updateService = updateService;
        _downloadService = downloadService;
        _logger = logger;
        Dependencies = _dependencyManager.GetDependencies();
        AreAllInstalled = _dependencyManager.AreAllDependenciesInstalled();
        if (AreAllInstalled)
        {
            StatusText = "Todas as ferramentas estão prontas.";
        }
    }

    [RelayCommand]
    public async Task CheckAllAsync() => await CheckAsync();

    [RelayCommand]
    public async Task CheckAsync()
    {
        IsBusy = true;
        HasError = false;
        StatusText = "Verificando dependências locais...";

        try
        {
            AreAllInstalled = await _dependencyManager.CheckDependenciesAsync().ConfigureAwait(false);
            Dependencies = _dependencyManager.GetDependencies();

            if (AreAllInstalled)
            {
                StatusText = "Todas as ferramentas estão prontas.";
                AllReady?.Invoke(this, EventArgs.Empty);
            }
            else
            {
                var missing = Dependencies.Where(d => !d.IsInstalled).Select(d => d.Name).ToList();
                StatusText = $"Componentes ausentes: {string.Join(", ", missing)}. Clique abaixo para instalar automaticamente.";
            }
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"Falha ao verificar ferramentas: {ex.Message}";
            _logger?.Error("Erro ao verificar dependências.", ex);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    public async Task CheckUpdatesAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        StatusText = "Verificando atualizações online para as ferramentas...";

        try
        {
            if (_updateService != null)
            {
                var ytDlpRes = await _updateService.CheckYtDlpUpdateAsync().ConfigureAwait(false);
                var denoRes = await _updateService.CheckDenoUpdateAsync().ConfigureAwait(false);

                var deps = _dependencyManager.GetDependencies();
                var ytDep = deps.FirstOrDefault(d => d.Name.Equals("yt-dlp", StringComparison.OrdinalIgnoreCase));
                if (ytDep != null && ytDlpRes.HasUpdate)
                {
                    ytDep.State = DependencyState.UpdateAvailable;
                    ytDep.StatusText = $"Atualização disponível: {ytDlpRes.LatestVersion}";
                }

                var denoDep = deps.FirstOrDefault(d => d.Name.Equals("Deno", StringComparison.OrdinalIgnoreCase));
                if (denoDep != null && denoRes.HasUpdate)
                {
                    denoDep.State = DependencyState.UpdateAvailable;
                    denoDep.StatusText = $"Atualização disponível: {denoRes.LatestVersion}";
                }

                Dependencies = deps;

                if (ytDlpRes.HasUpdate || denoRes.HasUpdate)
                {
                    StatusText = "Atualizações encontradas para os componentes do BaixALL.";
                }
                else
                {
                    StatusText = "Todas as ferramentas estão na versão mais recente.";
                }
            }
            else
            {
                await CheckAsync().ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            _logger?.Warning($"Erro ao verificar atualizações: {ex.Message}");
            StatusText = "Não foi possível verificar atualizações online.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    public async Task ExecuteMainActionAsync()
    {
        if (AreAllInstalled)
        {
            CloseRequested?.Invoke(this, EventArgs.Empty);
            return;
        }

        await InstallMissingAsync();
    }

    [RelayCommand]
    public async Task InstallMissingAsync()
    {
        if (IsBusy) return;

        if (_downloadService?.HasActiveDownloads == true)
        {
            HasError = true;
            ErrorMessage = "Não é possível atualizar ferramentas enquanto houver downloads em andamento.";
            StatusText = "Operação bloqueada: downloads ativos na fila.";
            return;
        }

        IsBusy = true;
        HasError = false;
        OverallProgress = 0;
        StatusText = "Iniciando download e configuração automática...";

        var progress = new Progress<(string ToolName, double Progress, string Status)>(report =>
        {
            StatusText = $"[{report.ToolName}] {report.Status}";
            OverallProgress = Math.Clamp(report.Progress, 0, 100);
        });

        try
        {
            var success = await _dependencyManager.EnsureAllDependenciesAsync(progress).ConfigureAwait(false);
            Dependencies = _dependencyManager.GetDependencies();
            AreAllInstalled = success;

            if (success)
            {
                StatusText = "Todas as ferramentas foram instaladas e validadas com sucesso!";
                OverallProgress = 100;
                AllReady?.Invoke(this, EventArgs.Empty);
            }
            else
            {
                HasError = true;
                ErrorMessage = "Não foi possível concluir a instalação de todas as ferramentas. Tente novamente.";
            }
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"Erro durante instalação: {ex.Message}";
            _logger?.Error("Erro ao baixar dependências.", ex);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    public async Task InstallToolAsync(string? toolName)
    {
        if (string.IsNullOrWhiteSpace(toolName) || IsBusy) return;

        if (_downloadService?.HasActiveDownloads == true)
        {
            HasError = true;
            ErrorMessage = "Não é possível atualizar ferramentas enquanto houver downloads em andamento.";
            StatusText = "Operação bloqueada: downloads ativos na fila.";
            return;
        }

        IsBusy = true;
        HasError = false;
        StatusText = $"Instalando {toolName}...";

        var progress = new Progress<(string ToolName, double Progress, string Status)>(report =>
        {
            StatusText = $"[{report.ToolName}] {report.Status}";
            OverallProgress = Math.Clamp(report.Progress, 0, 100);
        });

        try
        {
            var success = await _dependencyManager.InstallDependencyByNameAsync(toolName, progress).ConfigureAwait(false);
            Dependencies = _dependencyManager.GetDependencies();
            AreAllInstalled = _dependencyManager.AreAllDependenciesInstalled();

            if (success)
            {
                StatusText = $"{toolName} instalado e validado com sucesso!";
                if (AreAllInstalled)
                {
                    AllReady?.Invoke(this, EventArgs.Empty);
                }
            }
            else
            {
                HasError = true;
                ErrorMessage = $"Não foi possível instalar {toolName}. Tente novamente.";
            }
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = $"Erro ao instalar {toolName}: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    public async Task ReinstallToolAsync(string? toolName) => await InstallToolAsync(toolName);

    [RelayCommand]
    public async Task UpdateToolAsync(string? toolName) => await InstallToolAsync(toolName);

    [RelayCommand]
    public async Task VerifyToolAsync(string? toolName)
    {
        if (string.IsNullOrWhiteSpace(toolName) || IsBusy) return;

        IsBusy = true;
        StatusText = $"Validando {toolName}...";

        try
        {
            var success = await _dependencyManager.ValidateDependencyAsync(toolName).ConfigureAwait(false);
            Dependencies = _dependencyManager.GetDependencies();
            AreAllInstalled = _dependencyManager.AreAllDependenciesInstalled();

            StatusText = success
                ? $"{toolName} validado com sucesso."
                : $"Falha ao validar {toolName}.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    public void CloseSetup()
    {
        CloseRequested?.Invoke(this, EventArgs.Empty);
    }
}
