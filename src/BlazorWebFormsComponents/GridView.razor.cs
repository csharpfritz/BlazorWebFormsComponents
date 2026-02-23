using BlazorWebFormsComponents.DataBinding;
using BlazorWebFormsComponents.Enums;
using BlazorWebFormsComponents.Interfaces;
using Microsoft.AspNetCore.Components;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BlazorWebFormsComponents
{
	/// <summary>
	/// Blazor version of WebForms GridView control
	/// </summary>
	/// <typeparam name="ItemType"></typeparam>
	public partial class GridView<ItemType> : DataBoundComponent<ItemType>, IRowCollection<ItemType>, IColumnCollection<ItemType>
	{

		/// <summary>
		///	Specify if the GridView component will autogenerate its columns
		/// </summary>
		[Parameter] public bool AutoGenerateColumns { get; set; } = true;

		/// <summary>
		/// Text to show when there are no items to show
		/// </summary>
		[Parameter] public string EmptyDataText { get; set; }

		/// <summary>
		/// Not supported yet
		/// </summary>
		[Parameter] public string DataKeyNames { get; set; }

		/// <summary>
		/// Enables or disables sorting for the GridView
		/// </summary>
		[Parameter] public bool AllowSorting { get; set; }

		/// <summary>
		/// The current sort direction
		/// </summary>
		[Parameter] public SortDirection SortDirection { get; set; } = SortDirection.Ascending;

		/// <summary>
		/// The current sort expression (column name)
		/// </summary>
		[Parameter] public string SortExpression { get; set; }

		/// <summary>
		/// Fires before sort is applied. Can be cancelled.
		/// </summary>
		[Parameter] public EventCallback<GridViewSortEventArgs> Sorting { get; set; }

		/// <summary>
		/// Fires after sort is applied
		/// </summary>
		[Parameter] public EventCallback<GridViewSortEventArgs> Sorted { get; set; }

		///<inheritdoc/>
		public List<IColumn<ItemType>> ColumnList { get; set; } = new List<IColumn<ItemType>>();

		/// <summary>
		/// The Rows of the GridView
		/// </summary>
		public List<IRow<ItemType>> Rows { get => RowList; set => RowList = value; }

		///<inheritdoc/>
		public List<IRow<ItemType>> RowList { get; set; } = new List<IRow<ItemType>>();

		#region Templates
		/// <summary>
		/// The columns template of the GridView
		/// </summary>
		[Parameter] public RenderFragment Columns { get; set; }

		/// <summary>
		/// The ChildContent of the GridView
		/// </summary>
		[Parameter] public RenderFragment ChildContent { get; set; }
		#endregion
		protected override void OnInitialized()
		{
			base.OnInitialized();
			if (AutoGenerateColumns)
			{
				GridViewColumnGenerator.GenerateColumns(this);
			}
		}

		[Parameter]
		public EventCallback<GridViewCommandEventArgs> OnRowCommand { get; set; }

		/// <summary>
		/// Initiates a sort operation for the specified sort expression
		/// </summary>
		internal async Task Sort(string sortExpression)
		{
			var newDirection = (sortExpression == SortExpression && SortDirection == SortDirection.Ascending)
				? SortDirection.Descending
				: SortDirection.Ascending;

			var args = new GridViewSortEventArgs(sortExpression, newDirection);
			await Sorting.InvokeAsync(args);
			if (args.Cancel) return;

			SortExpression = args.SortExpression;
			SortDirection = args.SortDirection;
			await Sorted.InvokeAsync(args);
			StateHasChanged();
		}

		///<inheritdoc/>
		public void AddColumn(IColumn<ItemType> column)
		{
			ColumnList.Add(column);
			StateHasChanged();
		}

		///<inheritdoc/>
		public void RemoveColumn(IColumn<ItemType> column)
		{
			ColumnList.Remove(column);
			StateHasChanged();
		}

		///<inheritdoc/>
		public void RemoveRow(IRow<ItemType> row)
		{
			Rows.Remove(row);
			StateHasChanged();
		}

		///<inheritdoc/>
		public void AddRow(IRow<ItemType> row)
		{
			Rows.Add(row);
			StateHasChanged();
		}

	}
}
