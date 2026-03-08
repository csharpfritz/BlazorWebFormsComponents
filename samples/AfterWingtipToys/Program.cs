using BlazorWebFormsComponents;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using WingtipToys.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents();

builder.Services.AddBlazorWebFormsComponents();

// Database
builder.Services.AddDbContextFactory<ProductContext>(options =>
    options.UseSqlite("Data Source=wingtiptoys.db"));
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite("Data Source=wingtiptoys.db"));

// Identity
builder.Services.AddDefaultIdentity<IdentityUser>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;
    options.Password.RequireDigit = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequireUppercase = true;
    options.Password.RequiredLength = 6;
})
.AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.AddCascadingAuthenticationState();

// Session for cart
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});
builder.Services.AddHttpContextAccessor();

var app = builder.Build();

// Seed the database
using (var scope = app.Services.CreateScope())
{
    var dbFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<ProductContext>>();
    using var db = dbFactory.CreateDbContext();
    db.Database.EnsureCreated();
    ProductDatabaseInitializer.Seed(db);

    var identityDb = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    identityDb.Database.EnsureCreated();
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapRazorComponents<WingtipToys.Components.App>();

// Minimal API: Add to cart
app.MapGet("/AddToCart", async (HttpContext ctx, int productID, IDbContextFactory<ProductContext> dbFactory) =>
{
    var cartId = ctx.Session.GetString("CartId");
    if (string.IsNullOrEmpty(cartId))
    {
        cartId = Guid.NewGuid().ToString();
        ctx.Session.SetString("CartId", cartId);
    }

    using var db = dbFactory.CreateDbContext();
    var existingItem = db.ShoppingCartItems
        .FirstOrDefault(c => c.CartId == cartId && c.ProductId == productID);

    if (existingItem != null)
    {
        existingItem.Quantity++;
    }
    else
    {
        db.ShoppingCartItems.Add(new CartItem
        {
            ItemId = Guid.NewGuid().ToString(),
            CartId = cartId,
            ProductId = productID,
            Quantity = 1,
            DateCreated = DateTime.Now
        });
    }
    db.SaveChanges();
    return Results.Redirect("/ShoppingCart");
});

// Minimal API: Remove from cart
app.MapPost("/api/remove-from-cart", async (HttpContext ctx, IDbContextFactory<ProductContext> dbFactory) =>
{
    var form = await ctx.Request.ReadFormAsync();
    var itemId = form["itemId"].ToString();
    var cartId = ctx.Session.GetString("CartId") ?? "";

    using var db = dbFactory.CreateDbContext();
    var item = db.ShoppingCartItems.FirstOrDefault(c => c.ItemId == itemId && c.CartId == cartId);
    if (item != null)
    {
        db.ShoppingCartItems.Remove(item);
        db.SaveChanges();
    }
    return Results.Redirect("/ShoppingCart");
});

// Minimal API: Update cart quantity
app.MapPost("/api/update-cart", async (HttpContext ctx, IDbContextFactory<ProductContext> dbFactory) =>
{
    var form = await ctx.Request.ReadFormAsync();
    var cartId = ctx.Session.GetString("CartId") ?? "";

    using var db = dbFactory.CreateDbContext();
    foreach (var key in form.Keys.Where(k => k.StartsWith("qty-")))
    {
        var itemId = key.Substring(4);
        if (int.TryParse(form[key], out var qty) && qty > 0)
        {
            var item = db.ShoppingCartItems.FirstOrDefault(c => c.ItemId == itemId && c.CartId == cartId);
            if (item != null)
            {
                item.Quantity = qty;
            }
        }
    }
    db.SaveChanges();
    return Results.Redirect("/ShoppingCart");
});

// Minimal API: Logout
app.MapGet("/account/logout", async (HttpContext ctx, SignInManager<IdentityUser> signInManager) =>
{
    await signInManager.SignOutAsync();
    return Results.Redirect("/");
});

app.Run();

