using BlazorWebFormsComponents.Cli.Pipeline;
using BlazorWebFormsComponents.Cli.Transforms.CodeBehind;

namespace BlazorWebFormsComponents.Cli.Tests.TransformUnit;

/// <summary>
/// Unit tests for ClientScriptTransform — detects Page.ClientScript and ScriptManager
/// code-behind patterns and transforms automatable cases to IJSRuntime skeletons,
/// or emits TODO markers for manual migration.
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
    public void TC36_RegisterStartupScript_ConvertsToJSInterop()
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

        Assert.Contains("JS.InvokeVoidAsync(\"eval\"", result);
        Assert.Contains("alert('ready');", result);
        Assert.DoesNotContain("RegisterStartupScript", result);
    }

    [Fact]
    public void TC36_RegisterStartupScript_InjectsTodoReviewComment()
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
        Assert.Contains("eval()", result);
    }

    [Fact]
    public void TC36_RegisterStartupScript_InjectsIJSRuntimeProperty()
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

        Assert.Contains("[Inject] private IJSRuntime JS { get; set; }", result);
    }

    [Fact]
    public void TC36_ClientScriptWithoutPage_RegisterStartupScript_ConvertsToJSInterop()
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

        Assert.Contains("JS.InvokeVoidAsync(\"eval\"", result);
        Assert.DoesNotContain("RegisterStartupScript", result);
    }

    [Fact]
    public void TC36_ScriptManagerRegisterStartupScript_ConvertsToJSInterop()
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

        Assert.Contains("JS.InvokeVoidAsync(\"eval\"", result);
        Assert.Contains("alert('hello');", result);
        Assert.DoesNotContain("ScriptManager.RegisterStartupScript", result);
    }

    [Fact]
    public void TC36_MultipleStartupScripts_AllConverted()
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
        Assert.DoesNotContain("RegisterStartupScript", result);
    }

    #endregion

    #region TC37 — RegisterClientScriptInclude and RegisterClientScriptBlock

    [Fact]
    public void TC37_RegisterClientScriptInclude_EmitsTodoWithScriptTag()
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

        Assert.Contains("TODO(bwfc-general)", result);
        Assert.Contains("<script src=", result);
        Assert.Contains("Scripts/jquery-ui.min.js", result);
        Assert.DoesNotContain("RegisterClientScriptInclude", result);
    }

    [Fact]
    public void TC37_RegisterClientScriptInclude_StripsResolveUrlTildePrefix()
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

        // Should strip ~/ prefix
        Assert.Contains("lib/custom.js", result);
        Assert.DoesNotContain("~/lib/custom.js", result);
    }

    [Fact]
    public void TC37_RegisterClientScriptBlock_EmitsTodo()
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

        Assert.Contains("TODO(bwfc-general)", result);
        Assert.Contains("Move script block to IJSRuntime", result);
        Assert.DoesNotContain("RegisterClientScriptBlock", result);
    }

    [Fact]
    public void TC37_RegisterClientScriptBlock_MentionsMigrationGuide()
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

        Assert.Contains("ClientScriptMigrationGuide.md", result);
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

        // Startup scripts converted
        Assert.Contains("JS.InvokeVoidAsync", result);
        // Include → TODO with script tag
        Assert.Contains("<script src=", result);
        // Postback → TODO with EventCallback
        Assert.Contains("@onclick", result);
        // Block → TODO
        Assert.Contains("Move script block to IJSRuntime", result);
        // ScriptManager.GetCurrent → TODO
        Assert.Contains("ScriptManager.GetCurrent() has no Blazor equivalent", result);
        // IJSRuntime injected
        Assert.Contains("[Inject] private IJSRuntime JS", result);
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
    public void DoesNotInjectIJSRuntime_WhenNoStartupScripts()
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

        Assert.DoesNotContain("[Inject] private IJSRuntime JS", result);
    }

    [Fact]
    public void DoesNotDuplicateIJSRuntime_WhenAlreadyPresent()
    {
        var input = @"namespace MyApp
{
    public partial class MyPage
    {
    [Inject] private IJSRuntime JS { get; set; }
        void Page_Load()
        {
            Page.ClientScript.RegisterStartupScript(this.GetType(), ""key"", ""alert('a');"", true);
        }
    }
}";
        var result = _transform.Apply(input, TestMetadata(input));

        var injectCount = result.Split("[Inject] private IJSRuntime JS").Length - 1;
        Assert.Equal(1, injectCount);
    }

    [Fact]
    public void OrderIs850()
    {
        Assert.Equal(850, _transform.Order);
    }

    #endregion
}
