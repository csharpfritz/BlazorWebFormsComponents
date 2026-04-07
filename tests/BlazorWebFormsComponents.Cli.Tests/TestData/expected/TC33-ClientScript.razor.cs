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
using System;

namespace MyApp
{
    public partial class TC33_ClientScript
    {
    // TODO(bwfc-general): ClientScript calls preserved — uses ClientScriptShim + ScriptManagerShim. Inject @inject ClientScriptShim ClientScript and @inject ScriptManagerShim ScriptManager if not using BaseWebFormsComponent.

        protected override async Task OnInitializedAsync()
        {
            // TODO(bwfc-lifecycle): Review lifecycle conversion — verify async behavior
            await base.OnInitializedAsync();

            // Pattern 1: RegisterStartupScript with inline script
            ClientScript.RegisterStartupScript(
                this.GetType(),
                "InitializeUI",
                "$(function() { console.log('ready'); });",
                true);

            // Pattern 2: RegisterClientScriptInclude with URL
            ClientScript.RegisterClientScriptInclude(
                "jqueryUI",
                ResolveUrl("~/Scripts/jquery-ui.min.js"));

            // Pattern 3: GetPostBackEventReference
            var postbackRef = ClientScript.GetPostBackEventReference(btnSubmit, "validate");

            // Pattern 4: RegisterClientScriptBlock
            ClientScript.RegisterClientScriptBlock(this.GetType(), "block1", "<script>var x = 1;</script>");

            // Pattern 5: ScriptManager.RegisterStartupScript
            ClientScript.RegisterStartupScript(this.GetType(), "smScript", "alert('hello');", true);

            // Pattern 6: ScriptManager.GetCurrent
            var sm = ScriptManager.GetCurrent(this);
        }
    }
}
