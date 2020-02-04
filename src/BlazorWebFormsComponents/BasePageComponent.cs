using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace BlazorWebFormsComponents
{

	public abstract class BasePageComponent : ComponentBase
	{

		private Queue<Action> _Scheduler = new Queue<Action>();
		private ScheduledJsRuntime JsRuntime;

		public BasePageComponent()
		{
		}

		[Inject]
		public IJSRuntime InnerJsRuntime {
			get => JsRuntime;
			set
			{
				JsRuntime = new ScheduledJsRuntime(value);
			}
		}

		/// <summary>
		/// The web page Title
		/// </summary>
		public string Title
		{
			get { return JsRuntime.InvokeAsync<string>("BlazorWebFormsComponents.GetPageTitle").GetAwaiter().GetResult(); }
			set { JsRuntime.InvokeVoidAsync("BlazorWebFormsComponents.SetPageTitle", value); }
		}

		protected override Task OnAfterRenderAsync(bool firstRender)
		{

			base.OnAfterRenderAsync(firstRender);

			JsRuntime?.SetAfterRender(true);

			return Task.CompletedTask;

		}


	}

}
