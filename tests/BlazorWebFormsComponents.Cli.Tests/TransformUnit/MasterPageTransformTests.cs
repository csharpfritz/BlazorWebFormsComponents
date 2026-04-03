using BlazorWebFormsComponents.Cli.Pipeline;
using BlazorWebFormsComponents.Cli.Transforms.Markup;

namespace BlazorWebFormsComponents.Cli.Tests.TransformUnit;

/// <summary>
/// Unit tests for MasterPageTransform — converts master page layout elements to Blazor layout syntax.
/// Corresponds to TC23-MasterPage test case.
/// </summary>
public class MasterPageTransformTests
{
    private readonly MasterPageTransform _transform = new();

    private static FileMetadata MasterMetadata => new()
    {
        SourceFilePath = "Site.master",
        OutputFilePath = "Site.razor",
        FileType = FileType.Master,
        OriginalContent = ""
    };

    private static FileMetadata PageMetadata => new()
    {
        SourceFilePath = "Default.aspx",
        OutputFilePath = "Default.razor",
        FileType = FileType.Page,
        OriginalContent = ""
    };

    [Fact]
    public void ConvertsSelfClosingContentPlaceHolder()
    {
        var input = "<asp:ContentPlaceHolder ID=\"HeadContent\" runat=\"server\" />";

        var result = _transform.Apply(input, MasterMetadata);
        Assert.Contains("@Body", result);
        Assert.DoesNotContain("asp:ContentPlaceHolder", result);
    }

    [Fact]
    public void ConvertsBlockContentPlaceHolderWithDefaultContent()
    {
        var input = "<asp:ContentPlaceHolder ID=\"MainContent\" runat=\"server\">\n" +
                    "    <p>Default content</p>\n" +
                    "</asp:ContentPlaceHolder>";

        var result = _transform.Apply(input, MasterMetadata);
        Assert.Contains("@Body", result);
        Assert.DoesNotContain("asp:ContentPlaceHolder", result);
        Assert.DoesNotContain("Default content", result);
    }

    [Fact]
    public void AddsInheritsDirective()
    {
        var input = "<html><body></body></html>";

        var result = _transform.Apply(input, MasterMetadata);
        Assert.StartsWith("@inherits LayoutComponentBase", result);
    }

    [Fact]
    public void DoesNotDuplicateInheritsDirective()
    {
        var input = "@inherits LayoutComponentBase\n<html><body></body></html>";

        var result = _transform.Apply(input, MasterMetadata);
        var count = result.Split("@inherits LayoutComponentBase").Length - 1;
        Assert.Equal(1, count);
    }

    [Fact]
    public void AddsTodoComment()
    {
        var input = "<html></html>";

        var result = _transform.Apply(input, MasterMetadata);
        Assert.Contains("@* TODO(bwfc-master-page): Review head content extraction for App.razor *@", result);
    }

    [Fact]
    public void StripsRunatFromHead()
    {
        var input = "<head runat=\"server\">\n    <title>Test</title>\n</head>";

        var result = _transform.Apply(input, MasterMetadata);
        Assert.Contains("<head>", result);
        Assert.DoesNotContain("<head runat=\"server\">", result);
    }

    [Fact]
    public void StripsRunatFromHeadPreservingOtherAttributes()
    {
        var input = "<head id=\"Head1\" runat=\"server\">";

        var result = _transform.Apply(input, MasterMetadata);
        Assert.Contains("<head id=\"Head1\">", result);
        Assert.DoesNotContain("runat", result.Substring(result.IndexOf("<head")));
    }

    [Fact]
    public void StripsRunatFromForm()
    {
        var input = "<form id=\"form1\" runat=\"server\">\n    <div>content</div>\n</form>";

        var result = _transform.Apply(input, MasterMetadata);
        Assert.Contains("<form id=\"form1\">", result);
        Assert.DoesNotContain("runat=\"server\"", result.Substring(result.IndexOf("<form")));
    }

    [Fact]
    public void SkipsNonMasterFiles()
    {
        var input = "<asp:ContentPlaceHolder ID=\"MainContent\" runat=\"server\" />";

        var result = _transform.Apply(input, PageMetadata);
        Assert.Equal(input, result);
    }

    [Fact]
    public void PreservesHeadContent()
    {
        var input = "<head runat=\"server\">\n" +
                    "    <title>My Site</title>\n" +
                    "    <link href=\"Site.css\" rel=\"stylesheet\" />\n" +
                    "</head>";

        var result = _transform.Apply(input, MasterMetadata);
        Assert.Contains("<title>My Site</title>", result);
        Assert.Contains("<link href=\"Site.css\" rel=\"stylesheet\" />", result);
    }

    [Fact]
    public void ConvertsFullMasterPage()
    {
        var input = "<!DOCTYPE html>\n" +
                    "<html>\n" +
                    "<head runat=\"server\">\n" +
                    "    <title>My Site</title>\n" +
                    "</head>\n" +
                    "<body>\n" +
                    "    <form id=\"form1\" runat=\"server\">\n" +
                    "        <asp:ContentPlaceHolder ID=\"MainContent\" runat=\"server\" />\n" +
                    "    </form>\n" +
                    "</body>\n" +
                    "</html>";

        var result = _transform.Apply(input, MasterMetadata);

        Assert.StartsWith("@inherits LayoutComponentBase", result);
        Assert.Contains("@Body", result);
        Assert.Contains("<head>", result);
        Assert.Contains("<form id=\"form1\">", result);
        Assert.DoesNotContain("asp:ContentPlaceHolder", result);
        Assert.DoesNotContain("runat=\"server\"", result);
        Assert.Contains("TODO(bwfc-master-page)", result);
    }

    [Fact]
    public void OrderIs250()
    {
        Assert.Equal(250, _transform.Order);
    }
}
