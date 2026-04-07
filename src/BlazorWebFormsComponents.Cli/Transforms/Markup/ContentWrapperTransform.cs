using System.Text.RegularExpressions;
using BlazorWebFormsComponents.Cli.Pipeline;

namespace BlazorWebFormsComponents.Cli.Transforms.Markup;

/// <summary>
/// Removes &lt;asp:Content&gt; wrapper tags, preserving inner content.
/// Handles HeadContent placeholders and TitleContent extraction.
/// </summary>
public class ContentWrapperTransform : IMarkupTransform
{
    public string Name => "ContentWrapper";
    public int Order => 300;

    // Open tags for any ContentPlaceHolderID — strip entirely, keeping content
    private static readonly Regex ContentOpenRegex = new(
        @"<asp:Content\s+[^>]*ContentPlaceHolderID\s*=\s*""[^""]*""[^>]*>[ \t]*\r?\n?",
        RegexOptions.Compiled);

    // Closing </asp:Content> tags
    private static readonly Regex ContentCloseRegex = new(
        @"</asp:Content>\s*\r?\n?",
        RegexOptions.Compiled);

    public string Apply(string content, FileMetadata metadata)
    {
        // Strip opening <asp:Content> wrappers
        content = ContentOpenRegex.Replace(content, "");

        // Strip closing </asp:Content> tags
        content = ContentCloseRegex.Replace(content, "");

        return content;
    }
}
