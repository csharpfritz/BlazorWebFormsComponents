using System.Text.RegularExpressions;
using BlazorWebFormsComponents.Cli.Pipeline;

namespace BlazorWebFormsComponents.Cli.Transforms.CodeBehind;

/// <summary>
/// Unwraps simple if (!IsPostBack) guards (brace-counting) and adds TODO for complex ones (with else).
/// In Blazor, OnInitializedAsync runs only once so the guard is unnecessary.
/// </summary>
public class IsPostBackTransform : ICodeBehindTransform
{
    public string Name => "IsPostBack";
    public int Order => 500;

    // Combined pattern for "if not postback" variants
    private static readonly Regex GuardRegex = new(
        @"(?:if\s*\(\s*!(?:Page\.|this\.)?IsPostBack\s*\)|if\s*\(\s*(?:Page\.|this\.)?IsPostBack\s*==\s*false\s*\)|if\s*\(\s*false\s*==\s*(?:Page\.|this\.)?IsPostBack\s*\))",
        RegexOptions.Compiled);

    private const int MaxIterations = 50;

    public string Apply(string content, FileMetadata metadata)
    {
        var iterations = 0;

        while (GuardRegex.IsMatch(content) && iterations < MaxIterations)
        {
            iterations++;
            var match = GuardRegex.Match(content);
            var matchStart = match.Index;
            var afterMatch = matchStart + match.Length;

            // Skip whitespace to find opening brace
            var braceStart = afterMatch;
            while (braceStart < content.Length && char.IsWhiteSpace(content[braceStart]))
                braceStart++;

            if (braceStart >= content.Length || content[braceStart] != '{')
            {
                // Single-statement guard — add TODO
                content = content[..matchStart]
                    + "/* TODO: IsPostBack guard — review for Blazor */ "
                    + content[matchStart..];
                continue;
            }

            // Brace-count to find matching close brace
            var depth = 1;
            var pos = braceStart + 1;
            while (pos < content.Length && depth > 0)
            {
                if (content[pos] == '{') depth++;
                else if (content[pos] == '}') depth--;
                pos++;
            }

            if (depth != 0)
            {
                // Unbalanced braces
                content = content[..matchStart]
                    + "/* TODO: IsPostBack guard — could not parse */ "
                    + content[matchStart..];
                continue;
            }

            var braceEnd = pos - 1; // position of closing brace

            // Check for else clause
            var checkPos = braceEnd + 1;
            while (checkPos < content.Length && char.IsWhiteSpace(content[checkPos]))
                checkPos++;

            var hasElse = (checkPos + 3 < content.Length)
                && content.Substring(checkPos, 4).StartsWith("else", StringComparison.Ordinal)
                && (checkPos + 4 >= content.Length || !char.IsLetterOrDigit(content[checkPos + 4]));

            if (hasElse)
            {
                // Complex case — add TODO comment
                var todoComment = "// TODO: BWFC — IsPostBack guard with else clause. In Blazor, OnInitializedAsync runs once (no postback).\n            // Review: move 'if' body to OnInitializedAsync and 'else' body to an event handler or remove.\n            ";
                content = content[..matchStart] + todoComment + content[matchStart..];
            }
            else
            {
                // Simple case — unwrap the guard
                var body = content.Substring(braceStart + 1, braceEnd - braceStart - 1);

                // Dedent: remove one level of leading whitespace (4 spaces or 1 tab) per line
                var bodyLines = body.Split('\n');
                var dedentedLines = bodyLines.Select(line =>
                {
                    if (line.StartsWith("    ")) return line[4..];
                    if (line.StartsWith("\t")) return line[1..];
                    return line;
                }).ToArray();
                var dedentedBody = string.Join("\n", dedentedLines).Trim();

                // Determine indentation of original if statement
                var lineStart = matchStart;
                while (lineStart > 0 && content[lineStart - 1] != '\n') lineStart--;
                var indent = "";
                var leadingText = content[lineStart..matchStart];
                var indentMatch = Regex.Match(leadingText, @"^(\s+)");
                if (indentMatch.Success) indent = indentMatch.Groups[1].Value;

                var replacement = indent + "// BWFC: IsPostBack guard unwrapped — Blazor re-renders on every state change\n";
                foreach (var line in dedentedBody.Split('\n'))
                {
                    if (line.Trim().Length > 0)
                        replacement += indent + line + "\n";
                    else
                        replacement += "\n";
                }

                content = content[..matchStart] + replacement.TrimEnd('\n') + content[(braceEnd + 1)..];
            }
        }

        return content;
    }
}
