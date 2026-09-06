namespace BaixALL.App.Models;

public class AudioFormatOption
{
    public string Id { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string Extension { get; set; } = string.Empty;
    public bool RequiresConversion { get; set; }

    public override string ToString() => Label;

    public static List<AudioFormatOption> DefaultOptions => new()
    {
        new AudioFormatOption
        {
            Id = "best",
            Label = "Melhor áudio disponível (Original)",
            Extension = "m4a",
            RequiresConversion = false
        },
        new AudioFormatOption
        {
            Id = "mp3",
            Label = "MP3 (Alta compatibilidade)",
            Extension = "mp3",
            RequiresConversion = true
        },
        new AudioFormatOption
        {
            Id = "m4a",
            Label = "M4A (AAC)",
            Extension = "m4a",
            RequiresConversion = false
        },
        new AudioFormatOption
        {
            Id = "opus",
            Label = "Opus (Alta fidelidade)",
            Extension = "opus",
            RequiresConversion = false
        }
    };
}
