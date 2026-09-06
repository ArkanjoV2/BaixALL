namespace BaixALL.App.Models;

public class ContainerOption
{
    public string Id { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string Extension { get; set; } = string.Empty;

    public override string ToString() => Label;

    public static List<ContainerOption> DefaultOptions => new()
    {
        new ContainerOption
        {
            Id = "auto",
            Label = "Automático (Recomendado)",
            Extension = "mp4"
        },
        new ContainerOption
        {
            Id = "mp4",
            Label = "MP4 (Alta compatibilidade)",
            Extension = "mp4"
        },
        new ContainerOption
        {
            Id = "mkv",
            Label = "MKV (Preserva codecs originais)",
            Extension = "mkv"
        },
        new ContainerOption
        {
            Id = "webm",
            Label = "WebM",
            Extension = "webm"
        }
    };
}
