using System.Text.RegularExpressions;
using BlazorWebFormsComponents.Cli.Pipeline;

namespace BlazorWebFormsComponents.Cli.Transforms.Markup;

/// <summary>
/// Converts &lt;form runat="server"&gt; to &lt;div&gt;, preserving id attribute for CSS.
/// </summary>
public class FormWrapperTransform : IMarkupTransform
{
    public string Name => "FormWrapper";
    public int Order => 310;

    private static readonly Regex FormOpenRegex = new(
        @"<form\s+([^>]*)runat\s*=\s*""server""([^>]*)>",
        RegexOptions.Compiled);

    private static readonly Regex FormCloseRegex = new(@"</form>", RegexOptions.Compiled);
    private static readonly Regex IdRegex = new(@"id\s*=\s*""([^""]*)""", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public string Apply(string content, FileMetadata metadata)
    {
        var match = FormOpenRegex.Match(content);
        if (!match.Success)
            return content;

        // Extract id attribute if present
        var fullAttrs = match.Groups[1].Value + match.Groups[2].Value;
        var idMatch = IdRegex.Match(fullAttrs);
        var idAttr = idMatch.Success ? $" id=\"{idMatch.Groups[1].Value}\"" : "";

        // Replace opening <form> with <div>
        content = FormOpenRegex.Replace(content, $"<div{idAttr}>", 1);

        // Replace corresponding </form> with </div>
        content = FormCloseRegex.Replace(content, "</div>", 1);

        return content;
    }
}
