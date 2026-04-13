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

    // catch (ThreadAbortException) — dead code after migration (Blazor doesn't throw)
    private static readonly Regex ThreadAbortCatchRegex = new(
        @"catch\s*\(\s*ThreadAbortException\b",
        RegexOptions.Compiled);

    // Response.Redirect(url, true) — endResponse=true silently ignored by ResponseShim
    private static readonly Regex RedirectEndResponseTrueRegex = new(
        @"Response\.Redirect\s*\([^,)]+,\s*true\s*\)",
        RegexOptions.Compiled);

    private const string ThreadAbortMarker = "// TODO(bwfc-navigation): DEAD CODE";
    private const string EndResponseMarker = "// TODO(bwfc-navigation): endResponse=true";

    public string Apply(string content, FileMetadata metadata)
    {
        var hasRedirectConversion = false;

        // Pre-detect endResponse=true BEFORE conversion replaces it
        var hasEndResponseTrue = RedirectEndResponseTrueRegex.IsMatch(content);

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

        // Detect ThreadAbortException catch blocks — dead code after migration
        if (ThreadAbortCatchRegex.IsMatch(content) && !content.Contains(ThreadAbortMarker))
        {
            content = ThreadAbortCatchRegex.Replace(content, m =>
                m.Value + $"\n            {ThreadAbortMarker} — Blazor does not throw ThreadAbortException on redirect. " +
                "This catch block is dead code after migration. Review and remove if safe.");
        }

        // Warn about Response.Redirect(url, true) endResponse behavior
        if (hasEndResponseTrue && !content.Contains(EndResponseMarker))
        {
            if (ClassOpenRegex.IsMatch(content))
            {
                var warning = $"\n    {EndResponseMarker} is silently ignored by ResponseShim. " +
                    "Code after redirect calls WILL execute (unlike Web Forms where it threw ThreadAbortException).\n";
                content = ClassOpenRegex.Replace(content, "$1" + warning, 1);
            }
        }

        return content;
    }
}
