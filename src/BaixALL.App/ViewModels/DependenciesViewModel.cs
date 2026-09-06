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
    private bool _areAllInstalled;

    [ObservableProperty]
    private bool _hasError;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    public event EventHandler? AllReady;

    public DependenciesViewModel(IDependencyManager dependencyManager, ILoggerService? logger = null)
    {
        _dependencyManager = dependencyManager;
        _logger = logger;
        Dependencies = _dependencyManager.GetDependencies();
    }

    public async Task CheckAsync()
    {
        IsBusy = true;
        HasError = false;
        StatusText = "Verificando dependências locais...";

        try
        {
            AreAllInstalled = await _dependencyManager.CheckDependenciesAsync();
            Dependencies = _dependencyManager.GetDependencies();

            if (AreAllInstalled)
            {
                StatusText = "Todas as ferramentas estão prontas para uso.";
                AllReady?.Invoke(this, EventArgs.Empty);
            }
            else
            {
                var missing = Dependencies.Where(d => !d.IsInstalled).Select(d => d.Name).ToList();
                StatusText = $"Componentes ausentes: {string.Join(", ", missing)}. Clique em 'Instalar' para obter automaticamente.";
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
    public async Task InstallMissingAsync()
    {
        if (IsBusy) return;

        IsBusy = true;
        HasError = false;
        OverallProgress = 0;
        StatusText = "Iniciando download e configuração das dependências oficiais...";

        var progress = new Progress<(string ToolName, double Progress, string Status)>(report =>
        {
            StatusText = $"[{report.ToolName}] {report.Status}";
            OverallProgress = Math.Clamp(report.Progress, 0, 100);

            var dep = Dependencies.FirstOrDefault(d => d.Name.Equals(report.ToolName, StringComparison.OrdinalIgnoreCase));
            if (dep != null)
            {
                dep.DownloadProgress = report.Progress;
                dep.StatusText = report.Status;
                dep.IsDownloading = report.Progress > 0 && report.Progress < 100;
            }
        });

        try
        {
            var success = await _dependencyManager.EnsureAllDependenciesAsync(progress);
            Dependencies = _dependencyManager.GetDependencies();
            AreAllInstalled = success;

            if (success)
            {
                StatusText = "Todas as dependências foram configuradas com sucesso!";
                OverallProgress = 100;
                AllReady?.Invoke(this, EventArgs.Empty);
            }
            else
            {
                HasError = true;
                ErrorMessage = "Não foi possível concluir o download de todas as ferramentas. Verifique sua internet.";
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
}
