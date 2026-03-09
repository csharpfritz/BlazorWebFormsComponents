namespace BlazorWebFormsComponents.DataBinding
{
	/// <summary>
	/// Defines the delegate signature for the UpdateMethod parameter on data-bound controls.
	/// This follows the ASP.NET Web Forms model binding pattern where the method receives
	/// the item to be updated.
	/// </summary>
	/// <typeparam name="TItemType">The type of item being updated.</typeparam>
	/// <param name="item">The item to update.</param>
	public delegate void UpdateHandler<TItemType>(TItemType item);
}
