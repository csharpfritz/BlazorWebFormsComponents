using System.Text.RegularExpressions;
using BlazorWebFormsComponents.Cli.Pipeline;

namespace BlazorWebFormsComponents.Cli.Transforms.CodeBehind;

/// <summary>
/// Detects Page.ClientScript and ScriptManager code-behind patterns and transforms
/// automatable cases to IJSRuntime skeletons, or emits TODO markers for manual migration.
///
/// Automatable:
///   - RegisterStartupScript() with inline script → OnAfterRenderAsync + eval()
///   - RegisterClientScriptInclude() with URL → TODO to add script tag to layout
///   - ScriptManager.RegisterStartupScript() → same as RegisterStartupScript
///
/// Non-automatable (TODO markers):
///   - GetPostBackEventReference() → TODO: replace with @onclick / EventCallback
///   - RegisterClientScriptBlock() → TODO: move to IJSRuntime or .js file
///   - ScriptManager.GetCurrent() → TODO: use IJSRuntime directly
/// </summary>
public class ClientScriptTransform : ICodeBehindTransform
{
    public string Name => "ClientScript";
    public int Order => 850;

    // --- Automatable patterns ---

    // Pattern 1: Page.ClientScript.RegisterStartupScript(type, key, "script", bool)
    //   Also matches: ClientScript.RegisterStartupScript(...)
    //   Also matches: this.ClientScript.RegisterStartupScript(...)
    private static readonly Regex RegisterStartupScriptRegex = new(
        @"[ \t]*(?:Page\.ClientScript|(?:this\.)?ClientScript)\.RegisterStartupScript\s*\([^,]*,\s*[^,]*,\s*""([^""]*)""\s*(?:,\s*(?:true|false)\s*)?\)\s*;[ \t]*\r?\n?",
        RegexOptions.Compiled);

    // Pattern 1b: ScriptManager.RegisterStartupScript(control, type, key, "script", bool)
    //   ScriptManager.RegisterStartupScript has 5 params typically
    private static readonly Regex ScriptManagerStartupScriptRegex = new(
        @"[ \t]*ScriptManager\.RegisterStartupScript\s*\([^,]*,\s*[^,]*,\s*[^,]*,\s*""([^""]*)""\s*(?:,\s*(?:true|false)\s*)?\)\s*;[ \t]*\r?\n?",
        RegexOptions.Compiled);

    // Pattern 2: Page.ClientScript.RegisterClientScriptInclude(key, "url")
    //   Also matches: ClientScript.RegisterClientScriptInclude(...)
    //   Also matches variants with ResolveUrl
    private static readonly Regex RegisterScriptIncludeRegex = new(
        @"[ \t]*(?:Page\.ClientScript|(?:this\.)?ClientScript)\.RegisterClientScriptInclude\s*\([^,]*,\s*(?:ResolveUrl\s*\(\s*)?""([^""]*)""(?:\s*\))?\s*\)\s*;[ \t]*\r?\n?",
        RegexOptions.Compiled);

    // --- Non-automatable patterns ---

    // Pattern 3: Page.ClientScript.GetPostBackEventReference(...)
    //   Uses alternation of quoted-strings and non-quote chars to handle complex args
    private static readonly Regex GetPostBackEventRefRegex = new(
        @"([ \t]*)(.*(?:Page\.ClientScript|(?:this\.)?ClientScript)\.GetPostBackEventReference\s*\((?:""[^""]*""|[^""])*?\)\s*;)",
        RegexOptions.Compiled);

    // Pattern 4: Page.ClientScript.RegisterClientScriptBlock(...) or ClientScript.RegisterClientScriptBlock(...)
    //   Uses alternation of quoted-strings and non-quote chars to handle semicolons inside string args
    private static readonly Regex RegisterScriptBlockRegex = new(
        @"([ \t]*)(?:Page\.ClientScript|(?:this\.)?ClientScript)\.RegisterClientScriptBlock\s*\((?:""[^""]*""|[^""])*?\)\s*;",
        RegexOptions.Compiled);

    // Pattern 5: ScriptManager.GetCurrent(...) — handles nested parens
    private static readonly Regex ScriptManagerGetCurrentRegex = new(
        @"([ \t]*)(?:var\s+\w+\s*=\s*)?ScriptManager\.GetCurrent\s*\((?:""[^""]*""|[^""])*?\)\s*;",
        RegexOptions.Compiled);

    // For injecting [Inject] IJSRuntime
    private static readonly Regex ClassOpenRegex = new(
        @"((?:public|internal|private)\s+(?:partial\s+)?class\s+\w+[^{]*\{)",
        RegexOptions.Compiled);

    public string Apply(string content, FileMetadata metadata)
    {
        var hasStartupScript = false;

        // Pattern 1: RegisterStartupScript with inline literal → OnAfterRenderAsync skeleton
        if (RegisterStartupScriptRegex.IsMatch(content))
        {
            content = RegisterStartupScriptRegex.Replace(content, m =>
            {
                var script = m.Groups[1].Value;
                return BuildStartupScriptReplacement(script);
            });
            hasStartupScript = true;
        }

        // Pattern 1b: ScriptManager.RegisterStartupScript with inline literal
        if (ScriptManagerStartupScriptRegex.IsMatch(content))
        {
            content = ScriptManagerStartupScriptRegex.Replace(content, m =>
            {
                var script = m.Groups[1].Value;
                return BuildStartupScriptReplacement(script);
            });
            hasStartupScript = true;
        }

        // Pattern 2: RegisterClientScriptInclude → TODO comment
        if (RegisterScriptIncludeRegex.IsMatch(content))
        {
            content = RegisterScriptIncludeRegex.Replace(content, m =>
            {
                var url = m.Groups[1].Value;
                // Strip ~/ prefix for Blazor
                var cleanUrl = url.StartsWith("~/") ? url[2..] : url;
                return $"            // TODO(bwfc-general): Add <script src=\"{cleanUrl}\"/> to _Host.cshtml or App.razor\n";
            });
            hasStartupScript = true;
        }

        // Pattern 3: GetPostBackEventReference → TODO
        if (GetPostBackEventRefRegex.IsMatch(content))
        {
            content = GetPostBackEventRefRegex.Replace(content, m =>
            {
                var indent = m.Groups[1].Value;
                var originalLine = m.Groups[2].Value;
                return $"{indent}// TODO(bwfc-general): Replace __doPostBack with @onclick or EventCallback. See ClientScriptMigrationGuide.md\n{indent}// Original: {originalLine.Trim()}";
            });
        }

        // Pattern 4: RegisterClientScriptBlock → TODO
        if (RegisterScriptBlockRegex.IsMatch(content))
        {
            content = RegisterScriptBlockRegex.Replace(content, m =>
            {
                var indent = m.Groups[1].Value;
                return $"{indent}// TODO(bwfc-general): Move script block to IJSRuntime.InvokeVoidAsync or a .js file. See ClientScriptMigrationGuide.md";
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

        // Inject [Inject] IJSRuntime if startup script conversions were made
        if (hasStartupScript && ClassOpenRegex.IsMatch(content) &&
            !content.Contains("[Inject] private IJSRuntime JS"))
        {
            var injectLine = "\n    [Inject] private IJSRuntime JS { get; set; } // TODO(bwfc-general): Add @using Microsoft.JSInterop to _Imports.razor if needed\n";
            content = ClassOpenRegex.Replace(content, "$1" + injectLine, 1);
        }

        return content;
    }

    private static string BuildStartupScriptReplacement(string script)
    {
        return "            // TODO(bwfc-general): Review and refactor eval() usage — move script to a .js file and call via IJSRuntime\n"
             + $"            await JS.InvokeVoidAsync(\"eval\", @\"{EscapeForVerbatimString(script)}\");\n";
    }

    private static string EscapeForVerbatimString(string input)
    {
        return input.Replace("\"", "\"\"");
    }
}
