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

    public override string ToString() => Label;
}
