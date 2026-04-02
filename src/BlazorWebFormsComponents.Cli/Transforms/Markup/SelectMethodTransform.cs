using System.Text.RegularExpressions;
using BlazorWebFormsComponents.Cli.Pipeline;

namespace BlazorWebFormsComponents.Cli.Transforms.Markup;

/// <summary>
/// Detects SelectMethod, InsertMethod, UpdateMethod, and DeleteMethod attributes on data-bound
/// controls. Preserves the attribute as-is and adds a TODO comment for delegate conversion.
/// </summary>
public class SelectMethodTransform : IMarkupTransform
{
    public string Name => "SelectMethod";
    public int Order => 520;

    private static readonly Regex MethodAttrRegex = new(
        @"(SelectMethod|InsertMethod|UpdateMethod|DeleteMethod)=""([^""]+)""",
        RegexOptions.Compiled);

    public string Apply(string content, FileMetadata metadata)
    {
        var lines = content.Split('\n');
        var result = new List<string>();

        foreach (var line in lines)
        {
            result.Add(line);

            var matches = MethodAttrRegex.Matches(line);
            foreach (Match match in matches)
            {
                var attrName = match.Groups[1].Value;
                var methodName = match.Groups[2].Value;
                result.Add(
                    $"@* TODO(bwfc-select-method): Convert {attrName}=\"{methodName}\" to a code-behind method that sets Items property *@");
            }
        }

        return string.Join('\n', result);
    }
}
