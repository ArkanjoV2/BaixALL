namespace BaixALL.App.Models;

public class FormatOption
{
    public string Label { get; set; } = string.Empty;
    public int? Height { get; set; }
    public int? Fps { get; set; }
    public string FormatSelector { get; set; } = "bestvideo+bestaudio/best";
    public bool IsBestQuality { get; set; }
    public bool IsAudioOnly { get; set; }
    public string Description { get; set; } = string.Empty;
    public WatermarkStatus WatermarkStatus { get; set; } = WatermarkStatus.NotApplicable;
    public bool RequiresWatermarkConsent { get; set; }

    public override string ToString() => Label;
}
