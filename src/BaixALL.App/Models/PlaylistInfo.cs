using System;
using System.Collections.Generic;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;

namespace BaixALL.App.Models;

public partial class PlaylistInfo : ObservableObject
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Channel { get; set; } = string.Empty;
    public string ThumbnailUrl { get; set; } = string.Empty;
    public string OriginalUrl { get; set; } = string.Empty;
    public PlatformType Platform { get; set; } = PlatformType.YouTube;
    public bool IsCarousel { get; set; }
    public string Author => Channel;
    public int TotalVideosCount { get; set; }
    public List<PlaylistItemInfo> Items { get; set; } = new();

    [ObservableProperty]
    private int _selectedVideosCount;

    public string SelectionSummary => $"{SelectedVideosCount} de {TotalVideosCount} selecionados";

    public void UpdateCounts()
    {
        SelectedVideosCount = Items.Count(x => x.IsSelected && x.IsAvailable);
        OnPropertyChanged(nameof(SelectionSummary));
    }

    public string FormattedTotalDuration
    {
        get
        {
            var totalSeconds = Items.Where(x => x.DurationSeconds.HasValue).Sum(x => x.DurationSeconds!.Value);
            if (totalSeconds <= 0) return "—";

            var ts = TimeSpan.FromSeconds(totalSeconds);
            if (ts.TotalHours >= 1)
            {
                return $"{(int)ts.TotalHours}h {ts.Minutes}m";
            }
            return $"{ts.Minutes}m {ts.Seconds}s";
        }
    }
}
