using System.Text.RegularExpressions;
using BlazorWebFormsComponents.Cli.Pipeline;

namespace BlazorWebFormsComponents.Cli.Transforms.CodeBehind;

/// <summary>
/// Transforms Web Forms event handler signatures to Blazor-compatible signatures:
///   - Standard EventArgs → strip both params: Handler()
///   - Specialized EventArgs → strip sender, keep EventArgs: Handler(SpecializedEventArgs e)
/// </summary>
public class EventHandlerSignatureTransform : ICodeBehindTransform
{
    public string Name => "EventHandlerSignature";
    public int Order => 700;

    // Group 1: everything before parens (modifiers + return type + method name)
    // Group 2: the EventArgs type name
    // Group 3: the EventArgs parameter name
    private static readonly Regex HandlerRegex = new(
        @"((?:(?:protected|private|public|internal)\s+)?(?:(?:static|virtual|override|new|sealed|abstract|async)\s+)*(?:void|Task(?:<[^>]+>)?)\s+\w+)\s*\(\s*object\s+\w+\s*,\s*(\w*EventArgs)\s+(\w+)\s*\)",
        RegexOptions.Compiled);

    private const int MaxIterations = 200;

    public string Apply(string content, FileMetadata metadata)
    {
        var iterations = 0;

        while (HandlerRegex.IsMatch(content) && iterations < MaxIterations)
        {
            iterations++;
            var match = HandlerRegex.Match(content);
            var prefix = match.Groups[1].Value;
            var eventArgsType = match.Groups[2].Value;
            var eventArgsParam = match.Groups[3].Value;

            string replacement;
            if (eventArgsType == "EventArgs")
            {
                // Standard EventArgs — strip both params entirely
                replacement = $"{prefix}()";
            }
            else
            {
                // Specialized EventArgs — strip sender, keep EventArgs param
                replacement = $"{prefix}({eventArgsType} {eventArgsParam})";
            }

            content = content[..match.Index] + replacement + content[(match.Index + match.Length)..];
        }

        return content;
    }
}
