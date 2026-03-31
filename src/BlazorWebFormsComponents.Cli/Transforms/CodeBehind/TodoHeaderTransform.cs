using BlazorWebFormsComponents.Cli.Pipeline;

namespace BlazorWebFormsComponents.Cli.Transforms.CodeBehind;

/// <summary>
/// Injects the TODO migration guidance header at the top of code-behind files.
/// Must run first so other transforms can reference the header marker.
/// </summary>
public class TodoHeaderTransform : ICodeBehindTransform
{
    public string Name => "TodoHeader";
    public int Order => 10;

    private const string TodoHeader = """
        // =============================================================================
        // TODO: This code-behind was copied from Web Forms and needs manual migration.
        //
        // Common transforms needed (use the BWFC Copilot skill for assistance):
        //   - Page_Load / Page_Init → OnInitializedAsync / OnParametersSetAsync
        //   - Page_PreRender → OnAfterRenderAsync
        //   - IsPostBack checks → remove or convert to state logic
        //   - ViewState usage → component [Parameter] or private fields
        //   - Session/Cache access → inject IHttpContextAccessor or use DI
        //   - Response.Redirect → NavigationManager.NavigateTo
        //   - Event handlers (Button_Click, etc.) → convert to Blazor event callbacks
        //   - Data binding (DataBind, DataSource) → component parameters or OnInitialized
        //   - ScriptManager code-behind references → remove (Blazor handles updates)
        //   - UpdatePanel markup preserved by BWFC (ContentTemplate supported) — remove only code-behind API calls
        //   - User controls → Blazor component references
        // =============================================================================

        """;

    public string Apply(string content, FileMetadata metadata)
    {
        return TodoHeader + content;
    }
}
