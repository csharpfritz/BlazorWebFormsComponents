using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using System.Text;
using System.Threading.Tasks;

namespace BlazorWebFormsComponents
{

	public class MasterPageLayoutBase : LayoutComponentBase
	{

		[CascadingParameter()]
		public MasterPageSettings Settings { get; set; } = new MasterPageSettings();

		protected override void BuildRenderTree(RenderTreeBuilder builder)
		{
			base.BuildRenderTree(builder);
		}

		protected override Task OnInitializedAsync()
		{
			Settings.StateHasChanged = StateHasChanged;

			return base.OnInitializedAsync();
		}

	}

}
