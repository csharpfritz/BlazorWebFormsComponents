using System.Text;
using System.Text.RegularExpressions;
using BlazorWebFormsComponents.Cli.Pipeline;

namespace BlazorWebFormsComponents.Cli.Transforms.Markup;

/// <summary>
/// Detects DataSourceID attributes and data source controls. Replaces DataSourceID with
/// Items binding and scaffolds compilable data properties so the output compiles immediately.
/// BWFC uses Items binding instead of DataSourceID references.
/// </summary>
public class DataSourceIdTransform : IMarkupTransform
{
    public string Name => "DataSourceId";
    public int Order => 820;

    // DataSourceID attribute on bound controls
    private static readonly Regex DataSourceIdAttrRegex = new(
        @"\s*DataSourceID=""([^""]+)""",
        RegexOptions.Compiled);

    // Extract id attribute (case-insensitive) from within a matched tag
    private static readonly Regex TagIdRegex = new(
        @"\bid\s*=\s*""([^""]+)""",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    // Data source controls (asp: prefix already stripped at this point)
    private static readonly string[] DataSourceControls =
    [
        "SqlDataSource", "ObjectDataSource", "LinqDataSource",
        "EntityDataSource", "XmlDataSource", "SiteMapDataSource", "AccessDataSource"
    ];

    public string Apply(string content, FileMetadata metadata)
    {
        var dataSourceIds = new List<string>();

        // Replace DataSourceID attributes with Items binding
        content = DataSourceIdAttrRegex.Replace(content, m =>
        {
            var dsId = m.Groups[1].Value;
            if (!dataSourceIds.Contains(dsId))
                dataSourceIds.Add(dsId);
            return $" Items=\"@{dsId}Data\"";
        });

        // Replace data source control declarations with TODO comments
        foreach (var ctrl in DataSourceControls)
        {
            // Self-closing: <SqlDataSource ... />
            var selfCloseRegex = new Regex($@"(?s)<{Regex.Escape(ctrl)}\b.*?/>");
            content = selfCloseRegex.Replace(content, m => BuildDataSourceTodo(m.Value, ctrl));

            // Open+close: <SqlDataSource ...>...</SqlDataSource>
            var openCloseRegex = new Regex($@"(?s)<{Regex.Escape(ctrl)}\b.*?</{Regex.Escape(ctrl)}\s*>");
            content = openCloseRegex.Replace(content, m => BuildDataSourceTodo(m.Value, ctrl));
        }

        // Inject data scaffolding so the output compiles
        if (dataSourceIds.Count > 0)
        {
            content = InjectDataScaffolding(dataSourceIds, content, metadata);
        }

        return content;
    }

    private static string BuildDataSourceTodo(string tagContent, string controlName)
    {
        var idMatch = TagIdRegex.Match(tagContent);
        if (idMatch.Success)
        {
            var id = idMatch.Groups[1].Value;
            return $"@* TODO(bwfc-datasource): Implement I{id}DataService to replace {controlName} *@";
        }

        return $"@* TODO(bwfc-datasource): <{controlName}> has no Blazor equivalent — wire data through code-behind service injection and SelectMethod/Items *@";
    }

    private static string InjectDataScaffolding(
        List<string> dataSourceIds, string content, FileMetadata metadata)
    {
        var propertyLines = new List<string>();
        foreach (var dsId in dataSourceIds)
        {
            propertyLines.Add($"    // TODO(bwfc-datasource): Replace {dsId}Data with real data from injected service");
            propertyLines.Add($"    private IEnumerable<object> {dsId}Data {{ get; set; }} = Array.Empty<object>();");
        }

        if (metadata.CodeBehindContent != null)
        {
            // Inject properties into code-behind class body (before last closing brace)
            var lastBrace = metadata.CodeBehindContent.LastIndexOf('}');
            if (lastBrace >= 0)
            {
                var injection = "\n" + string.Join("\n", propertyLines) + "\n";
                metadata.CodeBehindContent = metadata.CodeBehindContent.Insert(lastBrace, injection);
            }
        }
        else
        {
            // No code-behind — append @code block to markup
            var sb = new StringBuilder(content.TrimEnd());
            sb.Append('\n');
            sb.Append('\n');
            sb.Append("@code {\n");
            foreach (var line in propertyLines)
            {
                sb.Append(line);
                sb.Append('\n');
            }
            sb.Append('}');
            content = sb.ToString();
        }

        return content;
    }
}
