using System;
using System.Collections.Specialized;

namespace BlazorWebFormsComponents
{
	/// <summary>
	/// Provides data for the ItemInserting event of the ListView control.
	/// </summary>
	public class ListViewInsertEventArgs : EventArgs
	{
		public ListViewInsertEventArgs()
		{
			Values = new OrderedDictionary();
		}

		/// <summary>
		/// Gets or sets a value indicating whether the event should be cancelled.
		/// </summary>
		public bool Cancel { get; set; }

		/// <summary>
		/// Gets the item to be inserted.
		/// </summary>
		public object Item { get; set; }

		/// <summary>
		/// Gets the values for the record to insert.
		/// </summary>
		public IOrderedDictionary Values { get; }
	}
}
