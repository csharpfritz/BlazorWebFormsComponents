using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;
using WingtipToys.Models;

namespace WingtipToys.Components.Layout
{
    public partial class MainLayout : LayoutComponentBase
    {
        [Inject] private IDbContextFactory<ProductContext> DbFactory { get; set; } = default!;
        [Inject] private IHttpContextAccessor HttpContextAccessor { get; set; } = default!;
        [Inject] private AuthenticationStateProvider AuthStateProvider { get; set; } = default!;

        private List<Category> _categories = new();
        private int _cartCount;
        private string _userName = "";

        protected override async Task OnInitializedAsync()
        {
            using var db = DbFactory.CreateDbContext();
            _categories = await db.Categories.OrderBy(c => c.CategoryID).ToListAsync();

            var cartId = HttpContextAccessor.HttpContext?.Session.GetString("CartId") ?? "";
            if (!string.IsNullOrEmpty(cartId))
            {
                _cartCount = db.ShoppingCartItems.Where(c => c.CartId == cartId).Sum(c => c.Quantity);
            }

            var authState = await AuthStateProvider.GetAuthenticationStateAsync();
            if (authState.User.Identity?.IsAuthenticated == true)
            {
                _userName = authState.User.Identity.Name ?? "";
            }
        }
    }
}
