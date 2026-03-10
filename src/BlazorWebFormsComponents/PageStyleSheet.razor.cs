using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System;
using System.Threading.Tasks;

namespace BlazorWebFormsComponents;

/// <summary>
/// Dynamically loads a CSS stylesheet when the component renders and unloads it on dispose.
/// 
/// Use this component to include page-specific CSS that should not persist across navigation.
/// This is the Blazor equivalent of Web Forms' ContentPlaceHolder pattern for CSS:
/// <code>
/// &lt;asp:Content ContentPlaceHolderID="head"&gt;
///     &lt;link href="CSS/CSS_Courses.css" rel="stylesheet" /&gt;
/// &lt;/asp:Content&gt;
/// </code>
/// 
/// Becomes:
/// <code>
/// &lt;PageStyleSheet Href="CSS/CSS_Courses.css" /&gt;
/// </code>
/// </summary>
/// <remarks>
/// <para>
/// Unlike Blazor's built-in <c>HeadContent</c> which only works in page components (not layouts),
/// <c>PageStyleSheet</c> can be used anywhere and properly cleans up on dispose.
/// </para>
/// <para>
/// <strong>Render Mode Behavior:</strong>
/// </para>
/// <list type="bullet">
/// <item><description>
/// <strong>Static SSR:</strong> Renders a static <c>&lt;link&gt;</c> tag directly into the HTML stream.
/// No JavaScript interop is used, and the stylesheet persists until full-page navigation.
/// </description></item>
/// <item><description>
/// <strong>Prerendering:</strong> Renders a static <c>&lt;link&gt;</c> tag during prerender. After hydration
/// to interactive mode, JavaScript manages the stylesheet lifecycle.
/// </description></item>
/// <item><description>
/// <strong>InteractiveServer/WebAssembly:</strong> Uses JavaScript interop to dynamically load the
/// stylesheet and remove it when the component disposes (e.g., on navigation).
/// </description></item>
/// </list>
/// <para>
/// <strong>Registry-Based Lifecycle:</strong>
/// </para>
/// <para>
/// CSS lifecycle is managed by a reference-counting registry in JavaScript. A stylesheet persists
/// as long as at least one PageStyleSheet component references it:
/// </para>
/// <list type="bullet">
/// <item><description>Layout CSS persists because the layout component stays alive across navigations.</description></item>
/// <item><description>Page CSS swaps on navigation (old page unregisters, new page registers).</description></item>
/// <item><description>Shared CSS (multiple components) persists until the last component unregisters.</description></item>
/// </list>
/// </remarks>
public partial class PageStyleSheet : ComponentBase, IAsyncDisposable
{
    [Inject]
    private IJSRuntime JS { get; set; } = null!;

    /// <summary>
    /// The URL of the stylesheet to load. Required.
    /// Can be a relative path (e.g., "CSS/page.css") or absolute URL.
    /// </summary>
    [Parameter, EditorRequired]
    public string Href { get; set; } = "";

    /// <summary>
    /// Optional media query for the stylesheet (e.g., "screen", "print", "(max-width: 600px)").
    /// </summary>
    [Parameter]
    public string? Media { get; set; }

    /// <summary>
    /// Optional ID for the link element. If not provided, a unique ID is generated.
    /// Use this if you need to reference the stylesheet element from JavaScript.
    /// </summary>
    [Parameter]
    public string? Id { get; set; }

    /// <summary>
    /// Optional integrity hash for subresource integrity (SRI) when loading from CDN.
    /// </summary>
    [Parameter]
    public string? Integrity { get; set; }

    /// <summary>
    /// Optional crossorigin attribute. Typically "anonymous" when using integrity.
    /// </summary>
    [Parameter]
    public string? CrossOrigin { get; set; }

    private string _componentId = "";
    private bool _registered;
    private IJSObjectReference? _module;

    /// <summary>
    /// Gets the component/link element ID, generating one if needed.
    /// Called from both .razor markup and code-behind.
    /// </summary>
    private string GetLinkId()
    {
        if (string.IsNullOrEmpty(_componentId))
        {
            _componentId = Id ?? $"bwfc-css-{Guid.NewGuid():N}";
        }
        return _componentId;
    }

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender && !string.IsNullOrEmpty(Href))
        {
            // Ensure we have a component ID (may already be set from static render)
            GetLinkId();
            
            try
            {
                _module = await JS.InvokeAsync<IJSObjectReference>(
                    "import", "./_content/Fritz.BlazorWebFormsComponents/js/Basepage.module.js");
                
                // Check if we're in SSR-to-interactive transition (static link may exist)
                // The registry handles adoption of existing links automatically
                if (!RendererInfo.IsInteractive)
                {
                    // During prerender, just adopt the static link we rendered
                    await _module.InvokeVoidAsync("adoptStyleSheet", _componentId, _componentId, Href);
                }
                else
                {
                    // Interactive mode: register with the global stylesheet registry
                    // This either adopts existing link from SSR or creates new one
                    await _module.InvokeVoidAsync("registerStyleSheet", _componentId, Href, Media, Integrity, CrossOrigin);
                }
                
                _registered = true;
            }
            catch (InvalidOperationException)
            {
                // Prerendering or SSR-only mode - JS not available
                // The stylesheet is already rendered via the static <link> tag in the .razor file
            }
        }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        // Only unregister if we successfully registered with the JS registry.
        // The registry handles reference counting and deferred cleanup.
        if (_registered && _module is not null)
        {
            try
            {
                // Unregister from registry (may trigger deferred cleanup after 100ms)
                await _module.InvokeVoidAsync("unregisterStyleSheet", _componentId, Href);
            }
            catch (JSDisconnectedException)
            {
                // Circuit disconnected - stylesheet will be cleaned up on page reload
            }
            catch (ObjectDisposedException)
            {
                // Runtime already disposed
            }
            catch (InvalidOperationException)
            {
                // Prerendering scenario - shouldn't happen but guard anyway
            }
        }

        if (_module is not null)
        {
            try
            {
                await _module.DisposeAsync();
            }
            catch
            {
                // Ignore disposal errors
            }
        }
    }
}
