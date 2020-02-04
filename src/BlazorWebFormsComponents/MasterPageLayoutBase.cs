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


		protected override Task OnInitializedAsync()
		{
			Settings.StateHasChanged = StateHasChanged;
			Settings.PropertyChanged += Settings_PropertyChanged;

			return base.OnInitializedAsync();
		}

		protected virtual void Settings_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
		{
		}
	}

}
