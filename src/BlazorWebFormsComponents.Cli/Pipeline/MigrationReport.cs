namespace BlazorWebFormsComponents.Cli.Pipeline;

/// <summary>
/// Summary report from a migration run.
/// </summary>
public class MigrationReport
{
    public int FilesProcessed { get; set; }
    public int FilesWritten { get; set; }
    public int TransformsApplied { get; set; }
    public List<string> Errors { get; } = new();
    public List<string> Warnings { get; } = new();
}
