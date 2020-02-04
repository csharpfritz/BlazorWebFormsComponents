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
		private IJSRuntime JsRuntime;

		public BasePageComponent()
		{
		}

		[Inject]
		public IJSRuntime InnerJsRuntime {
			get => JsRuntime;
			set
			{
				JsRuntime = value;// new ScheduledJsRuntime(value);
		 	}
		}

		/// <summary>
		/// The web page Title
		/// </summary>
		public string Title
		{
			get { return JsRuntime.InvokeAsync<string>("BlazorWebFormsComponents.GetPageTitle", new object[] { }).GetAwaiter().GetResult(); }
			set { JsRuntime.InvokeVoidAsync("BlazorWebFormsComponents.SetPageTitle", value); }
		}

		public ValueTask<string> GetTitle() {

			return JsRuntime.InvokeAsync<string>("BlazorWebFormsComponents.GetPageTitle");

		}


		protected override Task OnAfterRenderAsync(bool firstRender)
		{

			base.OnAfterRenderAsync(firstRender);

			if (firstRender)
			{
				//JsRuntime?.SetAfterRender(true);
			}

			return Task.CompletedTask;

		}


	}

}
