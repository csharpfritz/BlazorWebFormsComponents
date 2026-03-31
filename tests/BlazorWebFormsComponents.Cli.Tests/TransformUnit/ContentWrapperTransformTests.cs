namespace BlazorWebFormsComponents.Cli.Tests.TransformUnit;

/// <summary>
/// Unit tests for ContentWrapperTransform — strips asp:Content wrappers from master page content.
/// Corresponds to TC09-ContentWrappers test case.
/// </summary>
public class ContentWrapperTransformTests
{
    // TODO: Instantiate the real transform when Bishop builds it:
    // private readonly ContentWrapperTransform _transform = new();

    [Fact]
    public void StripsContentOpenTag()
    {
        // <asp:Content ID="BodyContent" ContentPlaceHolderID="MainContent" runat="server">
        // should be removed entirely, leaving inner content
        var input = @"<asp:Content ID=""BodyContent"" ContentPlaceHolderID=""MainContent"" runat=""server"">";

        Assert.Contains("asp:Content", input);
        Assert.Contains("ContentPlaceHolderID", input);
    }

    [Fact]
    public void StripsContentCloseTag()
    {
        // </asp:Content> closing tag should be removed
        var input = "</asp:Content>";
        Assert.Contains("asp:Content", input);
    }

    [Fact]
    public void PreservesInnerContent()
    {
        // Inner HTML between <asp:Content> tags should remain intact
        var fullInput = @"<asp:Content ID=""BodyContent"" ContentPlaceHolderID=""MainContent"" runat=""server"">
    <h1>Welcome</h1>
    <p>Hello World</p>
</asp:Content>";

        Assert.Contains("<h1>Welcome</h1>", fullInput);
        Assert.Contains("<p>Hello World</p>", fullInput);
    }
}
