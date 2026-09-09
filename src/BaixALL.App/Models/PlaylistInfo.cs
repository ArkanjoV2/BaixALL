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

    public string CollectionTypeBadgeText => Platform switch
    {
        PlatformType.Instagram => IsCarousel ? "CARROSSEL DO INSTAGRAM" : "INSTAGRAM",
        PlatformType.Twitter => "PUBLICAÇÃO DO X/TWITTER",
        _ => "PLAYLIST DO YOUTUBE"
    };

    public string PlatformBadgeColor => Platform switch
    {
        PlatformType.Instagram => "#F472B6",
        PlatformType.Twitter => "#38BDF8",
        _ => "#FFFFFF"
    };

    public string PlatformBackgroundColor => Platform switch
    {
        PlatformType.Instagram => "#331424",
        PlatformType.Twitter => "#0C2538",
        _ => "#0078D4"
    };

    public string AuthorOrChannelLabel => Platform switch
    {
        PlatformType.Instagram or PlatformType.Twitter => string.IsNullOrWhiteSpace(Channel)
            ? "Autor desconhecido"
            : $"Autor: @{Channel.TrimStart('@')}",
        _ => string.IsNullOrWhiteSpace(Channel)
            ? "Canal do YouTube"
            : $"Canal: {Channel}"
    };
}
