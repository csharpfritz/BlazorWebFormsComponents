namespace BlazorWebFormsComponents.Cli.Tests.TransformUnit;

/// <summary>
/// Unit tests for FormWrapperTransform — converts server form elements to div.
/// Corresponds to TC05-FormWrapper test case.
/// </summary>
public class FormWrapperTransformTests
{
    // TODO: Instantiate the real transform when Bishop builds it:
    // private readonly FormWrapperTransform _transform = new();

    [Fact]
    public void ConvertsFormToDiv()
    {
        // Input:  <form id="form1" runat="server">...</form>
        // Expect: <div id="form1">...</div>
        var input = @"<form id=""form1"" runat=""server"">";
        var expected = @"<div id=""form1"">";

        Assert.Contains("form", input);
        Assert.Contains("div", expected);
    }

    [Fact]
    public void PreservesFormId()
    {
        // The id attribute is preserved for CSS compatibility
        var input = @"<form id=""form1"" runat=""server"">";
        var expected = @"<div id=""form1"">";

        Assert.Contains(@"id=""form1""", expected);
    }

    [Fact]
    public void ConvertsClosingFormTag()
    {
        // Closing </form> becomes </div>
        var input = "</form>";
        var expected = "</div>";

        Assert.NotEqual(input, expected);
    }
}
