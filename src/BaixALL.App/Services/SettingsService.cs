using System;
using System.IO;
using System.Text.Json;
using BaixALL.App.Infrastructure;
using BaixALL.App.Models;

namespace BaixALL.App.Services;

public class SettingsService : ISettingsService
{
    private readonly string _settingsFilePath;
    private readonly ILoggerService? _logger;
    private AppSettings _settings;

    public AppSettings Settings => _settings;

    public SettingsService(ILoggerService? logger = null, string? settingsFilePath = null)
    {
        _logger = logger;
        _settingsFilePath = settingsFilePath ?? AppConstants.SettingsFilePath;
        _settings = LoadSettings();
    }

    public void SaveSettings()
    {
        try
        {
            var directory = Path.GetDirectoryName(_settingsFilePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var json = JsonSerializer.Serialize(_settings, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            File.WriteAllText(_settingsFilePath, json);
            _logger?.Info("Configurações salvas com sucesso.");
        }
        catch (Exception ex)
        {
            _logger?.Error("Falha ao salvar configurações.", ex);
        }
    }

    public void ReloadSettings()
    {
        _settings = LoadSettings();
    }

    private AppSettings LoadSettings()
    {
        try
        {
            if (File.Exists(_settingsFilePath))
            {
                var json = File.ReadAllText(_settingsFilePath);
                var loaded = JsonSerializer.Deserialize<AppSettings>(json);
                if (loaded != null)
                {
                    return loaded;
                }
            }
        }
        catch (Exception ex)
        {
            _logger?.Error("Erro ao carregar configurações salvas, usando padrões.", ex);
        }

        return new AppSettings();
    }
}
