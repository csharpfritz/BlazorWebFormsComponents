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
        // TODO(bwfc-general): This code-behind was copied from Web Forms and needs manual migration.
        //
        // Common transforms needed (use the BWFC Copilot skill for assistance):
        //   TODO(bwfc-lifecycle): Page_Load / Page_Init → OnInitializedAsync / OnParametersSetAsync
        //   TODO(bwfc-lifecycle): Page_PreRender → OnAfterRenderAsync
        //   TODO(bwfc-ispostback): IsPostBack checks → remove or convert to state logic
        //   TODO(bwfc-viewstate): ViewState usage → component [Parameter] or private fields
        //   TODO(bwfc-session-state): Session/Cache access → inject IHttpContextAccessor or use DI
        //   TODO(bwfc-navigation): Response.Redirect → NavigationManager.NavigateTo
        //   TODO(bwfc-general): Event handlers (Button_Click, etc.) → convert to Blazor event callbacks
        //   TODO(bwfc-datasource): Data binding (DataBind, DataSource) → component parameters or OnInitialized
        //   TODO(bwfc-general): ScriptManager code-behind references → remove (Blazor handles updates)
        //   TODO(bwfc-general): UpdatePanel markup preserved by BWFC (ContentTemplate supported) — remove only code-behind API calls
        //   TODO(bwfc-general): User controls → Blazor component references
        // =============================================================================

        """;

    public string Apply(string content, FileMetadata metadata)
    {
        return TodoHeader + content;
    }
}
