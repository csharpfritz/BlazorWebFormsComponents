using System.Text.RegularExpressions;
using BlazorWebFormsComponents.Cli.Pipeline;

namespace BlazorWebFormsComponents.Cli.Transforms.CodeBehind;

/// <summary>
/// Detects Session["key"] patterns and generates migration guidance block.
/// </summary>
public class SessionDetectTransform : ICodeBehindTransform
{
    public string Name => "SessionDetect";
    public int Order => 400;

    private static readonly Regex SessionKeyRegex = new(
        @"Session\[""([^""]*)""\]",
        RegexOptions.Compiled);

    private const string TodoEndMarker = "// =============================================================================";

    public string Apply(string content, FileMetadata metadata)
    {
        var matches = SessionKeyRegex.Matches(content);
        if (matches.Count == 0) return content;

        // Collect unique keys in order of appearance
        var sessionKeys = new List<string>();
        foreach (Match m in matches)
        {
            var key = m.Groups[1].Value;
            if (!sessionKeys.Contains(key)) sessionKeys.Add(key);
        }

        // Build guidance block
        var sessionBlock = "// --- Session State Migration ---\n"
            + $"// Session keys found: {string.Join(", ", sessionKeys)}\n"
            + "// Options:\n"
            + "//   (1) ProtectedSessionStorage (Blazor Server) — persists across circuits\n"
            + "//   (2) Scoped service via DI — lifetime matches user circuit\n"
            + "//   (3) Cascading parameter from a root-level state provider\n"
            + "// See: https://learn.microsoft.com/aspnet/core/blazor/state-management\n\n";

        // Insert after the TODO header end marker
        var lastTodoIdx = content.LastIndexOf(TodoEndMarker);
        if (lastTodoIdx >= 0)
        {
            var insertPos = lastTodoIdx + TodoEndMarker.Length;
            // Skip past newlines after marker
            while (insertPos < content.Length && (content[insertPos] == '\r' || content[insertPos] == '\n'))
                insertPos++;
            content = content[..insertPos] + "\n" + sessionBlock + content[insertPos..];
        }

        return content;
    }
}
