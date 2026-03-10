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
/// The component uses JavaScript interop to inject link elements into the document head.
/// Stylesheets are automatically removed when the component is disposed (e.g., on navigation).
/// </para>
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

    private string _linkId = "";
    private bool _isLoaded;
    private IJSObjectReference? _module;

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender && !string.IsNullOrEmpty(Href))
        {
            _linkId = Id ?? $"bwfc-css-{Guid.NewGuid():N}";
            
            try
            {
                _module = await JS.InvokeAsync<IJSObjectReference>(
                    "import", "./_content/Fritz.BlazorWebFormsComponents/js/Basepage.module.js");
                
                await _module.InvokeVoidAsync("loadStyleSheet", _linkId, Href, Media, Integrity, CrossOrigin);
                _isLoaded = true;
            }
            catch (InvalidOperationException)
            {
                // Prerendering or SSR-only mode - JS not available
                // The stylesheet won't be dynamically loaded, but that's OK for static scenarios
            }
        }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_isLoaded && _module is not null)
        {
            try
            {
                await _module.InvokeVoidAsync("unloadStyleSheet", _linkId);
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
                // Prerendering scenario
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
