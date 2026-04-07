namespace BlazorWebFormsComponents.Cli.Pipeline;

/// <summary>
/// Per-file metadata passed to each transform step.
/// </summary>
public class FileMetadata
{
    public required string SourceFilePath { get; init; }
    public required string OutputFilePath { get; init; }
    public required FileType FileType { get; init; }
    public required string OriginalContent { get; init; }
    public string? CodeBehindContent { get; set; }
    public Dictionary<string, string> DataBindMap { get; set; } = new();
    public string FileName => Path.GetFileNameWithoutExtension(SourceFilePath);
}

public enum FileType
{
    Page,
    Master,
    Control
}
