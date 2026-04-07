using System.Text.RegularExpressions;
using BlazorWebFormsComponents.Cli.Pipeline;

namespace BlazorWebFormsComponents.Cli.Transforms.Markup;

/// <summary>
/// Converts master page layout elements to Blazor layout syntax.
/// Replaces &lt;asp:ContentPlaceHolder&gt; with @Body, adds @inherits LayoutComponentBase,
/// and strips runat="server" from head and form tags.
/// </summary>
public class MasterPageTransform : IMarkupTransform
{
    public string Name => "MasterPage";
    public int Order => 250;

    // Block: <asp:ContentPlaceHolder ...>...</asp:ContentPlaceHolder>
    private static readonly Regex ContentPlaceHolderBlockRegex = new(
        @"<asp:ContentPlaceHolder\s+[^>]*>[\s\S]*?</asp:ContentPlaceHolder>[ \t]*\r?\n?",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    // Self-closing: <asp:ContentPlaceHolder ... />
    private static readonly Regex ContentPlaceHolderSelfClosingRegex = new(
        @"<asp:ContentPlaceHolder\s+[^>]*?/>[ \t]*\r?\n?",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    // runat="server" on <head> tags
    private static readonly Regex HeadRunatRegex = new(
        @"(<head\b[^>]*?)\s+runat\s*=\s*""server""([^>]*>)",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    // runat="server" on <form> tags
    private static readonly Regex FormRunatRegex = new(
        @"(<form\b[^>]*?)\s+runat\s*=\s*""server""([^>]*>)",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private const string InheritsDirective = "@inherits LayoutComponentBase";
    private const string TodoComment = "@* TODO(bwfc-master-page): Review head content extraction for App.razor *@";

    public string Apply(string content, FileMetadata metadata)
    {
        if (metadata.FileType != FileType.Master)
            return content;

        // Convert ContentPlaceHolder blocks (with default content) → @Body
        content = ContentPlaceHolderBlockRegex.Replace(content, "@Body\n");

        // Convert ContentPlaceHolder self-closing → @Body
        content = ContentPlaceHolderSelfClosingRegex.Replace(content, "@Body\n");

        // Strip runat="server" from <head> tags
        content = HeadRunatRegex.Replace(content, "$1$2");

        // Strip runat="server" from <form> tags
        content = FormRunatRegex.Replace(content, "$1$2");

        // Add @inherits LayoutComponentBase at the top if not already present
        if (!content.Contains(InheritsDirective))
        {
            content = InheritsDirective + "\n" + TodoComment + "\n\n" + content;
        }

        return content;
    }
}
