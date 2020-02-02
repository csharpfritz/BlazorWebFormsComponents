using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MasterPages.Data
{

	public class LayoutSettings
	{

		private int _TheCount = 10;

		public Action StateHasChanged { get; set; }

		public int TheCount {
			get { return _TheCount; }
			set {
				_TheCount = value;
				StateHasChanged?.Invoke();
			}
		} 

		public string PageTitle { get; set; }

	}

}
