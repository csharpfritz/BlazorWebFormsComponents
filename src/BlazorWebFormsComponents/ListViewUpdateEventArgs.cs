using System;
using System.Collections.Specialized;

namespace BlazorWebFormsComponents
{
	/// <summary>
	/// Provides data for the ItemUpdating event of the ListView control.
	/// </summary>
	public class ListViewUpdateEventArgs : EventArgs
	{
		public ListViewUpdateEventArgs(int itemIndex)
		{
			ItemIndex = itemIndex;
			Keys = new OrderedDictionary();
			OldValues = new OrderedDictionary();
			NewValues = new OrderedDictionary();
		}

		/// <summary>
		/// Gets the index of the item being updated.
		/// </summary>
		public int ItemIndex { get; }

		/// <summary>
		/// Gets or sets a value indicating whether the event should be cancelled.
		/// </summary>
		public bool Cancel { get; set; }

		/// <summary>
		/// Gets a dictionary of field name/value pairs that represent the key of the item to update.
		/// </summary>
		public IOrderedDictionary Keys { get; }

		/// <summary>
		/// Gets a dictionary that contains the original values of the item to update.
		/// </summary>
		public IOrderedDictionary OldValues { get; }

		/// <summary>
		/// Gets a dictionary that contains the revised values of the item to update.
		/// </summary>
		public IOrderedDictionary NewValues { get; }
	}
}
