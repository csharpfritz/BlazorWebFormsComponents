namespace BlazorWebFormsComponents.DataBinding
{
	/// <summary>
	/// Defines the delegate signature for the InsertMethod parameter on data-bound controls.
	/// This follows the ASP.NET Web Forms model binding pattern where the method receives
	/// the item to be inserted.
	/// </summary>
	/// <typeparam name="TItemType">The type of item being inserted.</typeparam>
	/// <param name="item">The item to insert.</param>
	public delegate void InsertHandler<TItemType>(TItemType item);
}
