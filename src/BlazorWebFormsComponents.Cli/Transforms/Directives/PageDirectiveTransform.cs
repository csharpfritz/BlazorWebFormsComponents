using System.Text.RegularExpressions;
using BlazorWebFormsComponents.Cli.Pipeline;

namespace BlazorWebFormsComponents.Cli.Transforms.Directives;

/// <summary>
/// Converts &lt;%@ Page ... %&gt; directive to @page "/route" and &lt;PageTitle&gt;.
/// </summary>
public class PageDirectiveTransform : IMarkupTransform
{
    public string Name => "PageDirective";
    public int Order => 100;

    private static readonly Regex PageDirectiveRegex = new(@"<%@\s*Page[^%]*%>\s*\r?\n?", RegexOptions.Compiled);
    private static readonly Regex TitleRegex = new(@"<%@\s*Page[^%]*Title\s*=\s*""([^""]*)""", RegexOptions.Compiled);

    public string Apply(string content, FileMetadata metadata)
    {
        if (!PageDirectiveRegex.IsMatch(content))
            return content;

        // Extract title before stripping
        string? pageTitle = null;
        var titleMatch = TitleRegex.Match(content);
        if (titleMatch.Success)
            pageTitle = titleMatch.Groups[1].Value;

        // Strip the directive
        content = PageDirectiveRegex.Replace(content, "");

        // Build the route from the file name
        var fileName = Path.GetFileNameWithoutExtension(metadata.SourceFilePath);
        var route = "/" + fileName;

        // Home page detection
        var isHomePage = route is "/Default" or "/default" or "/Index" or "/index";
        if (Regex.IsMatch(fileName, @"^(Home|home)\.aspx$"))
            isHomePage = true;

        if (isHomePage && route != "/")
            route = "/";

        var header = $"@page \"{route}\"\n";

        // Dual-route for home pages that aren't already "/"
        if (isHomePage && route == "/" && fileName is not ("Default" or "default" or "Index" or "index"))
            header += "@page \"/\"\n";

        if (pageTitle != null)
            header += $"<PageTitle>{pageTitle}</PageTitle>\n";

        content = header + content;
        return content;
    }
}
