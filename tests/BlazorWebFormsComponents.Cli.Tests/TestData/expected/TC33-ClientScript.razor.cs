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
    [Inject] private IJSRuntime JS { get; set; } // TODO(bwfc-general): Add @using Microsoft.JSInterop to _Imports.razor if needed

        protected override async Task OnInitializedAsync()
        {
            // TODO(bwfc-lifecycle): Review lifecycle conversion — verify async behavior
            await base.OnInitializedAsync();

            // Pattern 1: RegisterStartupScript with inline script
            // TODO(bwfc-general): Review and refactor eval() usage — move script to a .js file and call via IJSRuntime
            await JS.InvokeVoidAsync("eval", @"$(function() { console.log('ready'); });");

            // Pattern 2: RegisterClientScriptInclude with URL
            // TODO(bwfc-general): Add <script src="Scripts/jquery-ui.min.js"/> to _Host.cshtml or App.razor

            // Pattern 3: GetPostBackEventReference
            // TODO(bwfc-general): Replace __doPostBack with @onclick or EventCallback. See ClientScriptMigrationGuide.md
            // Original: var postbackRef = Page.ClientScript.GetPostBackEventReference(btnSubmit, "validate");

            // Pattern 4: RegisterClientScriptBlock
            // TODO(bwfc-general): Move script block to IJSRuntime.InvokeVoidAsync or a .js file. See ClientScriptMigrationGuide.md

            // Pattern 5: ScriptManager.RegisterStartupScript
            // TODO(bwfc-general): Review and refactor eval() usage — move script to a .js file and call via IJSRuntime
            await JS.InvokeVoidAsync("eval", @"alert('hello');");

            // Pattern 6: ScriptManager.GetCurrent
            // TODO(bwfc-general): ScriptManager.GetCurrent() has no Blazor equivalent. Use IJSRuntime directly.
        }
    }
}
