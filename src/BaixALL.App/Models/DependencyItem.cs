using CommunityToolkit.Mvvm.ComponentModel;

namespace BaixALL.App.Models;

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
    private string _statusText = "Aguardando";

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
}
