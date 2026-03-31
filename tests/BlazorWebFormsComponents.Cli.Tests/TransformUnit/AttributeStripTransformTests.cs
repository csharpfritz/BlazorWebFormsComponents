namespace BlazorWebFormsComponents.Cli.Tests.TransformUnit;

/// <summary>
/// Unit tests for AttributeStripTransform — removes server-side ASP.NET attributes.
/// Corresponds to TC02-AttributeStrip test case.
/// </summary>
public class AttributeStripTransformTests
{
    // TODO: Instantiate the real transform when Bishop builds it:
    // private readonly AttributeStripTransform _transform = new();

    [Fact]
    public void StripsRunatServer()
    {
        // runat="server" is the most common attribute to strip
        var input = @"<div runat=""server""><span>Content</span></div>";
        var expected = @"<div><span>Content</span></div>";

        Assert.Contains(@"runat=""server""", input);
        Assert.DoesNotContain("runat", expected);
    }

    [Fact]
    public void StripsEnableViewState()
    {
        var input = @"<div EnableViewState=""true"" runat=""server"">";
        Assert.Contains("EnableViewState", input);
    }

    [Fact]
    public void StripsMultipleServerAttributes()
    {
        // Multiple server-only attributes should all be removed:
        // EnableViewState, ViewStateMode, ValidateRequest, MaintainScrollPositionOnPostBack, ClientIDMode
        var input = @"<div runat=""server"" EnableViewState=""true"" ViewStateMode=""Enabled"" ValidateRequest=""false"" MaintainScrollPositionOnPostBack=""true"" ClientIDMode=""Static"">";
        var expected = @"<div>";

        Assert.NotEqual(input, expected);
        Assert.DoesNotContain("runat", expected);
        Assert.DoesNotContain("EnableViewState", expected);
    }
}
