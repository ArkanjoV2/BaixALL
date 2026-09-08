using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
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

        SourceInitialized += (_, _) =>
        {
            ApplyTitleBarTheme(_viewModel.SettingsVm.Theme);
        };

        Loaded += async (_, _) =>
        {
            ApplyTitleBarTheme(_viewModel.SettingsVm.Theme);
            await _viewModel.InitializeStartupAsync();
        };

        Closing += (sender, e) =>
        {
            if (_viewModel.HasActiveDownloads)
            {
                var result = MessageBox.Show(
                    "Existem downloads em andamento ou itens aguardando na fila.\n\n" +
                    "Se você fechar o BaixALL agora, os downloads ativos serão cancelados e os itens pendentes NÃO serão retomados automaticamente na próxima inicialização.\n\n" +
                    "Os arquivos já concluídos, o histórico e as configurações permanecerão salvos.\n\n" +
                    "Deseja realmente cancelar e fechar?",
                    "Downloads em Andamento - BaixALL",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result != MessageBoxResult.Yes)
                {
                    e.Cancel = true;
                    return;
                }

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
            ? new Uri("pack://application:,,,/BaixALL;component/Resources/Themes/DarkTheme.xaml", UriKind.Absolute)
            : new Uri("pack://application:,,,/BaixALL;component/Resources/Themes/LightTheme.xaml", UriKind.Absolute);

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

        ApplyTitleBarTheme(theme);
    }

    private void ApplyTitleBarTheme(string theme)
    {
        try
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

            var helper = new WindowInteropHelper(this);
            var hwnd = helper.Handle;
            if (hwnd == IntPtr.Zero)
            {
                return;
            }

            // 1. Modo escuro imersivo no Windows 10 (19041+) e Windows 11
            int darkMode = isDark ? 1 : 0;
            if (DwmSetWindowAttribute(hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE, ref darkMode, sizeof(int)) != 0)
            {
                DwmSetWindowAttribute(hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE_BEFORE_20H1, ref darkMode, sizeof(int));
            }

            // 2. Cor exata da barra superior no Windows 11 (DWMWA_CAPTION_COLOR em formato 0x00BBGGRR)
            // Dark: #0B1E33 -> 0x00331E0B
            // Light: Refined Blue #1C3A60 -> 0x00603A1C
            int captionColor = isDark ? 0x00331E0B : 0x00603A1C;
            DwmSetWindowAttribute(hwnd, DWMWA_CAPTION_COLOR, ref captionColor, sizeof(int));

            // 3. Cor do texto e ícones na barra de título (Branco puro nos dois temas)
            int textColor = 0x00FFFFFF;
            DwmSetWindowAttribute(hwnd, DWMWA_TEXT_COLOR, ref textColor, sizeof(int));
        }
        catch
        {
            // Silencioso se executado em ambiente sem suporte a DWM ou versões antigas do Windows
        }
    }

    [DllImport("dwmapi.dll", PreserveSig = true)]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

    private const int DWMWA_USE_IMMERSIVE_DARK_MODE_BEFORE_20H1 = 19;
    private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;
    private const int DWMWA_CAPTION_COLOR = 35;
    private const int DWMWA_TEXT_COLOR = 36;

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
