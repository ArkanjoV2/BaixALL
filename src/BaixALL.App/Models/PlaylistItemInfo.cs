using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace BaixALL.App.Models;

public partial class PlaylistItemInfo : ObservableObject
{
    public string Id { get; set; } = string.Empty;
    public string VideoUrl { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Channel { get; set; } = string.Empty;
    public double? DurationSeconds { get; set; }
    public string ThumbnailUrl { get; set; } = string.Empty;
    public int PlaylistIndex { get; set; }
    public PlatformType Platform { get; set; } = PlatformType.YouTube;
    public string CanonicalKey { get; set; } = string.Empty;

    [ObservableProperty]
    private bool _isSelected = true;

    [ObservableProperty]
    private bool _isAvailable = true;

    [ObservableProperty]
    private string _availabilityNotice = string.Empty;

    public string FormattedDuration
    {
        get
        {
            if (!DurationSeconds.HasValue || DurationSeconds.Value <= 0)
            {
                return "—";
            }

            var ts = TimeSpan.FromSeconds(DurationSeconds.Value);
            return ts.TotalHours >= 1
                ? ts.ToString(@"h\:mm\:ss")
                : ts.ToString(@"m\:ss");
        }
    }

    public string FormattedIndex => $"#{PlaylistIndex:D2}";
}
