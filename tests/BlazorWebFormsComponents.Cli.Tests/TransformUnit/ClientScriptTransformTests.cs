using BlazorWebFormsComponents.Cli.Pipeline;
using BlazorWebFormsComponents.Cli.Transforms.CodeBehind;

namespace BlazorWebFormsComponents.Cli.Tests.TransformUnit;

/// <summary>
/// Unit tests for ClientScriptTransform — strips Page./this. prefixes from ClientScript
/// calls so they work with the ClientScriptShim, converts ScriptManager.RegisterStartupScript
/// to ClientScript calls, and emits TODO markers for unsupported patterns.
///
/// Covers TC36 (startup scripts), TC37 (script includes/blocks), TC38 (postback references).
/// </summary>
public class ClientScriptTransformTests
{
    private readonly ClientScriptTransform _transform = new();

    private static FileMetadata TestMetadata(string content) => new()
    {
        SourceFilePath = "Default.aspx.cs",
        OutputFilePath = "Default.razor.cs",
        FileType = FileType.Page,
        OriginalContent = content
    };

    #region TC36 — RegisterStartupScript

    [Fact]
    public void TC36_RegisterStartupScript_StripsPagePrefix()
    {
        var input = @"namespace MyApp
{
    public partial class MyPage
    {
        void Page_Load()
        {
            Page.ClientScript.RegisterStartupScript(this.GetType(), ""InitUI"", ""alert('ready');"", true);
        }
    }
}";
        var result = _transform.Apply(input, TestMetadata(input));

        Assert.Contains("ClientScript.RegisterStartupScript", result);
        Assert.DoesNotContain("Page.ClientScript", result);
        Assert.DoesNotContain("JS.InvokeVoidAsync", result);
    }

    [Fact]
    public void TC36_RegisterStartupScript_AddsShimTodoComment()
    {
        var input = @"namespace MyApp
{
    public partial class MyPage
    {
        void Page_Load()
        {
            Page.ClientScript.RegisterStartupScript(this.GetType(), ""Init"", ""console.log('hi');"", true);
        }
    }
}";
        var result = _transform.Apply(input, TestMetadata(input));

        Assert.Contains("TODO(bwfc-general)", result);
        Assert.Contains("ClientScriptShim", result);
    }

    [Fact]
    public void TC36_RegisterStartupScript_NoIJSRuntimeInjection()
    {
        var input = @"namespace MyApp
{
    public partial class MyPage
    {
        void Page_Load()
        {
            Page.ClientScript.RegisterStartupScript(this.GetType(), ""key"", ""alert('a');"", true);
        }
    }
}";
        var result = _transform.Apply(input, TestMetadata(input));

        Assert.DoesNotContain("[Inject] private IJSRuntime JS", result);
        Assert.Contains("ClientScriptShim", result);
    }

    [Fact]
    public void TC36_ClientScriptWithoutPagePrefix_PreservedUnchanged()
    {
        var input = @"namespace MyApp
{
    public partial class MyPage
    {
        void Page_Load()
        {
            ClientScript.RegisterStartupScript(this.GetType(), ""key"", ""init();"", true);
        }
    }
}";
        var result = _transform.Apply(input, TestMetadata(input));

        Assert.Contains("ClientScript.RegisterStartupScript", result);
        Assert.DoesNotContain("JS.InvokeVoidAsync", result);
        Assert.Contains("ClientScriptShim", result);
    }

    [Fact]
    public void TC36_ScriptManagerRegisterStartupScript_ConvertedToClientScript()
    {
        var input = @"namespace MyApp
{
    public partial class MyPage
    {
        void Page_Load()
        {
            ScriptManager.RegisterStartupScript(this, this.GetType(), ""smScript"", ""alert('hello');"", true);
        }
    }
}";
        var result = _transform.Apply(input, TestMetadata(input));

        Assert.Contains("ClientScript.RegisterStartupScript(this.GetType()", result);
        Assert.DoesNotContain("ScriptManager.RegisterStartupScript", result);
        Assert.DoesNotContain("JS.InvokeVoidAsync", result);
    }

    [Fact]
    public void TC36_MultipleStartupScripts_AllPreserved()
    {
        var input = @"namespace MyApp
{
    public partial class MyPage
    {
        void Page_Load()
        {
            Page.ClientScript.RegisterStartupScript(this.GetType(), ""init1"", ""alert('a');"", true);
            Page.ClientScript.RegisterStartupScript(this.GetType(), ""init2"", ""alert('b');"", true);
        }
    }
}";
        var result = _transform.Apply(input, TestMetadata(input));

        Assert.Contains("alert('a');", result);
        Assert.Contains("alert('b');", result);
        Assert.Contains("ClientScript.RegisterStartupScript", result);
        Assert.DoesNotContain("Page.ClientScript", result);
    }

    #endregion

    #region TC37 — RegisterClientScriptInclude and RegisterClientScriptBlock

    [Fact]
    public void TC37_RegisterClientScriptInclude_PreservedWithPrefixStripped()
    {
        var input = @"namespace MyApp
{
    public partial class MyPage
    {
        void Page_Load()
        {
            Page.ClientScript.RegisterClientScriptInclude(""jqueryUI"", ResolveUrl(""~/Scripts/jquery-ui.min.js""));
        }
    }
}";
        var result = _transform.Apply(input, TestMetadata(input));

        Assert.Contains("ClientScript.RegisterClientScriptInclude", result);
        Assert.DoesNotContain("Page.ClientScript", result);
        Assert.Contains("ClientScriptShim", result);
    }

    [Fact]
    public void TC37_RegisterClientScriptInclude_PreservesResolveUrl()
    {
        var input = @"namespace MyApp
{
    public partial class MyPage
    {
        void Page_Load()
        {
            Page.ClientScript.RegisterClientScriptInclude(""custom"", ResolveUrl(""~/lib/custom.js""));
        }
    }
}";
        var result = _transform.Apply(input, TestMetadata(input));

        Assert.Contains("ClientScript.RegisterClientScriptInclude", result);
        Assert.Contains("ResolveUrl", result);
        Assert.DoesNotContain("Page.ClientScript", result);
    }

    [Fact]
    public void TC37_RegisterClientScriptBlock_PreservedWithPrefixStripped()
    {
        var input = @"namespace MyApp
{
    public partial class MyPage
    {
        void Page_Load()
        {
            Page.ClientScript.RegisterClientScriptBlock(this.GetType(), ""block1"", ""<script>var x = 1;</script>"");
        }
    }
}";
        var result = _transform.Apply(input, TestMetadata(input));

        Assert.Contains("ClientScript.RegisterClientScriptBlock", result);
        Assert.DoesNotContain("Page.ClientScript", result);
        Assert.Contains("ClientScriptShim", result);
    }

    [Fact]
    public void TC37_RegisterClientScriptBlock_NoLongerEmitsMigrationGuideTodo()
    {
        var input = @"namespace MyApp
{
    public partial class MyPage
    {
        void Page_Load()
        {
            Page.ClientScript.RegisterClientScriptBlock(this.GetType(), ""key"", ""var x=1;"");
        }
    }
}";
        var result = _transform.Apply(input, TestMetadata(input));

        Assert.DoesNotContain("Move script block to IJSRuntime", result);
        Assert.Contains("ClientScript.RegisterClientScriptBlock", result);
    }

    #endregion

    #region TC38 — GetPostBackEventReference and ScriptManager.GetCurrent

    [Fact]
    public void TC38_GetPostBackEventReference_EmitsTodoWithEventCallbackGuidance()
    {
        var input = @"namespace MyApp
{
    public partial class MyPage
    {
        void DoWork()
        {
            var postbackRef = Page.ClientScript.GetPostBackEventReference(btnSubmit, ""validate"");
        }
    }
}";
        var result = _transform.Apply(input, TestMetadata(input));

        Assert.Contains("TODO(bwfc-general)", result);
        Assert.Contains("@onclick", result);
        Assert.Contains("EventCallback", result);
    }

    [Fact]
    public void TC38_GetPostBackEventReference_PreservesOriginalAsComment()
    {
        var input = @"namespace MyApp
{
    public partial class MyPage
    {
        void DoWork()
        {
            var postbackRef = Page.ClientScript.GetPostBackEventReference(btnSubmit, ""validate"");
        }
    }
}";
        var result = _transform.Apply(input, TestMetadata(input));

        Assert.Contains("// Original:", result);
        Assert.Contains("GetPostBackEventReference", result);
    }

    [Fact]
    public void TC38_ScriptManagerGetCurrent_EmitsTodo()
    {
        var input = @"namespace MyApp
{
    public partial class MyPage
    {
        void Page_Load()
        {
            var sm = ScriptManager.GetCurrent(Page);
        }
    }
}";
        var result = _transform.Apply(input, TestMetadata(input));

        Assert.Contains("TODO(bwfc-general)", result);
        Assert.Contains("ScriptManager.GetCurrent()", result);
        Assert.Contains("IJSRuntime", result);
    }

    [Fact]
    public void TC38_ScriptManagerGetCurrent_WithThis_EmitsTodo()
    {
        var input = @"namespace MyApp
{
    public partial class MyPage
    {
        void Page_Load()
        {
            var sm = ScriptManager.GetCurrent(this);
        }
    }
}";
        var result = _transform.Apply(input, TestMetadata(input));

        Assert.Contains("TODO(bwfc-general)", result);
        Assert.Contains("ScriptManager.GetCurrent()", result);
    }

    #endregion

    #region Combined and Edge Cases

    [Fact]
    public void AllPatterns_InOneFile_AllTransformed()
    {
        var input = @"namespace MyApp
{
    public partial class MyPage
    {
        void Page_Load()
        {
            Page.ClientScript.RegisterStartupScript(this.GetType(), ""init"", ""alert('hi');"", true);
            Page.ClientScript.RegisterClientScriptInclude(""jqueryUI"", ResolveUrl(""~/lib/jquery-ui.min.js""));
            var postbackRef = Page.ClientScript.GetPostBackEventReference(btnSubmit, ""validate"");
            Page.ClientScript.RegisterClientScriptBlock(this.GetType(), ""block1"", ""var x=1;"");
            ScriptManager.RegisterStartupScript(this, this.GetType(), ""smScript"", ""alert('sm');"", true);
            var sm = ScriptManager.GetCurrent(Page);
        }
    }
}";
        var result = _transform.Apply(input, TestMetadata(input));

        // Startup scripts preserved with prefix stripped
        Assert.Contains("ClientScript.RegisterStartupScript", result);
        Assert.DoesNotContain("Page.ClientScript.RegisterStartupScript", result);
        // Include preserved with prefix stripped
        Assert.Contains("ClientScript.RegisterClientScriptInclude", result);
        // Script block preserved with prefix stripped
        Assert.Contains("ClientScript.RegisterClientScriptBlock", result);
        // ScriptManager.RegisterStartupScript → ClientScript.RegisterStartupScript
        Assert.DoesNotContain("ScriptManager.RegisterStartupScript", result);
        // Postback → TODO with EventCallback
        Assert.Contains("@onclick", result);
        // ScriptManager.GetCurrent → TODO
        Assert.Contains("ScriptManager.GetCurrent() has no Blazor equivalent", result);
        // ClientScriptShim comment injected (not IJSRuntime)
        Assert.Contains("ClientScriptShim", result);
        Assert.DoesNotContain("[Inject] private IJSRuntime JS", result);
    }

    [Fact]
    public void PreservesContent_WithoutClientScript()
    {
        var input = @"namespace MyApp
{
    public partial class MyPage
    {
        void DoWork() { var x = 42; }
    }
}";
        var result = _transform.Apply(input, TestMetadata(input));

        Assert.Equal(input, result);
    }

    [Fact]
    public void DoesNotAddShimComment_WhenOnlyPostbackRef()
    {
        var input = @"namespace MyApp
{
    public partial class MyPage
    {
        void DoWork()
        {
            var postbackRef = Page.ClientScript.GetPostBackEventReference(btnSubmit, ""validate"");
        }
    }
}";
        var result = _transform.Apply(input, TestMetadata(input));

        Assert.DoesNotContain("ClientScriptShim", result);
    }

    [Fact]
    public void DoesNotDuplicateShimComment_WhenAlreadyPresent()
    {
        var input = @"namespace MyApp
{
    public partial class MyPage
    {
    // TODO(bwfc-general): ClientScript calls preserved — uses ClientScriptShim. Inject @inject ClientScriptShim ClientScript if not using BaseWebFormsComponent.
        void Page_Load()
        {
            Page.ClientScript.RegisterStartupScript(this.GetType(), ""key"", ""alert('a');"", true);
        }
    }
}";
        var result = _transform.Apply(input, TestMetadata(input));

        var shimCount = result.Split("ClientScript calls preserved").Length - 1;
        Assert.Equal(1, shimCount);
    }

    [Fact]
    public void OrderIs850()
    {
        Assert.Equal(850, _transform.Order);
    }

    #endregion
}
