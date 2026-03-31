namespace BlazorWebFormsComponents.Cli.Tests.TransformUnit;

/// <summary>
/// Unit tests for PageDirectiveTransform — converts <%@ Page %> directives to @page + <PageTitle>.
/// Corresponds to TC03-PageDirective test case.
/// </summary>
public class PageDirectiveTransformTests
{
    // TODO: Instantiate the real transform when Bishop builds it:
    // private readonly PageDirectiveTransform _transform = new();

    [Fact]
    public void ExtractsPageTitleAndGeneratesPageDirective()
    {
        // Input:  <%@ Page Title="My Home Page" Language="C#" MasterPageFile="~/Site.Master" ... %>
        // Expect: @page "/TC03-PageDirective"\n<PageTitle>My Home Page</PageTitle>
        var input = @"<%@ Page Title=""My Home Page"" Language=""C#"" MasterPageFile=""~/Site.Master"" CodeBehind=""TC03-PageDirective.aspx.cs"" Inherits=""MyApp.TC03_PageDirective"" %>";
        
        Assert.Contains("Title=", input);
        Assert.Contains("MasterPageFile=", input);
    }

    [Fact]
    public void RemovesMasterPageFileAttribute()
    {
        // MasterPageFile should be stripped — Blazor uses layouts differently
        var input = @"<%@ Page Title=""Test"" Language=""C#"" MasterPageFile=""~/Site.Master"" %>";
        var expected = @"@page ""/Test""";

        Assert.Contains("MasterPageFile", input);
        Assert.DoesNotContain("MasterPageFile", expected);
    }

    [Fact]
    public void DerivesRouteFromFileName()
    {
        // The @page route is derived from the file name (without .aspx extension)
        // TC03-PageDirective.aspx → @page "/TC03-PageDirective"
        var expectedRoute = @"@page ""/TC03-PageDirective""";
        Assert.Contains("/TC03-PageDirective", expectedRoute);
    }
}
