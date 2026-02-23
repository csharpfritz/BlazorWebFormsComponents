using BlazorWebFormsComponents.Interfaces;
using Microsoft.AspNetCore.Components;

namespace BlazorWebFormsComponents
{
	public partial class Label : BaseStyledComponent, ITextComponent
	{
		[Parameter]
		public string Text { get; set; }
	}
}
