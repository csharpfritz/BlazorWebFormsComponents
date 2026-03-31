using System.Text.RegularExpressions;
using BlazorWebFormsComponents.Cli.Pipeline;

namespace BlazorWebFormsComponents.Cli.Transforms.Markup;

/// <summary>
/// Detects DataSourceID attributes and data source controls, replaces with TODO warnings.
/// BWFC uses SelectMethod/Items binding instead.
/// </summary>
public class DataSourceIdTransform : IMarkupTransform
{
    public string Name => "DataSourceId";
    public int Order => 820;

    // DataSourceID attribute
    private static readonly Regex DataSourceIdAttrRegex = new(
        @"\s*DataSourceID=""([^""]+)""",
        RegexOptions.Compiled);

    // Data source controls (asp: prefix already stripped at this point)
    private static readonly string[] DataSourceControls =
    [
        "SqlDataSource", "ObjectDataSource", "LinqDataSource",
        "EntityDataSource", "XmlDataSource", "SiteMapDataSource", "AccessDataSource"
    ];

    public string Apply(string content, FileMetadata metadata)
    {
        // Remove DataSourceID attributes
        content = DataSourceIdAttrRegex.Replace(content, "");

        // Replace data source control declarations with TODO comments
        foreach (var ctrl in DataSourceControls)
        {
            // Self-closing: <SqlDataSource ... />
            var selfCloseRegex = new Regex($@"(?s)<{Regex.Escape(ctrl)}\b.*?/>");
            content = selfCloseRegex.Replace(content,
                $"@* TODO: <{ctrl}> has no Blazor equivalent — wire data through code-behind service injection and SelectMethod/Items *@");

            // Open+close: <SqlDataSource ...>...</SqlDataSource>
            var openCloseRegex = new Regex($@"(?s)<{Regex.Escape(ctrl)}\b.*?</{Regex.Escape(ctrl)}\s*>");
            content = openCloseRegex.Replace(content,
                $"@* TODO: <{ctrl}> has no Blazor equivalent — wire data through code-behind service injection and SelectMethod/Items *@");
        }

        return content;
    }
}
