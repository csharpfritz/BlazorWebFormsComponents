using System.Text.RegularExpressions;
using BlazorWebFormsComponents.Cli.Pipeline;

namespace BlazorWebFormsComponents.Cli.Transforms.CodeBehind;

/// <summary>
/// Detects Page.ClientScript and ScriptManager code-behind patterns and preserves them
/// for use with the ClientScriptShim. Strips Page./this. prefixes so calls are compatible
/// with the shim API, and emits TODO markers for unsupported patterns.
///
/// Shim-compatible (prefix stripping, calls preserved):
///   - RegisterStartupScript() → strip Page./this. prefix
///   - RegisterClientScriptInclude() → strip Page./this. prefix
///   - RegisterClientScriptBlock() → strip Page./this. prefix
///   - ScriptManager.RegisterStartupScript() → convert to ClientScript.RegisterStartupScript()
///
/// Non-automatable (TODO markers):
///   - GetPostBackEventReference() → TODO: replace with @onclick / EventCallback
///   - ScriptManager.GetCurrent() → TODO: use IJSRuntime directly
/// </summary>
public class ClientScriptTransform : ICodeBehindTransform
{
    public string Name => "ClientScript";
    public int Order => 850;

    // --- Shim-compatible patterns (strip prefix, preserve calls) ---

    // Strips "Page." or "this." prefix before ClientScript method calls that the shim supports
    private static readonly Regex PageOrThisPrefixRegex = new(
        @"(?:Page\.|this\.)(?=ClientScript\.(?:RegisterStartupScript|RegisterClientScriptInclude|RegisterClientScriptBlock)\s*\()",
        RegexOptions.Compiled);

    // Pattern 1b: ScriptManager.RegisterStartupScript(control, type, key, script, bool)
    //   → ClientScript.RegisterStartupScript(type, key, script, bool) — drops the first param
    private static readonly Regex ScriptManagerStartupScriptRegex = new(
        @"ScriptManager\.RegisterStartupScript\s*\(\s*[^,]*,\s*",
        RegexOptions.Compiled);

    // --- Non-automatable patterns ---

    // Pattern 3: Page.ClientScript.GetPostBackEventReference(...)
    //   Uses alternation of quoted-strings and non-quote chars to handle complex args
    private static readonly Regex GetPostBackEventRefRegex = new(
        @"([ \t]*)(.*(?:Page\.ClientScript|(?:this\.)?ClientScript)\.GetPostBackEventReference\s*\((?:""[^""]*""|[^""])*?\)\s*;)",
        RegexOptions.Compiled);

    // Pattern 5: ScriptManager.GetCurrent(...) — handles nested parens
    private static readonly Regex ScriptManagerGetCurrentRegex = new(
        @"([ \t]*)(?:var\s+\w+\s*=\s*)?ScriptManager\.GetCurrent\s*\((?:""[^""]*""|[^""])*?\)\s*;",
        RegexOptions.Compiled);

    // For injecting ClientScriptShim dependency comment
    private static readonly Regex ClassOpenRegex = new(
        @"((?:public|internal|private)\s+(?:partial\s+)?class\s+\w+[^{]*\{)",
        RegexOptions.Compiled);

    public string Apply(string content, FileMetadata metadata)
    {
        var hasShimCall = false;

        // Patterns 1, 2, 4: Strip Page./this. prefix — calls become shim-compatible
        if (PageOrThisPrefixRegex.IsMatch(content))
        {
            content = PageOrThisPrefixRegex.Replace(content, "");
            hasShimCall = true;
        }

        // Pattern 1b: ScriptManager.RegisterStartupScript → ClientScript.RegisterStartupScript (drop first param)
        if (ScriptManagerStartupScriptRegex.IsMatch(content))
        {
            content = ScriptManagerStartupScriptRegex.Replace(content, "ClientScript.RegisterStartupScript(");
            hasShimCall = true;
        }

        // Detect calls that were already "ClientScript.XXX(...)" without prefix — still shim-compatible
        if (!hasShimCall && (content.Contains("ClientScript.RegisterStartupScript") ||
            content.Contains("ClientScript.RegisterClientScriptInclude") ||
            content.Contains("ClientScript.RegisterClientScriptBlock")))
        {
            hasShimCall = true;
        }

        // Pattern 3: GetPostBackEventReference → TODO (shim throws NotSupportedException)
        if (GetPostBackEventRefRegex.IsMatch(content))
        {
            content = GetPostBackEventRefRegex.Replace(content, m =>
            {
                var indent = m.Groups[1].Value;
                var originalLine = m.Groups[2].Value;
                return $"{indent}// TODO(bwfc-general): Replace __doPostBack with @onclick or EventCallback. See ClientScriptMigrationGuide.md\n{indent}// Original: {originalLine.Trim()}";
            });
        }

        // Pattern 5: ScriptManager.GetCurrent → TODO
        if (ScriptManagerGetCurrentRegex.IsMatch(content))
        {
            content = ScriptManagerGetCurrentRegex.Replace(content, m =>
            {
                var indent = m.Groups[1].Value;
                return $"{indent}// TODO(bwfc-general): ScriptManager.GetCurrent() has no Blazor equivalent. Use IJSRuntime directly.";
            });
        }

        // Add ClientScriptShim dependency comment when shim-preserving transforms were made
        if (hasShimCall && ClassOpenRegex.IsMatch(content) &&
            !content.Contains("// TODO(bwfc-general): ClientScript calls preserved"))
        {
            var shimComment = "\n    // TODO(bwfc-general): ClientScript calls preserved — uses ClientScriptShim. Inject @inject ClientScriptShim ClientScript if not using BaseWebFormsComponent.\n";
            content = ClassOpenRegex.Replace(content, "$1" + shimComment, 1);
        }

        return content;
    }
}
