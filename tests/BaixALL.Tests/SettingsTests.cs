using System;
using System.IO;
using BaixALL.App.Services;
using Xunit;

namespace BaixALL.Tests;

public class SettingsTests
{
    [Fact]
    public void SettingsService_ShouldSaveAndReloadSettings()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"baixall_settings_test_{Guid.NewGuid()}.json");

        try
        {
            var service1 = new SettingsService(settingsFilePath: tempFile);
            service1.Settings.MaxConcurrentDownloads = 3;
            service1.Settings.Theme = "Dark";
            service1.Settings.DownloadFolder = @"C:\TestDownloads";
            service1.SaveSettings();

            Assert.True(File.Exists(tempFile));

            var service2 = new SettingsService(settingsFilePath: tempFile);
            Assert.Equal(3, service2.Settings.MaxConcurrentDownloads);
            Assert.Equal("Dark", service2.Settings.Theme);
            Assert.Equal(@"C:\TestDownloads", service2.Settings.DownloadFolder);
        }
        finally
        {
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }
}
