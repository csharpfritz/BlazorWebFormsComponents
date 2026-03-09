// Layer2-transformed
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;
using WingtipToys.Models;

namespace WingtipToys
{
    public partial class CheckoutReview : ComponentBase
    {
        [Inject] private IDbContextFactory<ProductContext> DbFactory { get; set; } = default!;

        private List<CartItem> _checkoutReviews = new();

        protected override async Task OnInitializedAsync()
        {
            using var db = DbFactory.CreateDbContext();
            // TODO: Customize query as needed
            _checkoutReviews = await db.ShoppingCartItems.ToListAsync();
        }
    }
}

