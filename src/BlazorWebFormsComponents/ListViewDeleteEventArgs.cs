using System;
using System.Collections.Specialized;

namespace BlazorWebFormsComponents
{
	/// <summary>
	/// Provides data for the ItemDeleting event of the ListView control.
	/// </summary>
	public class ListViewDeleteEventArgs : EventArgs
	{
		public ListViewDeleteEventArgs(int itemIndex)
		{
			ItemIndex = itemIndex;
			Keys = new OrderedDictionary();
			Values = new OrderedDictionary();
		}

		/// <summary>
		/// Gets the index of the item being deleted.
		/// </summary>
		public int ItemIndex { get; }

		/// <summary>
		/// Gets or sets a value indicating whether the event should be cancelled.
		/// </summary>
		public bool Cancel { get; set; }

		/// <summary>
		/// Gets a dictionary of field name/value pairs that represent the key of the item to delete.
		/// </summary>
		public IOrderedDictionary Keys { get; }

		/// <summary>
		/// Gets a dictionary of the non-key field name/value pairs in the item to delete.
		/// </summary>
		public IOrderedDictionary Values { get; }
	}
}
