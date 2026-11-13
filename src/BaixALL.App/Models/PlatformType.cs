namespace BaixALL.App.Models;

public enum PlatformType
{
    Unknown = 0,
    YouTube = 1,
    Instagram = 2,
    Twitter = 3, // Exibido como "X / Twitter"
    TikTok = 4
}

public enum WatermarkStatus
{
    NotApplicable = 0,
    NoWatermark = 1,
    Watermarked = 2,
    Unknown = 3
}
