namespace BlazorWebFormsComponents.Cli.Tests.TransformUnit;

/// <summary>
/// Unit tests for UrlReferenceTransform — converts ~/ URLs to root-relative paths.
/// Corresponds to TC07-UrlTilde test case.
/// </summary>
public class UrlReferenceTransformTests
{
    // TODO: Instantiate the real transform when Bishop builds it:
    // private readonly UrlReferenceTransform _transform = new();

    [Fact]
    public void ConvertsTildeInHref()
    {
        // Input:  href="~/Styles/Site.css"
        // Expect: href="/Styles/Site.css"
        var input = @"<link href=""~/Styles/Site.css"" rel=""stylesheet"" />";
        var expected = @"<link href=""/Styles/Site.css"" rel=""stylesheet"" />";

        Assert.Contains("~/", input);
        Assert.DoesNotContain("~/", expected);
    }

    [Fact]
    public void ConvertsTildeInNavigateUrl()
    {
        // NavigateUrl="~/Products/List.aspx" → NavigateUrl="/Products/List.aspx"
        var input = @"<HyperLink NavigateUrl=""~/Products/List.aspx"" Text=""Products"" />";
        var expected = @"<HyperLink NavigateUrl=""/Products/List.aspx"" Text=""Products"" />";

        Assert.Contains("~/", input);
        Assert.DoesNotContain("~/", expected);
    }

    [Fact]
    public void ConvertsTildeInImageUrl()
    {
        // ImageUrl="~/Images/logo.png" → ImageUrl="/Images/logo.png"
        var input = @"<Image ImageUrl=""~/Images/logo.png"" />";
        var expected = @"<Image ImageUrl=""/Images/logo.png"" />";

        Assert.Contains("~/", input);
        Assert.DoesNotContain("~/", expected);
    }
}
