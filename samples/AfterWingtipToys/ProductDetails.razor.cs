using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using WingtipToys.Models;

namespace WingtipToys
{
    public partial class ProductDetails : ComponentBase
    {
        [Inject] private IDbContextFactory<ProductContext> DbFactory { get; set; } = default!;

        [SupplyParameterFromQuery(Name = "ProductID")]
        public int? ProductId { get; set; }

        private List<Product> _product = new();

        protected override async Task OnInitializedAsync()
        {
            if (ProductId.HasValue && ProductId > 0)
            {
                using var db = DbFactory.CreateDbContext();
                var item = await db.Products.FirstOrDefaultAsync(p => p.ProductID == ProductId);
                if (item != null)
                {
                    _product = new List<Product> { item };
                }
            }
        }
    }
}
