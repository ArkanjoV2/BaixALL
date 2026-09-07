using System;
using System.Windows;
using BaixALL.App.ViewModels;
using Microsoft.Win32;

namespace BaixALL.App.Views;

public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;

    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = _viewModel;

        _viewModel.SettingsVm.ThemeChanged += OnThemeChanged;
        ApplyTheme(_viewModel.SettingsVm.Theme);

        Loaded += async (_, _) =>
        {
            await _viewModel.InitializeStartupAsync();
        };

        Closing += (_, _) =>
        {
            if (_viewModel.HasActiveDownloads)
            {
                _viewModel.CancelAllDownloads();
            }
        };
    }

    private void OnThemeChanged(string theme)
    {
        ApplyTheme(theme);
    }

    private void ApplyTheme(string theme)
    {
        var isDark = true;

        if (theme.Equals("Light", StringComparison.OrdinalIgnoreCase))
        {
            isDark = false;
        }
        else if (theme.Equals("Dark", StringComparison.OrdinalIgnoreCase))
        {
            isDark = true;
        }
        else // System
        {
            isDark = IsSystemInDarkMode();
        }

        var themeUri = isDark
            ? new Uri("Resources/Themes/DarkTheme.xaml", UriKind.Relative)
            : new Uri("Resources/Themes/LightTheme.xaml", UriKind.Relative);

        var newDict = new ResourceDictionary { Source = themeUri };

        // Substitui ou adiciona o dicionário de tema no topo de Application.Current.Resources
        var appResources = Application.Current.Resources.MergedDictionaries;
        var existingTheme = -1;
        for (int i = 0; i < appResources.Count; i++)
        {
            var src = appResources[i].Source?.OriginalString ?? "";
            if (src.Contains("DarkTheme.xaml") || src.Contains("LightTheme.xaml"))
            {
                existingTheme = i;
                break;
            }
        }

        if (existingTheme >= 0)
        {
            appResources[existingTheme] = newDict;
        }
        else
        {
            appResources.Insert(0, newDict);
        }
    }

    private static bool IsSystemInDarkMode()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            if (key != null)
            {
                var val = key.GetValue("AppsUseLightTheme");
                if (val is int intVal)
                {
                    return intVal == 0;
                }
            }
        }
        catch { }

        return true; // Padrão Dark
    }
}
