---
name: bwfc-identity-migration
description: "Migrate ASP.NET Web Forms Identity, Membership, and FormsAuthentication to Blazor Server Identity. Covers OWIN to ASP.NET Core middleware, login page migration, BWFC login controls, role-based authorization, cookie auth under Interactive Server mode, Membership provider migration, and Roles provider patterns. WHEN: \"migrate identity\", \"login page migration\", \"OWIN to core\", \"cookie auth blazor\", \"LoginView migration\", \"Membership to Identity\", \"FormsAuthentication\"."
confidence: low
version: 2.0.0
---

# Web Forms Identity → Blazor Identity Migration

This skill covers migrating ASP.NET Web Forms authentication (Identity, Membership, FormsAuthentication) to Blazor Server using ASP.NET Core Identity. This is a **Layer 2 (L2) contextual transform** that requires understanding authentication patterns and database schema.

**Related skills:**
- `/bwfc-migration` — Core markup migration (controls, expressions, layouts)
- `/bwfc-data-migration` — EF6 → EF Core, data access, architecture decisions

---

## Overview

Web Forms authentication typically uses one of three systems. The migration path depends on which one:

| Web Forms Auth System | Era | Blazor Migration Path | Complexity |
|----------------------|-----|----------------------|-----------|
| ASP.NET Identity (OWIN) | 2013+ | ASP.NET Core Identity (closest match) | 🟢 Easy |
| ASP.NET Membership | 2005-2013 | ASP.NET Core Identity (schema migration required) | 🟡 Medium |
| FormsAuthentication | 2002-2005 | ASP.NET Core Identity or cookie auth | 🟡 Medium |

---

## ⚠️ Cookie Auth Under Interactive Server Mode

> **CRITICAL:** When using `<Routes @rendermode="InteractiveServer" />` (global interactive server mode), `HttpContext` is **NULL** during WebSocket circuits. This means cookie-based authentication operations — login, register, logout — **cannot** be performed via Blazor component event handlers (e.g., `@onclick`). They will silently fail: no exception is thrown, but no cookie is set.

**Why this happens:** After the initial HTTP request, Blazor Server communicates over a WebSocket (SignalR circuit). There is no HTTP response to attach a `Set-Cookie` header to. `SignInAsync()` called inside a component event handler has no `HttpContext.Response` to write the cookie.

**Required pattern:** Use standard HTML `<form method="post">` elements that submit to **minimal API endpoints** via full HTTP POST requests. The endpoint performs the auth operation and redirects back to a Blazor page.

```razor
@* Login.razor — form posts to a minimal API endpoint, NOT a Blazor event handler *@
<form method="post" action="/Account/LoginHandler">
    <div>
        <label>Email</label>
        <input type="email" name="email" required />
    </div>
    <div>
        <label>Password</label>
        <input type="password" name="password" required />
    </div>
    <button type="submit">Log in</button>
</form>
```

```csharp
// Program.cs — minimal API endpoint handles the actual SignInAsync()
app.MapPost("/Account/LoginHandler", async (HttpContext context, SignInManager<IdentityUser> signInManager) =>
{
    var form = await context.Request.ReadFormAsync();
    var email = form["email"].ToString();
    var password = form["password"].ToString();
    var result = await signInManager.PasswordSignInAsync(email, password, isPersistent: false, lockoutOnFailure: false);
    return result.Succeeded
        ? Results.Redirect("/")
        : Results.Redirect("/Account/Login?error=Invalid+login+attempt");
}).DisableAntiforgery();
```

**Key points:**
- The `<form>` submits a standard HTTP POST — this is a full page navigation, not a Blazor event
- The minimal API endpoint has a real `HttpContext` with a real HTTP response → cookies work
- After the operation, the endpoint redirects back to a Blazor page
- This pattern applies to **all** cookie-writing operations: login, register, logout, external auth callbacks
- This is **NOT optional** — without it, auth silently fails

> **Important:** The endpoint MUST call `.DisableAntiforgery()` because Blazor's HTML rendering does not include antiforgery tokens in `<form>` elements. See [DisableAntiforgery Requirement](#disableantiforgery-requirement) below.

---

## Step 1: Add Identity Packages

```bash
dotnet add package Microsoft.AspNetCore.Identity.EntityFrameworkCore
dotnet add package Microsoft.AspNetCore.Identity.UI
```

## Step 2: Configure Identity in Program.cs

```csharp
// Program.cs
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;
    options.Password.RequiredLength = 6;
})
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

// Required for Blazor Server
builder.Services.AddCascadingAuthenticationState();

// Middleware pipeline (ORDER MATTERS)
app.UseAuthentication();
app.UseAuthorization();
```

## Step 3: Create ApplicationUser and DbContext

```csharp
// Models/ApplicationUser.cs
using Microsoft.AspNetCore.Identity;

public class ApplicationUser : IdentityUser
{
    // Add custom properties from your Web Forms ApplicationUser
}

// Data/ApplicationDbContext.cs
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options) { }
}
```

## Step 4: Migrate the Database Schema

If migrating from ASP.NET Identity (OWIN), the schema is similar but not identical:

```bash
dotnet ef migrations add IdentityMigration
dotnet ef database update
```

**Key schema differences:**

| ASP.NET Identity (OWIN) | ASP.NET Core Identity |
|--------------------------|----------------------|
| `AspNetUsers.Id` (string GUID) | Same |
| `AspNetUsers.PasswordHash` | Same format — passwords are compatible |
| `AspNetUserClaims` | Same |
| `AspNetUserRoles` | Same |
| `AspNetRoles` | Same |
| `__MigrationHistory` | `__EFMigrationsHistory` |

> **Important:** ASP.NET Identity v2 password hashes (from Web Forms) are compatible with ASP.NET Core Identity. Users will NOT need to reset passwords.

If migrating from **Membership** (older):
- Use the `Microsoft.AspNetCore.Identity.MicrosoftAccountMigration` or a custom SQL migration script
- Membership password hashes are NOT compatible — users will need password resets

---

## Step 5: Migrate Login Pages

### BWFC Login Controls

BWFC provides login controls that match Web Forms markup. These controls use native Blazor `AuthenticationStateProvider` internally — they are **not shims** and do not need to be replaced with `AuthorizeView`.

> **Important:** BWFC login controls require `builder.Services.AddBlazorWebFormsComponents()` in `Program.cs` and `builder.Services.AddCascadingAuthenticationState()` for authentication state propagation.

| Web Forms | BWFC | Notes |
|-----------|------|-------|
| `<asp:Login runat="server" />` | `<Login />` | Login form with username/password |
| `<asp:LoginName runat="server" />` | `<LoginName />` | Displays authenticated username |
| `<asp:LoginStatus runat="server" />` | `<LoginStatus />` | Login/Logout toggle link |
| `<asp:LoginView runat="server">` | `<LoginView>` | Shows different content for anon vs auth users |
| `<asp:CreateUserWizard runat="server" />` | `<CreateUserWizard />` | Registration form |
| `<asp:ChangePassword runat="server" />` | `<ChangePassword />` | Password change form |
| `<asp:PasswordRecovery runat="server" />` | `<PasswordRecovery />` | Password reset flow |

### Login Page Migration Example

```html
<!-- Web Forms — Login.aspx -->
<%@ Page Title="Log in" MasterPageFile="~/Site.Master" ... %>
<asp:Content ContentPlaceHolderID="MainContent" runat="server">
    <h2>Log in</h2>
    <asp:Login ID="LoginCtrl" runat="server"
        ViewStateMode="Disabled"
        RenderOuterTable="false">
        <LayoutTemplate>
            <asp:TextBox ID="UserName" runat="server" CssClass="form-control" />
            <asp:RequiredFieldValidator ControlToValidate="UserName"
                ErrorMessage="Required" runat="server" />
            <asp:TextBox ID="Password" TextMode="Password" runat="server" />
            <asp:Button Text="Log in" CommandName="Login" runat="server" />
        </LayoutTemplate>
    </asp:Login>
</asp:Content>
```

```razor
@* Blazor — Login.razor *@
@page "/Account/Login"

<h2>Log in</h2>
<Login RenderOuterTable="false">
    <LayoutTemplate>
        <TextBox @bind-Text="model.UserName" CssClass="form-control" />
        <RequiredFieldValidator ControlToValidate="UserName" ErrorMessage="Required" />
        <TextBox TextMode="Password" @bind-Text="model.Password" />
        <Button Text="Log in" CommandName="Login" />
    </LayoutTemplate>
</Login>
```

---

## Step 6: Migrate Authorization Patterns

### Web.config Authorization → Blazor Authorization

```xml
<!-- Web Forms — Web.config -->
<location path="Admin">
    <system.web>
        <authorization>
            <allow roles="Administrator" />
            <deny users="*" />
        </authorization>
    </system.web>
</location>
```

```razor
@* Blazor — Admin pages use [Authorize] attribute *@
@page "/Admin"
@attribute [Authorize(Roles = "Administrator")]

<h1>Admin Panel</h1>
```

### LoginView Conditional Content

```html
<!-- Web Forms -->
<asp:LoginView runat="server">
    <AnonymousTemplate>
        <a href="~/Account/Login">Log in</a>
    </AnonymousTemplate>
    <LoggedInTemplate>
        Welcome, <asp:LoginName runat="server" />!
        <asp:LoginStatus runat="server" LogoutAction="Redirect" LogoutPageUrl="~/" />
    </LoggedInTemplate>
</asp:LoginView>
```

```razor
@* Blazor — BWFC LoginView (recommended — preserves markup and uses AuthenticationStateProvider natively) *@
<LoginView>
    <AnonymousTemplate>
        <a href="/Account/Login">Log in</a>
    </AnonymousTemplate>
    <LoggedInTemplate>
        Welcome, <LoginName />!
        <LoginStatus LogoutAction="Redirect" LogoutPageUrl="/" />
    </LoggedInTemplate>
</LoginView>
```

> **Note:** BWFC's `LoginView` internally injects `AuthenticationStateProvider` and evaluates authentication state natively — it is NOT a shim that needs to be replaced. Keep it for markup compatibility. If you prefer native Blazor syntax long-term:

```razor
@* Blazor — Native AuthorizeView (alternative — different template names) *@
<AuthorizeView>
    <NotAuthorized>
        <a href="/Account/Login">Log in</a>
    </NotAuthorized>
    <Authorized>
        Welcome, @context.User.Identity?.Name!
        <a href="/Account/Logout">Log out</a>
    </Authorized>
</AuthorizeView>
```

> ⚠️ **NEVER replace LoginView with AuthorizeView.** The BWFC `LoginView` component is a fully functional Blazor component that injects `AuthenticationStateProvider` natively. It uses the SAME template names as `asp:LoginView` (`AnonymousTemplate`, `LoggedInTemplate`). The migration script handles this automatically — do NOT convert to `AuthorizeView` manually. Use `AuthorizeView` only when you need Blazor-native role-based content (`<AuthorizeView Roles="...">`) with no Web Forms equivalent.

### Role-Based Content

```html
<!-- Web Forms -->
<asp:LoginView runat="server">
    <RoleGroups>
        <asp:RoleGroup Roles="Administrator">
            <ContentTemplate><a href="~/Admin">Admin Panel</a></ContentTemplate>
        </asp:RoleGroup>
    </RoleGroups>
</asp:LoginView>
```

```razor
@* Blazor *@
<AuthorizeView Roles="Administrator">
    <a href="/Admin">Admin Panel</a>
</AuthorizeView>
```

---

## Step 7: Migrate Authentication State Access

### Code-Behind Auth Patterns

```csharp
// Web Forms — code-behind
if (HttpContext.Current.User.Identity.IsAuthenticated)
{
    var userName = HttpContext.Current.User.Identity.Name;
    var isAdmin = HttpContext.Current.User.IsInRole("Administrator");
}

// Web Forms — OWIN
var manager = Context.GetOwinContext().GetUserManager<ApplicationUserManager>();
var user = manager.FindById(User.Identity.GetUserId());
```

```csharp
// Blazor — inject auth state
@inject AuthenticationStateProvider AuthStateProvider

@code {
    private string? userName;
    private bool isAdmin;

    protected override async Task OnInitializedAsync()
    {
        var authState = await AuthStateProvider.GetAuthenticationStateAsync();
        var user = authState.User;
        userName = user.Identity?.Name;
        isAdmin = user.IsInRole("Administrator");
    }
}
```

### Cascading Auth Parameter (Simpler)

```csharp
// Blazor — cascading parameter (requires AddCascadingAuthenticationState)
[CascadingParameter]
private Task<AuthenticationState>? AuthState { get; set; }

protected override async Task OnInitializedAsync()
{
    if (AuthState != null)
    {
        var state = await AuthState;
        var user = state.User;
    }
}
```

---

## OWIN Middleware → ASP.NET Core Middleware

| Web Forms (OWIN Startup.cs) | Blazor (Program.cs) |
|------------------------------|---------------------|
| `app.UseCookieAuthentication(...)` | `builder.Services.AddAuthentication().AddCookie(...)` |
| `app.UseExternalSignInCookie(...)` | `builder.Services.AddAuthentication().AddGoogle/Facebook(...)` |
| `ConfigureAuth(app)` in `Startup.Auth.cs` | Configuration in `Program.cs` services section |
| `app.CreatePerOwinContext<ApplicationUserManager>(...)` | `builder.Services.AddIdentity<ApplicationUser, IdentityRole>()` |
| `SecurityStampValidator.OnValidateIdentity(...)` | Built into ASP.NET Core Identity automatically |

---

## Ready-to-Use Endpoint Templates

These minimal API endpoints handle authentication operations that **must** run over HTTP (not WebSocket). Add them to `Program.cs` after building the app but before `app.Run()`. Each endpoint reads form data, performs the auth operation, and redirects back to a Blazor page.

> **Important:** Every endpoint below calls `.DisableAntiforgery()` because Blazor-rendered `<form>` elements do not include antiforgery tokens. See [DisableAntiforgery Requirement](#disableantiforgery-requirement).

### Login Endpoint

```csharp
// Program.cs — authenticates user and sets auth cookie via HTTP response
app.MapPost("/Account/LoginHandler", async (HttpContext context, SignInManager<IdentityUser> signInManager) =>
{
    var form = await context.Request.ReadFormAsync();
    var email = form["email"].ToString();
    var password = form["password"].ToString();

    var result = await signInManager.PasswordSignInAsync(email, password, isPersistent: false, lockoutOnFailure: false);
    if (result.Succeeded)
        return Results.Redirect("/");
    return Results.Redirect("/Account/Login?error=Invalid+login+attempt");
}).DisableAntiforgery();
```

**Blazor form that submits to this endpoint:**

```razor
@page "/Account/Login"

<h2>Log in</h2>
@if (!string.IsNullOrEmpty(errorMessage))
{
    <div class="text-danger">@errorMessage</div>
}
<form method="post" action="/Account/LoginHandler">
    <div>
        <label>Email</label>
        <input type="email" name="email" required />
    </div>
    <div>
        <label>Password</label>
        <input type="password" name="password" required />
    </div>
    <button type="submit">Log in</button>
</form>

@code {
    [SupplyParameterFromQuery] public string? Error { get; set; }
    private string? errorMessage;

    protected override void OnInitialized()
    {
        errorMessage = Error;
    }
}
```

### Register Endpoint

```csharp
// Program.cs — creates user, signs in, and redirects
app.MapPost("/Account/RegisterHandler", async (HttpContext context,
    UserManager<IdentityUser> userManager, SignInManager<IdentityUser> signInManager) =>
{
    var form = await context.Request.ReadFormAsync();
    var email = form["email"].ToString();
    var password = form["password"].ToString();

    var user = new IdentityUser { UserName = email, Email = email };
    var result = await userManager.CreateAsync(user, password);
    if (result.Succeeded)
    {
        await signInManager.SignInAsync(user, isPersistent: false);
        return Results.Redirect("/");
    }
    var errors = string.Join("; ", result.Errors.Select(e => e.Description));
    return Results.Redirect($"/Account/Register?error={Uri.EscapeDataString(errors)}");
}).DisableAntiforgery();
```

### Logout Endpoint

```csharp
// Program.cs — signs out and redirects to home page
app.MapPost("/Account/Logout", async (SignInManager<IdentityUser> signInManager) =>
{
    await signInManager.SignOutAsync();
    return Results.Redirect("/");
}).DisableAntiforgery();
```

**Blazor logout form (use instead of a link):**

```razor
<form method="post" action="/Account/Logout">
    <button type="submit" class="nav-link btn btn-link">Log out</button>
</form>
```

> **Note:** Do NOT use `<a href="/Account/Logout">` — Blazor's enhanced navigation will intercept the click and attempt client-side navigation instead of a real HTTP POST. Use a `<form method="post">` with a submit button, or add `data-enhance-nav="false"` to the link. See [Blazor Enhanced Navigation](#blazor-enhanced-navigation) in the data migration skill.

---

## DisableAntiforgery Requirement

> **Important:** When using `<form method="post">` to submit to minimal API endpoints from Blazor-rendered pages, the endpoint **MUST** call `.DisableAntiforgery()` because Blazor's HTML rendering does not include antiforgery tokens. Without this, the request will fail with a 400 Bad Request.
>
> ```csharp
> // ✅ CORRECT — DisableAntiforgery() required for Blazor form submissions
> app.MapPost("/Account/LoginHandler", handler).DisableAntiforgery();
>
> // ❌ WRONG — will reject POST from Blazor-rendered forms
> app.MapPost("/Account/LoginHandler", handler);
> ```
>
> If you need antiforgery protection, you must manually include the token in the form using `@inject Microsoft.AspNetCore.Antiforgery.IAntiforgery` and render a hidden field. For most migration scenarios, `.DisableAntiforgery()` is the pragmatic choice.

---

## Common Identity Gotchas

### No HttpContext.Current
Blazor Server has no `HttpContext.Current`. Use dependency injection:
```csharp
// WRONG: HttpContext.Current.User
// RIGHT: Inject AuthenticationStateProvider or use [CascadingParameter]
```

### Cookie Auth Requires HTTP Endpoints
Blazor Server login/logout MUST use HTTP endpoints (not component-based), because cookies are set on HTTP responses:
```csharp
// Program.cs — Add login/logout endpoints
app.MapPost("/Account/Logout", async (SignInManager<ApplicationUser> signInManager) =>
{
    await signInManager.SignOutAsync();
    return Results.Redirect("/");
});
```

### SignalR Circuit vs HTTP Request
Authentication state is captured when the circuit starts. If the user's session expires mid-circuit, they remain "authenticated" until the page refreshes. Use `RevalidatingServerAuthenticationStateProvider` for periodic revalidation.

### Blazor Identity UI Scaffolding
For a complete Identity UI (login, register, manage profile), scaffold it:
```bash
dotnet aspnet-codegenerator identity -dc ApplicationDbContext --files "Account.Login;Account.Register;Account.Logout"
```
This generates Razor Pages (not components) under `/Areas/Identity/`. They coexist with Blazor.

---

## FormsAuthentication Migration

### Web Forms Pattern

```csharp
// Web Forms — Login.aspx.cs
protected void btnLogin_Click(object sender, EventArgs e)
{
    if (ValidateUser(txtEmail.Text, txtPassword.Text))
    {
        FormsAuthentication.SetAuthCookie(txtEmail.Text, chkRememberMe.Checked);
        Response.Redirect(FormsAuthentication.GetRedirectUrl(txtEmail.Text, false));
    }
}

// Web Forms — Logout
protected void btnLogout_Click(object sender, EventArgs e)
{
    FormsAuthentication.SignOut();
    Response.Redirect("~/");
}

// Web Forms — web.config
<authentication mode="Forms">
  <forms loginUrl="~/Account/Login" timeout="2880" />
</authentication>
```

### Blazor Pattern: ASP.NET Core Cookie Authentication

```csharp
// Program.cs — Configure cookie auth
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.ExpireTimeSpan = TimeSpan.FromDays(2); // 2880 minutes = 2 days
        options.SlidingExpiration = true;
    });

builder.Services.AddCascadingAuthenticationState();
builder.Services.AddAuthorization();

// Middleware pipeline
app.UseAuthentication();
app.UseAuthorization();
```

```csharp
// Program.cs — Login endpoint (minimal API)
app.MapPost("/Account/LoginHandler", async (HttpContext context, IUserService userService) =>
{
    var form = await context.Request.ReadFormAsync();
    var email = form["email"].ToString();
    var password = form["password"].ToString();
    var rememberMe = form["rememberMe"] == "true";
    
    if (await userService.ValidateUserAsync(email, password))
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, email),
            new Claim(ClaimTypes.Email, email)
        };
        
        var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var authProperties = new AuthenticationProperties
        {
            IsPersistent = rememberMe,
            ExpiresUtc = DateTimeOffset.UtcNow.AddDays(rememberMe ? 14 : 1)
        };
        
        await context.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(claimsIdentity),
            authProperties);
        
        return Results.Redirect("/");
    }
    
    return Results.Redirect("/Account/Login?error=Invalid+credentials");
}).DisableAntiforgery();

// Logout endpoint
app.MapPost("/Account/Logout", async (HttpContext context) =>
{
    await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    return Results.Redirect("/");
}).DisableAntiforgery();
```

### FormsAuthentication API Migration

| FormsAuthentication Method | ASP.NET Core Equivalent |
|---------------------------|------------------------|
| `SetAuthCookie(username, persist)` | `HttpContext.SignInAsync(...)` with claims |
| `SignOut()` | `HttpContext.SignOutAsync()` |
| `GetAuthCookie(username, persist)` | Cookie created automatically by `SignInAsync` |
| `GetRedirectUrl(username, createPersist)` | Store in `ReturnUrl` query parameter, read manually |
| `Decrypt(ticket)` | No direct equivalent — use data protection APIs |
| `Encrypt(ticket)` | No direct equivalent — use data protection APIs |
| `LoginUrl` property | `CookieAuthenticationOptions.LoginPath` |
| `Timeout` property | `CookieAuthenticationOptions.ExpireTimeSpan` |

---

## Membership Provider Migration

### Web Forms Pattern

```csharp
// Web Forms — Using Membership provider
protected void btnRegister_Click(object sender, EventArgs e)
{
    MembershipUser user = Membership.CreateUser(txtEmail.Text, txtPassword.Text, txtEmail.Text);
    if (user != null)
    {
        FormsAuthentication.SetAuthCookie(txtEmail.Text, false);
        Response.Redirect("~/");
    }
}

protected void btnChangePassword_Click(object sender, EventArgs e)
{
    MembershipUser user = Membership.GetUser();
    if (user.ChangePassword(txtOldPassword.Text, txtNewPassword.Text))
    {
        lblMessage.Text = "Password changed successfully!";
    }
}

// Web Forms — User lookup
MembershipUser user = Membership.GetUser(username);
if (user != null && user.IsApproved)
{
    // User is valid
}
```

### Blazor Pattern: ASP.NET Core Identity

```csharp
// Program.cs — Configure Identity
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequiredLength = 6;
    options.Password.RequireNonAlphanumeric = false;
    options.User.RequireUniqueEmail = true;
})
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

builder.Services.AddCascadingAuthenticationState();
```

```csharp
// Register endpoint
app.MapPost("/Account/RegisterHandler", async (HttpContext context,
    UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager) =>
{
    var form = await context.Request.ReadFormAsync();
    var email = form["email"].ToString();
    var password = form["password"].ToString();
    
    var user = new ApplicationUser { UserName = email, Email = email };
    var result = await userManager.CreateAsync(user, password);
    
    if (result.Succeeded)
    {
        await signInManager.SignInAsync(user, isPersistent: false);
        return Results.Redirect("/");
    }
    
    var errors = string.Join("; ", result.Errors.Select(e => e.Description));
    return Results.Redirect($"/Account/Register?error={Uri.EscapeDataString(errors)}");
}).DisableAntiforgery();

// Change password endpoint
app.MapPost("/Account/ChangePasswordHandler", async (HttpContext context,
    UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager) =>
{
    var form = await context.Request.ReadFormAsync();
    var oldPassword = form["oldPassword"].ToString();
    var newPassword = form["newPassword"].ToString();
    
    var user = await userManager.GetUserAsync(context.User);
    if (user == null)
        return Results.Redirect("/Account/Login");
    
    var result = await userManager.ChangePasswordAsync(user, oldPassword, newPassword);
    if (result.Succeeded)
        return Results.Redirect("/Account/ChangePassword?success=true");
    
    return Results.Redirect("/Account/ChangePassword?error=Failed+to+change+password");
}).DisableAntiforgery();
```

### Membership API Migration

| Membership Method | ASP.NET Core Identity Equivalent |
|------------------|----------------------------------|
| `Membership.CreateUser(...)` | `userManager.CreateAsync(user, password)` |
| `Membership.GetUser(username)` | `await userManager.FindByNameAsync(username)` |
| `Membership.GetUser()` | `await userManager.GetUserAsync(User)` (in component) |
| `Membership.DeleteUser(username)` | `await userManager.DeleteAsync(user)` |
| `Membership.ValidateUser(username, password)` | `await signInManager.PasswordSignInAsync(...)` |
| `Membership.UpdateUser(user)` | `await userManager.UpdateAsync(user)` |
| `user.ChangePassword(old, new)` | `await userManager.ChangePasswordAsync(user, old, new)` |
| `user.IsApproved` | Check `user.EmailConfirmed` or custom property |
| `user.IsLockedOut` | `await userManager.IsLockedOutAsync(user)` |
| `Membership.GetAllUsers()` | `userManager.Users.ToListAsync()` |

### Database Schema Migration (Membership → Identity)

ASP.NET Membership and ASP.NET Core Identity use **different database schemas**. Migration requires:

1. **Schema mapping:**

| Membership Table | Identity Table | Notes |
|-----------------|---------------|-------|
| `aspnet_Users` | `AspNetUsers` | UserName, Email, PasswordHash |
| `aspnet_Membership` | `AspNetUsers` | Password fields merged into AspNetUsers |
| `aspnet_Roles` | `AspNetRoles` | Role names |
| `aspnet_UsersInRoles` | `AspNetUserRoles` | User-role mappings |

2. **Password hash compatibility:**

> **Critical:** Membership password hashes are **NOT compatible** with Identity. Users will need to:
> - Reset passwords (send password reset emails)
> - Or implement a custom `IPasswordHasher` that validates both formats during a transition period

```csharp
// Custom password hasher for transition period
public class LegacyPasswordHasher : IPasswordHasher<ApplicationUser>
{
    public string HashPassword(ApplicationUser user, string password)
    {
        // Use new Identity hasher
        var hasher = new PasswordHasher<ApplicationUser>();
        return hasher.HashPassword(user, password);
    }
    
    public PasswordVerificationResult VerifyHashedPassword(ApplicationUser user, string hashedPassword, string providedPassword)
    {
        // Try Identity format first
        var hasher = new PasswordHasher<ApplicationUser>();
        var result = hasher.VerifyHashedPassword(user, hashedPassword, providedPassword);
        
        if (result == PasswordVerificationResult.Failed)
        {
            // Try legacy Membership format
            if (VerifyLegacyPassword(hashedPassword, providedPassword))
            {
                return PasswordVerificationResult.SuccessRehashNeeded;
            }
        }
        
        return result;
    }
    
    private bool VerifyLegacyPassword(string hashedPassword, string providedPassword)
    {
        // Implement Membership password verification (SHA1/SHA256 + salt)
        // This depends on your Membership configuration
        return false; // Placeholder
    }
}

// Program.cs
builder.Services.AddScoped<IPasswordHasher<ApplicationUser>, LegacyPasswordHasher>();
```

---

## Roles Provider Migration

### Web Forms Pattern

```csharp
// Web Forms — Using Roles provider
if (Roles.IsUserInRole(username, "Administrator"))
{
    // Admin logic
}

string[] roles = Roles.GetRolesForUser(username);
Roles.AddUserToRole(username, "Manager");
Roles.RemoveUserFromRole(username, "User");

// Web Forms — web.config
<roleManager enabled="true" />
```

### Blazor Pattern: ASP.NET Core Identity Roles

```csharp
// Program.cs — Identity with roles
builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RequireAdministrator", policy => policy.RequireRole("Administrator"));
    options.AddPolicy("RequireManager", policy => policy.RequireRole("Manager", "Administrator"));
});
```

```csharp
// Component or service — using RoleManager
@inject RoleManager<IdentityRole> RoleManager
@inject UserManager<ApplicationUser> UserManager

@code {
    private async Task AssignRoleAsync(string username, string roleName)
    {
        var user = await UserManager.FindByNameAsync(username);
        if (user != null)
        {
            if (!await RoleManager.RoleExistsAsync(roleName))
            {
                await RoleManager.CreateAsync(new IdentityRole(roleName));
            }
            
            await UserManager.AddToRoleAsync(user, roleName);
        }
    }
    
    private async Task<bool> IsUserInRoleAsync(string username, string roleName)
    {
        var user = await UserManager.FindByNameAsync(username);
        return user != null && await UserManager.IsInRoleAsync(user, roleName);
    }
    
    private async Task<IList<string>> GetUserRolesAsync(string username)
    {
        var user = await UserManager.FindByNameAsync(username);
        return user != null ? await UserManager.GetRolesAsync(user) : new List<string>();
    }
}
```

### Roles API Migration

| Roles Method | ASP.NET Core Identity Equivalent |
|-------------|----------------------------------|
| `Roles.IsUserInRole(username, role)` | `await userManager.IsInRoleAsync(user, role)` |
| `Roles.GetRolesForUser(username)` | `await userManager.GetRolesAsync(user)` |
| `Roles.AddUserToRole(username, role)` | `await userManager.AddToRoleAsync(user, role)` |
| `Roles.RemoveUserFromRole(username, role)` | `await userManager.RemoveFromRoleAsync(user, role)` |
| `Roles.CreateRole(roleName)` | `await roleManager.CreateAsync(new IdentityRole(roleName))` |
| `Roles.DeleteRole(roleName)` | `await roleManager.DeleteAsync(role)` |
| `Roles.RoleExists(roleName)` | `await roleManager.RoleExistsAsync(roleName)` |
| `Roles.GetAllRoles()` | `roleManager.Roles.ToListAsync()` |
| `Roles.GetUsersInRole(roleName)` | `await userManager.GetUsersInRoleAsync(roleName)` |

### Policy-Based Authorization (Modern Pattern)

Instead of checking roles in code, use policy-based authorization:

```csharp
// Program.cs — Define policies
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RequireAdmin", policy => policy.RequireRole("Administrator"));
    options.AddPolicy("RequireEditor", policy => policy.RequireRole("Editor", "Administrator"));
    options.AddPolicy("RequireApprover", policy => policy.RequireAssertion(context =>
        context.User.IsInRole("Manager") || context.User.IsInRole("Administrator")));
});
```

```razor
@* Page-level authorization *@
@page "/Admin"
@attribute [Authorize(Policy = "RequireAdmin")]

<h1>Admin Panel</h1>
```

```razor
@* Conditional UI based on policy *@
<AuthorizeView Policy="RequireAdmin">
    <Authorized>
        <button>Delete All</button>
    </Authorized>
</AuthorizeView>

<AuthorizeView Roles="Editor,Administrator">
    <Authorized>
        <button>Edit Content</button>
    </Authorized>
</AuthorizeView>
```

---

## Migration Cheat Sheet

### Quick Reference: Authentication System Detection

Look for these patterns in the Web Forms project to identify which auth system is in use:

| Pattern | Auth System | Migration Complexity |
|---------|------------|---------------------|
| `using Microsoft.AspNet.Identity;` | OWIN Identity | 🟢 Easy — closest match |
| `Startup.Auth.cs` with `app.UseCookieAuthentication` | OWIN Identity | 🟢 Easy |
| `using System.Web.Security;` with `Membership.CreateUser` | Membership | 🟡 Medium — schema + password migration |
| `FormsAuthentication.SetAuthCookie` | FormsAuthentication | 🟡 Medium — manual auth implementation |
| `web.config` `<authentication mode="Forms">` | FormsAuthentication | 🟡 Medium |
| `web.config` `<roleManager enabled="true">` | Roles Provider | 🟢 Easy — convert to Identity roles |

### Quick Reference: Code Patterns

| Web Forms Pattern | Blazor Equivalent |
|------------------|-------------------|
| `FormsAuthentication.SetAuthCookie(user, persist)` | `await HttpContext.SignInAsync(...)` in minimal API |
| `FormsAuthentication.SignOut()` | `await HttpContext.SignOutAsync()` in minimal API |
| `Membership.CreateUser(user, pw, email)` | `await userManager.CreateAsync(user, password)` |
| `Membership.ValidateUser(user, pw)` | `await signInManager.PasswordSignInAsync(...)` |
| `Roles.IsUserInRole(user, role)` | `await userManager.IsInRoleAsync(user, role)` or `User.IsInRole(role)` |
| `Roles.AddUserToRole(user, role)` | `await userManager.AddToRoleAsync(user, role)` |
| `User.Identity.IsAuthenticated` | `User.Identity?.IsAuthenticated ?? false` (works unchanged) |
| `User.Identity.Name` | `User.Identity?.Name` (works unchanged) |
