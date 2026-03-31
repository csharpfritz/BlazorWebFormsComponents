using BlazorWebFormsComponents.Cli.Transforms;

namespace BlazorWebFormsComponents.Cli.Pipeline;

/// <summary>
/// Orchestrates the full file conversion pipeline: markup transforms, then code-behind transforms.
/// </summary>
public class MigrationPipeline
{
    private readonly IReadOnlyList<IMarkupTransform> _markupTransforms;
    private readonly IReadOnlyList<ICodeBehindTransform> _codeBehindTransforms;

    public MigrationPipeline(
        IEnumerable<IMarkupTransform> markupTransforms,
        IEnumerable<ICodeBehindTransform> codeBehindTransforms)
    {
        _markupTransforms = markupTransforms.OrderBy(t => t.Order).ToList();
        _codeBehindTransforms = codeBehindTransforms.OrderBy(t => t.Order).ToList();
    }

    /// <summary>
    /// Run the full pipeline on all source files in the context.
    /// </summary>
    public async Task<MigrationReport> ExecuteAsync(MigrationContext context)
    {
        var report = new MigrationReport();

        foreach (var sourceFile in context.SourceFiles)
        {
            try
            {
                var markupContent = await File.ReadAllTextAsync(sourceFile.MarkupPath);
                var metadata = new FileMetadata
                {
                    SourceFilePath = sourceFile.MarkupPath,
                    OutputFilePath = sourceFile.OutputPath,
                    FileType = sourceFile.FileType,
                    OriginalContent = markupContent
                };

                // Read code-behind if present
                if (sourceFile.HasCodeBehind)
                {
                    metadata.CodeBehindContent = await File.ReadAllTextAsync(sourceFile.CodeBehindPath!);
                }

                // Markup pipeline
                var markup = markupContent;
                foreach (var transform in _markupTransforms)
                {
                    markup = transform.Apply(markup, metadata);
                }

                // Code-behind pipeline
                string? codeBehind = null;
                if (sourceFile.HasCodeBehind && metadata.CodeBehindContent != null)
                {
                    codeBehind = metadata.CodeBehindContent;
                    foreach (var transform in _codeBehindTransforms)
                    {
                        codeBehind = transform.Apply(codeBehind, metadata);
                    }
                }

                // Write output
                if (!context.Options.DryRun)
                {
                    var outputDir = Path.GetDirectoryName(sourceFile.OutputPath);
                    if (outputDir != null)
                        Directory.CreateDirectory(outputDir);

                    await File.WriteAllTextAsync(sourceFile.OutputPath, markup);
                    report.FilesWritten++;

                    if (codeBehind != null)
                    {
                        var codeOutputPath = sourceFile.OutputPath + ".cs";
                        await File.WriteAllTextAsync(codeOutputPath, codeBehind);
                        report.FilesWritten++;
                    }
                }

                report.FilesProcessed++;
            }
            catch (Exception ex)
            {
                report.Errors.Add($"{sourceFile.MarkupPath}: {ex.Message}");
            }
        }

        return report;
    }

    /// <summary>
    /// Run only the markup pipeline on a single string (useful for single-file convert).
    /// </summary>
    public string TransformMarkup(string content, FileMetadata metadata)
    {
        foreach (var transform in _markupTransforms)
        {
            content = transform.Apply(content, metadata);
        }
        return content;
    }

    /// <summary>
    /// Run only the code-behind pipeline on a single string.
    /// </summary>
    public string TransformCodeBehind(string content, FileMetadata metadata)
    {
        foreach (var transform in _codeBehindTransforms)
        {
            content = transform.Apply(content, metadata);
        }
        return content;
    }
}
