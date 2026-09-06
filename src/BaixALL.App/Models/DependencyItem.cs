using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace BaixALL.App.Models;

public enum DependencyState
{
    NotInstalled,        // Não instalado
    Checking,            // Verificando
    Downloading,         // Baixando
    Installing,          // Instalando
    Validating,          // Validando
    Installed,           // Instalado
    UpdateAvailable,     // Atualização disponível
    Error                // Erro
}

public partial class DependencyItem : ObservableObject
{
    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private string _description = string.Empty;

    [ObservableProperty]
    private string _version = "Não instalado";

    [ObservableProperty]
    private bool _isInstalled;

    [ObservableProperty]
    private DependencyState _state = DependencyState.NotInstalled;

    [ObservableProperty]
    private string _statusText = "Não instalado";

    [ObservableProperty]
    private string _localPath = string.Empty;

    [ObservableProperty]
    private double _downloadProgress;

    [ObservableProperty]
    private bool _isDownloading;

    [ObservableProperty]
    private bool _hasError;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    [ObservableProperty]
    private DateTime? _lastChecked;

    [ObservableProperty]
    private string _lastCheckedText = "Nunca verificado";

    public string Status => State switch
    {
        DependencyState.Installed => "Pronto",
        DependencyState.NotInstalled => "Não instalado",
        DependencyState.Checking => "Verificando",
        DependencyState.Downloading => $"Baixando ({DownloadProgress:0}%)",
        DependencyState.Installing => "Instalando",
        DependencyState.Validating => "Validando",
        DependencyState.UpdateAvailable => "Atualização disponível",
        DependencyState.Error => "Erro",
        _ => "Não instalado"
    };

    public string StateBadgeText => State switch
    {
        DependencyState.Installed => "✓ Instalado",
        DependencyState.NotInstalled => "↓ Não instalado",
        DependencyState.Checking => "⟳ Verificando",
        DependencyState.Downloading => $"↓ Baixando {DownloadProgress:0}%",
        DependencyState.Installing => "⚙ Instalando",
        DependencyState.Validating => "⚙ Validando",
        DependencyState.UpdateAvailable => "↑ Atualização disponível",
        DependencyState.Error => "▲ Erro",
        _ => StatusText
    };

    partial void OnStateChanged(DependencyState value)
    {
        IsInstalled = value == DependencyState.Installed;
        HasError = value == DependencyState.Error;
        IsDownloading = value == DependencyState.Downloading;
        OnPropertyChanged(nameof(Status));
        OnPropertyChanged(nameof(StateBadgeText));
    }

    partial void OnDownloadProgressChanged(double value)
    {
        if (State == DependencyState.Downloading)
        {
            OnPropertyChanged(nameof(Status));
            OnPropertyChanged(nameof(StateBadgeText));
        }
    }

    partial void OnLastCheckedChanged(DateTime? value)
    {
        LastCheckedText = value.HasValue
            ? value.Value.ToString("dd/MM/yyyy HH:mm:ss")
            : "Nunca verificado";
    }

    public void SetChecking()
    {
        State = DependencyState.Checking;
        StatusText = "Verificando instalação...";
        HasError = false;
        ErrorMessage = string.Empty;
    }

    public void SetDownloading(double progress, string message)
    {
        State = DependencyState.Downloading;
        DownloadProgress = progress;
        StatusText = message;
        HasError = false;
        ErrorMessage = string.Empty;
    }

    public void SetInstalling(string message)
    {
        State = DependencyState.Installing;
        DownloadProgress = 85;
        StatusText = message;
    }

    public void SetValidating()
    {
        State = DependencyState.Validating;
        DownloadProgress = 95;
        StatusText = "Validando execução do binário...";
    }

    public void SetInstalled(string version)
    {
        State = DependencyState.Installed;
        Version = version;
        StatusText = $"Instalado ({version})";
        DownloadProgress = 100;
        HasError = false;
        ErrorMessage = string.Empty;
        LastChecked = DateTime.Now;
    }

    public void SetNotInstalled()
    {
        State = DependencyState.NotInstalled;
        Version = "Não instalado";
        StatusText = "Não instalado";
        DownloadProgress = 0;
        HasError = false;
        ErrorMessage = string.Empty;
        LastChecked = DateTime.Now;
    }

    public void SetError(string error)
    {
        State = DependencyState.Error;
        HasError = true;
        ErrorMessage = error;
        StatusText = $"Erro: {error}";
        DownloadProgress = 0;
    }
}
