using System.Text.RegularExpressions;
using BlazorWebFormsComponents.Cli.Pipeline;

namespace BlazorWebFormsComponents.Cli.Transforms.CodeBehind;

/// <summary>
/// Converts Response.Redirect() calls to NavigationManager.NavigateTo() and injects
/// [Inject] NavigationManager property into the class.
/// </summary>
public class ResponseRedirectTransform : ICodeBehindTransform
{
    public string Name => "ResponseRedirect";
    public int Order => 300;

    // Pattern 1: Response.Redirect("url", bool)
    private static readonly Regex RedirectLitBoolRegex = new(
        @"Response\.Redirect\(\s*""([^""]*)""\s*,\s*(?:true|false)\s*\)",
        RegexOptions.Compiled);

    // Pattern 2: Response.Redirect("url")
    private static readonly Regex RedirectLitRegex = new(
        @"Response\.Redirect\(\s*""([^""]*)""\s*\)",
        RegexOptions.Compiled);

    // Pattern 3: Response.Redirect(expr, bool)
    private static readonly Regex RedirectExprBoolRegex = new(
        @"Response\.Redirect\(\s*([^,)]+)\s*,\s*(?:true|false)\s*\)",
        RegexOptions.Compiled);

    // Pattern 4: Response.Redirect(expr)
    private static readonly Regex RedirectExprRegex = new(
        @"Response\.Redirect\(\s*([^)]+)\s*\)",
        RegexOptions.Compiled);

    // For injecting [Inject] NavigationManager
    private static readonly Regex ClassOpenRegex = new(
        @"((?:public|internal|private)\s+(?:partial\s+)?class\s+\w+[^{]*\{)",
        RegexOptions.Compiled);

    public string Apply(string content, FileMetadata metadata)
    {
        var hasRedirectConversion = false;

        // Pattern 1: literal URL with endResponse bool
        if (RedirectLitBoolRegex.IsMatch(content))
        {
            content = RedirectLitBoolRegex.Replace(content, m =>
            {
                var url = Regex.Replace(m.Groups[1].Value, @"^~/", "/");
                return $"NavigationManager.NavigateTo(\"{url}\")";
            });
            hasRedirectConversion = true;
        }

        // Pattern 2: simple literal URL
        if (RedirectLitRegex.IsMatch(content))
        {
            content = RedirectLitRegex.Replace(content, m =>
            {
                var url = Regex.Replace(m.Groups[1].Value, @"^~/", "/");
                return $"NavigationManager.NavigateTo(\"{url}\")";
            });
            hasRedirectConversion = true;
        }

        // Pattern 3: expression with endResponse bool
        if (RedirectExprBoolRegex.IsMatch(content))
        {
            content = RedirectExprBoolRegex.Replace(content, "NavigationManager.NavigateTo($1) /* TODO(bwfc-navigation): Verify navigation target */");
            hasRedirectConversion = true;
        }

        // Pattern 4: remaining expression URLs
        if (RedirectExprRegex.IsMatch(content))
        {
            content = RedirectExprRegex.Replace(content, "NavigationManager.NavigateTo($1) /* TODO(bwfc-navigation): Verify navigation target */");
            hasRedirectConversion = true;
        }

        // Inject [Inject] NavigationManager if conversions were made
        if (hasRedirectConversion && ClassOpenRegex.IsMatch(content))
        {
            var injectLine = "\n    [Inject] private NavigationManager NavigationManager { get; set; } // TODO(bwfc-navigation): Add @using Microsoft.AspNetCore.Components to _Imports.razor if needed\n";
            content = ClassOpenRegex.Replace(content, "$1" + injectLine, 1);
        }

        return content;
    }
}
