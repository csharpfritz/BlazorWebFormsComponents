namespace BlazorWebFormsComponents.DataBinding
{
	/// <summary>
	/// Defines the delegate signature for the DeleteMethod parameter on data-bound controls.
	/// This follows the ASP.NET Web Forms model binding pattern where the method receives
	/// the item to be deleted.
	/// </summary>
	/// <typeparam name="TItemType">The type of item being deleted.</typeparam>
	/// <param name="item">The item to delete.</param>
	public delegate void DeleteHandler<TItemType>(TItemType item);
}
