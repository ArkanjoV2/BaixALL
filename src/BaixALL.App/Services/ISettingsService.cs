using BaixALL.App.Models;

namespace BaixALL.App.Services;

public interface ISettingsService
{
    AppSettings Settings { get; }
    void SaveSettings();
    void ReloadSettings();
}
