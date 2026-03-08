<#
.SYNOPSIS
    Layer 2 semantic transforms for BWFC Web Forms → Blazor migration.

.DESCRIPTION
    Applies 3 persistent semantic transforms that Layer 1 (bwfc-migrate.ps1) cannot
    handle because they require cross-file awareness and architectural understanding:

      Pattern A: FormView/DataBound code-behind → ComponentBase + DI
      Pattern B: Auth form simplification (nested model → individual properties)
      Pattern C: Program.cs bootstrap generation

    Run this AFTER bwfc-migrate.ps1 has produced Layer 1 output.
    This script is idempotent — running it again will not double-apply transforms.

.PARAMETER Path
    Path to the Layer 1 output directory (the Blazor project root).

.PARAMETER WhatIf
    Dry-run mode — reports what would change without modifying files.

.PARAMETER DbProvider
    Database provider for Program.cs generation. Default: SQLite.
    Options: SQLite, SqlServer, PostgreSQL, InMemory

.PARAMETER DbContextName
    Name of the EF DbContext class. Auto-detected from Models/ if not specified.

.PARAMETER ProjectNamespace
    Namespace for generated code. Auto-detected from .csproj if not specified.

.EXAMPLE
    .\bwfc-migrate-layer2.ps1 -Path .\output\MyBlazorApp

.EXAMPLE
    .\bwfc-migrate-layer2.ps1 -Path .\output\MyBlazorApp -DbProvider SqlServer -WhatIf

#>

[CmdletBinding(SupportsShouldProcess)]
param(
    [Parameter(Mandatory)]
    [string]$Path,

    [ValidateSet('SQLite', 'SqlServer', 'PostgreSQL', 'InMemory')]
    [string]$DbProvider = 'SQLite',

    [string]$DbContextName,

    [string]$ProjectNamespace
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

# ============================================================================
# Globals & Logging
# ============================================================================

$script:TransformLog = @()
$script:Summary = @{ PatternA = 0; PatternB = 0; PatternC = 0; Skipped = 0 }
$script:Layer2Marker = '// Layer2-transformed'

function Write-Layer2Log {
    param([string]$File, [string]$Pattern, [string]$Detail)
    $entry = [PSCustomObject]@{ File = $File; Pattern = $Pattern; Detail = $Detail; Timestamp = Get-Date }
    $script:TransformLog += $entry
    $color = switch ($Pattern) {
        'PatternA' { 'Cyan' }
        'PatternB' { 'Green' }
        'PatternC' { 'Yellow' }
        default     { 'White' }
    }
    Write-Host "  [$Pattern] $File — $Detail" -ForegroundColor $color
}

# ============================================================================
# Helpers
# ============================================================================

function Get-OutputPath {
    param([string]$OriginalPath)
    return $OriginalPath
}

function Test-AlreadyTransformed {
    param([string]$FilePath)
    if (-not (Test-Path $FilePath)) { return $false }
    $content = Get-Content -Path $FilePath -Raw -Encoding UTF8
    return $content -match [regex]::Escape($script:Layer2Marker)
}

function Find-DbContextName {
    param([string]$ProjectRoot)

    if ($DbContextName) { return $DbContextName }

    # Scan Models/ for DbContext subclass
    $modelsDir = Join-Path $ProjectRoot 'Models'
    if (Test-Path $modelsDir) {
        $csFiles = Get-ChildItem -Path $modelsDir -Filter '*.cs' -Recurse
        foreach ($f in $csFiles) {
            $content = Get-Content -Path $f.FullName -Raw -Encoding UTF8
            if ($content -match 'class\s+(\w+)\s*:\s*DbContext\b') {
                return $Matches[1]
            }
        }
    }

    # Scan all .cs files for DbContext
    $csFiles = Get-ChildItem -Path $ProjectRoot -Filter '*.cs' -Recurse
    foreach ($f in $csFiles) {
        $content = Get-Content -Path $f.FullName -Raw -Encoding UTF8
        if ($content -match 'class\s+(\w+)\s*:\s*DbContext\b') {
            return $Matches[1]
        }
    }

    return $null
}

function Find-ProjectNamespace {
    param([string]$ProjectRoot)

    if ($ProjectNamespace) { return $ProjectNamespace }

    # Try to extract from .csproj RootNamespace
    $csproj = Get-ChildItem -Path $ProjectRoot -Filter '*.csproj' | Select-Object -First 1
    if ($csproj) {
        $content = Get-Content -Path $csproj.FullName -Raw -Encoding UTF8
        if ($content -match '<RootNamespace>([^<]+)</RootNamespace>') {
            return $Matches[1]
        }
        # Fall back to project file name
        return [System.IO.Path]::GetFileNameWithoutExtension($csproj.Name)
    }

    # Fall back to directory name
    return (Split-Path $ProjectRoot -Leaf)
}

function Find-IdentityDbContextName {
    param([string]$ProjectRoot)

    $modelsDir = Join-Path $ProjectRoot 'Models'
    if (Test-Path $modelsDir) {
        $csFiles = Get-ChildItem -Path $modelsDir -Filter '*.cs' -Recurse
        foreach ($f in $csFiles) {
            $content = Get-Content -Path $f.FullName -Raw -Encoding UTF8
            if ($content -match 'class\s+(\w+)\s*:\s*IdentityDbContext') {
                return $Matches[1]
            }
        }
    }
    return $null
}

# ============================================================================
# Pattern A: FormView/DataBound code-behind → ComponentBase + DI
# ============================================================================
#
# Detects .razor.cs files that have Layer 1 TODO markers and patterns like:
#   - SelectMethod references
#   - FormView, GetRouteUrl references
#   - Page_Load / Page_Init handlers
#   - [SupplyParameterFromQuery] already converted from [QueryString]
#
# Transforms to:
#   - inherit ComponentBase
#   - inject IDbContextFactory<T>
#   - SupplyParameterFromQuery for query params
#   - OnInitializedAsync with DB query logic

function Test-PatternA {
    param([string]$CodeBehindPath)

    if (-not (Test-Path $CodeBehindPath)) { return $false }
    $content = Get-Content -Path $CodeBehindPath -Raw -Encoding UTF8

    # Must have Layer 1 TODO header (confirms it's a migrated code-behind)
    if ($content -notmatch 'TODO:.*code-behind was copied from Web Forms') { return $false }

    # Check for FormView/DataBound patterns
    $hasSelectMethod = $content -match 'SelectMethod|GetProducts|GetProduct|GetCategories'
    $hasFormView = $content -match 'FormView|DataBound|IQueryable'
    $hasGetRouteUrl = $content -match 'GetRouteUrl'
    $hasPageLoad = $content -match 'Page_Load|Page_Init|Page_PreRender'
    $hasQueryString = $content -match '\[SupplyParameterFromQuery|QueryString'
    $hasRouteData = $content -match '\[Parameter\]|\[RouteData\]'

    return ($hasSelectMethod -or $hasFormView -or $hasGetRouteUrl -or $hasPageLoad -or $hasQueryString -or $hasRouteData)
}

function Invoke-PatternA {
    param(
        [string]$CodeBehindPath,
        [string]$RazorPath,
        [string]$Namespace,
        [string]$DbCtxName
    )

    if (Test-AlreadyTransformed -FilePath $CodeBehindPath) {
        Write-Layer2Log -File $CodeBehindPath -Pattern 'PatternA' -Detail 'Already transformed — skipping'
        $script:Summary.Skipped++
        return
    }

    $content = Get-Content -Path $CodeBehindPath -Raw -Encoding UTF8
    $className = [System.IO.Path]::GetFileNameWithoutExtension(
        [System.IO.Path]::GetFileNameWithoutExtension($CodeBehindPath)
    )

    # Extract existing [SupplyParameterFromQuery] parameters
    $queryParams = @()
    $paramRegex = [regex]'\[SupplyParameterFromQuery\(Name\s*=\s*"([^"]+)"\)\]\s*(?:public\s+)?(\w+\??)\s+(\w+)'
    $paramMatches = $paramRegex.Matches($content)
    foreach ($m in $paramMatches) {
        $queryParams += @{
            Name      = $m.Groups[1].Value
            Type      = $m.Groups[2].Value
            Property  = $m.Groups[3].Value
        }
    }

    # Extract [Parameter] (from RouteData) parameters
    $routeParams = @()
    $routeParamRegex = [regex]'\[Parameter\]\s*(?:public\s+)?(\w+\??)\s+(\w+)'
    $routeParamMatches = $routeParamRegex.Matches($content)
    foreach ($m in $routeParamMatches) {
        $routeParams += @{
            Type     = $m.Groups[1].Value
            Property = $m.Groups[2].Value
        }
    }

    # Detect what kind of data access this page does
    $isSingleItem = $content -match 'FirstOrDefault|SingleOrDefault|Find\(' -or $className -match 'Detail'
    $isFilteredList = $content -match 'Where\s*\(' -or ($queryParams.Count -gt 0)

    # Determine the entity type from content hints
    $entityType = 'object'
    if ($content -match 'Product') { $entityType = 'Product' }
    elseif ($content -match 'Category') { $entityType = 'Category' }
    elseif ($content -match 'Order(?!Detail)') { $entityType = 'Order' }

    # Build the new code-behind
    $newContent = "$script:Layer2Marker`n"
    $newContent += "using Microsoft.AspNetCore.Components;`n"
    $newContent += "using Microsoft.EntityFrameworkCore;`n"
    if ($DbCtxName) {
        $newContent += "using ${Namespace}.Models;`n"
    }
    $newContent += "`n"
    $newContent += "namespace ${Namespace}`n"
    $newContent += "{`n"
    $newContent += "    public partial class ${className} : ComponentBase`n"
    $newContent += "    {`n"

    # Inject DbContextFactory if we have a DbContext
    if ($DbCtxName) {
        $newContent += "        [Inject] private IDbContextFactory<${DbCtxName}> DbFactory { get; set; } = default!;`n"
        $newContent += "`n"
    }

    # Add query parameters
    foreach ($qp in $queryParams) {
        $newContent += "        [SupplyParameterFromQuery(Name = `"$($qp.Name)`")]`n"
        $newContent += "        public $($qp.Type) $($qp.Property) { get; set; }`n"
        $newContent += "`n"
    }

    # Add route parameters
    foreach ($rp in $routeParams) {
        $newContent += "        [Parameter]`n"
        $newContent += "        public $($rp.Type) $($rp.Property) { get; set; }`n"
        $newContent += "`n"
    }

    # Add data field
    $listField = '_items'  # default, overridden below if not single item
    if ($isSingleItem) {
        $newContent += "        private List<${entityType}> _item = new();`n"
    } else {
        $listField = '_' + ($className.Substring(0,1).ToLower() + $className.Substring(1)) -replace 'List$', 's' -replace 'Page$', 's'
        if ($listField -notmatch 's$') { $listField += 's' }
        $newContent += "        private List<${entityType}> ${listField} = new();`n"
    }
    $newContent += "`n"

    # Build OnInitializedAsync
    $newContent += "        protected override async Task OnInitializedAsync()`n"
    $newContent += "        {`n"

    if ($DbCtxName) {
        $newContent += "            using var db = DbFactory.CreateDbContext();`n"

        if ($isSingleItem -and $queryParams.Count -gt 0) {
            $qp = $queryParams[0]
            $newContent += "            if ($($qp.Property).HasValue && $($qp.Property) > 0)`n"
            $newContent += "            {`n"
            $dbSetName = $entityType + 's'
            if ($entityType -eq 'Category') { $dbSetName = 'Categories' }
            $newContent += "                var found = await db.${dbSetName}.FirstOrDefaultAsync(e => e.$($qp.Name) == $($qp.Property));`n"
            $newContent += "                if (found != null)`n"
            $newContent += "                {`n"
            $newContent += "                    _item = new List<${entityType}> { found };`n"
            $newContent += "                }`n"
            $newContent += "            }`n"
        } elseif ($isFilteredList -and $queryParams.Count -gt 0) {
            $qp = $queryParams[0]
            $dbSetName = $entityType + 's'
            if ($entityType -eq 'Category') { $dbSetName = 'Categories' }
            $newContent += "            IQueryable<${entityType}> query = db.${dbSetName};`n"
            $newContent += "`n"
            $newContent += "            if ($($qp.Property).HasValue && $($qp.Property) > 0)`n"
            $newContent += "            {`n"
            $newContent += "                query = query.Where(e => e.$($qp.Name) == $($qp.Property));`n"
            $newContent += "            }`n"
            $newContent += "`n"
            $newContent += "            ${listField} = await query.ToListAsync();`n"
        } else {
            $dbSetName = $entityType + 's'
            if ($entityType -eq 'Category') { $dbSetName = 'Categories' }
            $newContent += "            // TODO: Customize query as needed`n"
            $newContent += "            ${listField} = await db.${dbSetName}.ToListAsync();`n"
        }
    } else {
        $newContent += "            // TODO: Add data access logic`n"
        $newContent += "            await Task.CompletedTask;`n"
    }

    $newContent += "        }`n"
    $newContent += "    }`n"
    $newContent += "}`n"

    $outputPath = Get-OutputPath -OriginalPath $CodeBehindPath
    if ($PSCmdlet.ShouldProcess($outputPath, "Pattern A: Rewrite code-behind to ComponentBase + DI")) {
        Set-Content -Path $outputPath -Value $newContent -Encoding UTF8
        Write-Layer2Log -File $CodeBehindPath -Pattern 'PatternA' -Detail "Rewrote to ComponentBase + IDbContextFactory<$DbCtxName>"
        $script:Summary.PatternA++
    }
}

# ============================================================================
# Pattern B: Auth form simplification
# ============================================================================
#
# Detects .razor files with login/register patterns containing:
#   - EditForm with nested Model object
#   - [SupplyParameterFromForm] on a model class
#   - SignInManager / UserManager usage
#
# Transforms to:
#   - Individual [SupplyParameterFromForm] string properties with name attrs
#   - Plain HTML <form method="post"> with @onsubmit and @formname
#   - <AntiforgeryToken /> component
#   - Direct SignInManager/UserManager calls in @code block

function Test-PatternB {
    param([string]$RazorPath)

    if (-not (Test-Path $RazorPath)) { return $false }
    $content = Get-Content -Path $RazorPath -Raw -Encoding UTF8

    # Check for auth patterns
    $hasEditForm = $content -match '<EditForm|<form'
    $hasIdentity = $content -match 'SignInManager|UserManager|IdentityUser|LoginModel|RegisterModel'
    $hasLoginPattern = $content -match '(?i)login|register|sign.?in|sign.?up|create.*account'

    # Only match if it has auth patterns AND it's not already simplified
    if ($content -match $script:Layer2Marker) { return $false }

    return ($hasEditForm -and $hasIdentity -and $hasLoginPattern)
}

function Get-AuthPageType {
    param([string]$RazorPath)

    $name = [System.IO.Path]::GetFileNameWithoutExtension($RazorPath)
    if ($name -match '(?i)login') { return 'Login' }
    if ($name -match '(?i)register') { return 'Register' }
    return 'Unknown'
}

function Invoke-PatternB {
    param(
        [string]$RazorPath,
        [string]$Namespace
    )

    if (Test-AlreadyTransformed -FilePath $RazorPath) {
        Write-Layer2Log -File $RazorPath -Pattern 'PatternB' -Detail 'Already transformed — skipping'
        $script:Summary.Skipped++
        return
    }

    $content = Get-Content -Path $RazorPath -Raw -Encoding UTF8
    $authType = Get-AuthPageType -RazorPath $RazorPath

    # Extract the @page directive
    $pageRoute = ''
    if ($content -match '@page\s+"([^"]+)"') {
        $pageRoute = $Matches[1]
    }

    # Extract the PageTitle
    $pageTitle = ''
    if ($content -match '<PageTitle>([^<]+)</PageTitle>') {
        $pageTitle = $Matches[1]
    }

    $outputPath = Get-OutputPath -OriginalPath $RazorPath

    if ($authType -eq 'Login') {
        if (-not $pageTitle) { $pageTitle = 'Log in' }
        if (-not $pageRoute) { $pageRoute = '/Account/Login' }

        $newContent = @"
@* $script:Layer2Marker *@
@page "$pageRoute"
@using Microsoft.AspNetCore.Identity

<PageTitle>$pageTitle</PageTitle>
<h2>$pageTitle.</h2>

<div class="row">
    <div class="col-md-8">
        <section id="loginForm">
            <div class="form-horizontal">
                <h4>Use a local account to log in.</h4>
                <hr />
                @if (!string.IsNullOrEmpty(_errorMessage))
                {
                    <p class="text-danger">@_errorMessage</p>
                }
                <form method="post" @onsubmit="LogIn" @formname="login">
                    <AntiforgeryToken />
                    <div class="form-group">
                        <label class="col-md-2 control-label">Email</label>
                        <div class="col-md-10">
                            <input type="email" class="form-control" name="Email" value="@Email" required />
                        </div>
                    </div>
                    <div class="form-group">
                        <label class="col-md-2 control-label">Password</label>
                        <div class="col-md-10">
                            <input type="password" class="form-control" name="Password" value="@Password" required />
                        </div>
                    </div>
                    <div class="form-group">
                        <div class="col-md-offset-2 col-md-10">
                            <button type="submit" class="btn btn-default">Log in</button>
                        </div>
                    </div>
                </form>
                <p>
                    <a href="/Account/Register">Register as a new user</a>
                </p>
            </div>
        </section>
    </div>
</div>

@code {
    [Inject] private SignInManager<IdentityUser> SignInManager { get; set; } = default!;
    [Inject] private NavigationManager NavigationManager { get; set; } = default!;

    [SupplyParameterFromForm] private string? Email { get; set; }
    [SupplyParameterFromForm] private string? Password { get; set; }

    private string _errorMessage = "";

    private async Task LogIn()
    {
        if (string.IsNullOrEmpty(Email) || string.IsNullOrEmpty(Password))
        {
            _errorMessage = "Email and password are required.";
            return;
        }

        var result = await SignInManager.PasswordSignInAsync(Email, Password, false, false);
        if (result.Succeeded)
        {
            NavigationManager.NavigateTo("/", forceLoad: true);
        }
        else
        {
            _errorMessage = "Invalid login attempt";
        }
    }
}
"@

        if ($PSCmdlet.ShouldProcess($outputPath, "Pattern B: Simplify login form")) {
            Set-Content -Path $outputPath -Value $newContent -Encoding UTF8

            # Remove code-behind if it exists (login uses @code block)
            $codeBehind = $RazorPath + '.cs'
            $codeBehindOutput = Get-OutputPath -OriginalPath $codeBehind
            if (Test-Path $codeBehind) {
                Remove-Item -Path $codeBehindOutput -Force -ErrorAction SilentlyContinue
                Write-Layer2Log -File $codeBehind -Pattern 'PatternB' -Detail "Removed code-behind (merged into @code block)"
            }

            Write-Layer2Log -File $RazorPath -Pattern 'PatternB' -Detail "Simplified login to individual [SupplyParameterFromForm] properties"
            $script:Summary.PatternB++
        }
    }
    elseif ($authType -eq 'Register') {
        if (-not $pageTitle) { $pageTitle = 'Register' }
        if (-not $pageRoute) { $pageRoute = '/Account/Register' }

        $newContent = @"
@* $script:Layer2Marker *@
@page "$pageRoute"
@using Microsoft.AspNetCore.Identity

<PageTitle>$pageTitle</PageTitle>
<h2>$pageTitle.</h2>

@if (!string.IsNullOrEmpty(_errorMessage))
{
    <p class="text-danger">@_errorMessage</p>
}

<div class="form-horizontal">
    <h4>Create a new account</h4>
    <hr />
    <form method="post" @onsubmit="CreateUser" @formname="register">
        <AntiforgeryToken />
        <div class="form-group">
            <label class="col-md-2 control-label">Email</label>
            <div class="col-md-10">
                <input type="email" class="form-control" name="Email" value="@Email" required />
            </div>
        </div>
        <div class="form-group">
            <label class="col-md-2 control-label">Password</label>
            <div class="col-md-10">
                <input type="password" class="form-control" name="Password" value="@Password" required />
            </div>
        </div>
        <div class="form-group">
            <label class="col-md-2 control-label">Confirm password</label>
            <div class="col-md-10">
                <input type="password" class="form-control" name="ConfirmPassword" value="@ConfirmPassword" required />
            </div>
        </div>
        <div class="form-group">
            <div class="col-md-offset-2 col-md-10">
                <button type="submit" class="btn btn-default">Register</button>
            </div>
        </div>
    </form>
</div>

@code {
    [Inject] private UserManager<IdentityUser> UserManager { get; set; } = default!;
    [Inject] private SignInManager<IdentityUser> SignInManager { get; set; } = default!;
    [Inject] private NavigationManager NavigationManager { get; set; } = default!;

    [SupplyParameterFromForm] private string? Email { get; set; }
    [SupplyParameterFromForm] private string? Password { get; set; }
    [SupplyParameterFromForm] private string? ConfirmPassword { get; set; }

    private string _errorMessage = "";

    private async Task CreateUser()
    {
        if (string.IsNullOrEmpty(Email) || string.IsNullOrEmpty(Password))
        {
            _errorMessage = "Email and password are required.";
            return;
        }

        if (Password != ConfirmPassword)
        {
            _errorMessage = "The password and confirmation password do not match.";
            return;
        }

        var user = new IdentityUser { UserName = Email, Email = Email };
        var result = await UserManager.CreateAsync(user, Password);

        if (result.Succeeded)
        {
            await SignInManager.SignInAsync(user, isPersistent: false);
            NavigationManager.NavigateTo("/", forceLoad: true);
        }
        else
        {
            _errorMessage = string.Join(" ", result.Errors.Select(e => e.Description));
        }
    }
}
"@

        if ($PSCmdlet.ShouldProcess($outputPath, "Pattern B: Simplify register form")) {
            Set-Content -Path $outputPath -Value $newContent -Encoding UTF8

            # Remove code-behind if it exists
            $codeBehind = $RazorPath + '.cs'
            $codeBehindOutput = Get-OutputPath -OriginalPath $codeBehind
            if (Test-Path $codeBehind) {
                Remove-Item -Path $codeBehindOutput -Force -ErrorAction SilentlyContinue
                Write-Layer2Log -File $codeBehind -Pattern 'PatternB' -Detail "Removed code-behind (merged into @code block)"
            }

            Write-Layer2Log -File $RazorPath -Pattern 'PatternB' -Detail "Simplified register to individual [SupplyParameterFromForm] properties"
            $script:Summary.PatternB++
        }
    }
    else {
        Write-Layer2Log -File $RazorPath -Pattern 'PatternB' -Detail "Unknown auth page type — skipping (manual review needed)"
        $script:Summary.Skipped++
    }
}

# ============================================================================
# Pattern C: Program.cs bootstrap generation
# ============================================================================
#
# Generates a .NET 9+ SSR Program.cs with:
#   - AddDbContextFactory with configurable provider
#   - AddRazorComponents + AddInteractiveServerComponents
#   - Middleware pipeline
#   - Optional Identity setup
#   - Optional seed data support

function Test-PatternC {
    param([string]$ProjectRoot)

    # Only generate if .razor files exist
    $razorFiles = Get-ChildItem -Path $ProjectRoot -Filter '*.razor' -Recurse -ErrorAction SilentlyContinue
    if ($razorFiles.Count -eq 0) { return $false }

    # Check if Program.cs already exists and was already Layer2-transformed
    $programCs = Join-Path $ProjectRoot 'Program.cs'
    if ((Test-Path $programCs) -and (Test-AlreadyTransformed -FilePath $programCs)) {
        return $false
    }

    return $true
}

function Invoke-PatternC {
    param(
        [string]$ProjectRoot,
        [string]$Namespace,
        [string]$DbCtxName,
        [string]$Provider
    )

    $programCs = Join-Path $ProjectRoot 'Program.cs'

    if (Test-AlreadyTransformed -FilePath $programCs) {
        Write-Layer2Log -File 'Program.cs' -Pattern 'PatternC' -Detail 'Already transformed — skipping'
        $script:Summary.Skipped++
        return
    }

    $identityCtx = Find-IdentityDbContextName -ProjectRoot $ProjectRoot
    $hasIdentity = $null -ne $identityCtx
    $hasSeedData = $false

    # Check for seed data initializer
    $modelsDir = Join-Path $ProjectRoot 'Models'
    if (Test-Path $modelsDir) {
        $csFiles = Get-ChildItem -Path $modelsDir -Filter '*.cs' -Recurse
        foreach ($f in $csFiles) {
            $content = Get-Content -Path $f.FullName -Raw -Encoding UTF8
            if ($content -match 'class\s+(\w+DatabaseInitializer|Seed\w+)') {
                $hasSeedData = $true
                $seedClass = $Matches[1]
                break
            }
        }
    }

    # Build connection string based on provider
    $connString = switch ($Provider) {
        'SQLite'     { "Data Source=$($Namespace.ToLower()).db" }
        'SqlServer'  { "Server=(localdb)\\mssqllocaldb;Database=$Namespace;Trusted_Connection=True" }
        'PostgreSQL' { "Host=localhost;Database=$($Namespace.ToLower());Username=postgres;Password=postgres" }
        'InMemory'   { $Namespace }
    }

    $providerMethod = switch ($Provider) {
        'SQLite'     { 'UseSqlite' }
        'SqlServer'  { 'UseSqlServer' }
        'PostgreSQL' { 'UseNpgsql' }
        'InMemory'   { 'UseInMemoryDatabase' }
    }

    # Build Program.cs
    $sb = [System.Text.StringBuilder]::new()
    [void]$sb.AppendLine($script:Layer2Marker)
    [void]$sb.AppendLine('using BlazorWebFormsComponents;')
    if ($hasIdentity) {
        [void]$sb.AppendLine('using Microsoft.AspNetCore.Identity;')
    }
    [void]$sb.AppendLine('using Microsoft.EntityFrameworkCore;')
    [void]$sb.AppendLine("using ${Namespace}.Models;")
    [void]$sb.AppendLine('')
    [void]$sb.AppendLine('var builder = WebApplication.CreateBuilder(args);')
    [void]$sb.AppendLine('')
    [void]$sb.AppendLine('builder.Services.AddRazorComponents();')
    [void]$sb.AppendLine('')
    [void]$sb.AppendLine('builder.Services.AddBlazorWebFormsComponents();')
    [void]$sb.AppendLine('')

    # Database registration
    [void]$sb.AppendLine('// Database')
    if ($DbCtxName) {
        [void]$sb.AppendLine("builder.Services.AddDbContextFactory<${DbCtxName}>(options =>")
        [void]$sb.AppendLine("    options.${providerMethod}(`"${connString}`"));")
    }
    if ($hasIdentity -and $identityCtx) {
        [void]$sb.AppendLine("builder.Services.AddDbContext<${identityCtx}>(options =>")
        [void]$sb.AppendLine("    options.${providerMethod}(`"${connString}`"));")
    }
    [void]$sb.AppendLine('')

    # Identity registration
    if ($hasIdentity) {
        [void]$sb.AppendLine('// Identity')
        [void]$sb.AppendLine('builder.Services.AddDefaultIdentity<IdentityUser>(options =>')
        [void]$sb.AppendLine('{')
        [void]$sb.AppendLine('    options.SignIn.RequireConfirmedAccount = false;')
        [void]$sb.AppendLine('    options.Password.RequireDigit = true;')
        [void]$sb.AppendLine('    options.Password.RequireNonAlphanumeric = true;')
        [void]$sb.AppendLine('    options.Password.RequireUppercase = true;')
        [void]$sb.AppendLine('    options.Password.RequiredLength = 6;')
        [void]$sb.AppendLine('})')
        [void]$sb.AppendLine(".AddEntityFrameworkStores<${identityCtx}>();")
        [void]$sb.AppendLine('')
        [void]$sb.AppendLine('builder.Services.AddCascadingAuthenticationState();')
        [void]$sb.AppendLine('')
    }

    # Session support (commonly needed for cart patterns)
    [void]$sb.AppendLine('// Session')
    [void]$sb.AppendLine('builder.Services.AddDistributedMemoryCache();')
    [void]$sb.AppendLine('builder.Services.AddSession(options =>')
    [void]$sb.AppendLine('{')
    [void]$sb.AppendLine('    options.IdleTimeout = TimeSpan.FromMinutes(30);')
    [void]$sb.AppendLine('    options.Cookie.HttpOnly = true;')
    [void]$sb.AppendLine('    options.Cookie.IsEssential = true;')
    [void]$sb.AppendLine('});')
    [void]$sb.AppendLine('builder.Services.AddHttpContextAccessor();')
    [void]$sb.AppendLine('')
    [void]$sb.AppendLine('var app = builder.Build();')
    [void]$sb.AppendLine('')

    # Seed data
    if ($hasSeedData -and $DbCtxName) {
        [void]$sb.AppendLine('// Seed the database')
        [void]$sb.AppendLine('using (var scope = app.Services.CreateScope())')
        [void]$sb.AppendLine('{')
        [void]$sb.AppendLine("    var dbFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<${DbCtxName}>>();")
        [void]$sb.AppendLine('    using var db = dbFactory.CreateDbContext();')
        [void]$sb.AppendLine('    db.Database.EnsureCreated();')
        [void]$sb.AppendLine("    ${seedClass}.Seed(db);")
        if ($hasIdentity -and $identityCtx) {
            [void]$sb.AppendLine('')
            [void]$sb.AppendLine("    var identityDb = scope.ServiceProvider.GetRequiredService<${identityCtx}>();")
            [void]$sb.AppendLine('    identityDb.Database.EnsureCreated();')
        }
        [void]$sb.AppendLine('}')
        [void]$sb.AppendLine('')
    }

    # Middleware pipeline
    [void]$sb.AppendLine('if (!app.Environment.IsDevelopment())')
    [void]$sb.AppendLine('{')
    [void]$sb.AppendLine('    app.UseExceptionHandler("/Error");')
    [void]$sb.AppendLine('    app.UseHsts();')
    [void]$sb.AppendLine('}')
    [void]$sb.AppendLine('')
    [void]$sb.AppendLine('app.UseHttpsRedirection();')
    [void]$sb.AppendLine('app.UseStaticFiles();')
    [void]$sb.AppendLine('app.UseSession();')
    if ($hasIdentity) {
        [void]$sb.AppendLine('app.UseAuthentication();')
        [void]$sb.AppendLine('app.UseAuthorization();')
    }
    [void]$sb.AppendLine('app.UseAntiforgery();')
    [void]$sb.AppendLine('')
    [void]$sb.AppendLine("app.MapRazorComponents<${Namespace}.Components.App>();")
    [void]$sb.AppendLine('')

    # Logout endpoint if identity is present
    if ($hasIdentity) {
        [void]$sb.AppendLine('// Minimal API: Logout')
        [void]$sb.AppendLine('app.MapGet("/account/logout", async (HttpContext ctx, SignInManager<IdentityUser> signInManager) =>')
        [void]$sb.AppendLine('{')
        [void]$sb.AppendLine('    await signInManager.SignOutAsync();')
        [void]$sb.AppendLine('    return Results.Redirect("/");')
        [void]$sb.AppendLine('});')
        [void]$sb.AppendLine('')
    }

    [void]$sb.AppendLine('app.Run();')

    $outputPath = Get-OutputPath -OriginalPath $programCs
    if ($PSCmdlet.ShouldProcess($outputPath, "Pattern C: Generate Program.cs bootstrap")) {
        Set-Content -Path $outputPath -Value $sb.ToString() -Encoding UTF8
        Write-Layer2Log -File 'Program.cs' -Pattern 'PatternC' -Detail "Generated with $Provider provider, Identity=$hasIdentity, Seed=$hasSeedData"
        $script:Summary.PatternC++
    }
}

# ============================================================================
# Entry Point
# ============================================================================

Write-Host ''
Write-Host '╔══════════════════════════════════════════════════════════╗' -ForegroundColor Cyan
Write-Host '║  BWFC Migration — Layer 2 Semantic Transforms           ║' -ForegroundColor Cyan
Write-Host '╚══════════════════════════════════════════════════════════╝' -ForegroundColor Cyan
Write-Host ''

# Validate input path
if (-not (Test-Path $Path)) {
    Write-Error "Path not found: $Path"
    return
}

$Path = (Resolve-Path $Path).Path

if ($WhatIfPreference) {
    Write-Host "[WhatIf] Dry-run mode — no files will be modified" -ForegroundColor Yellow
}

# Auto-detect project settings
$ns = Find-ProjectNamespace -ProjectRoot $Path
$dbCtx = Find-DbContextName -ProjectRoot $Path
Write-Host "  Namespace:  $ns" -ForegroundColor DarkGray
Write-Host "  DbContext:  $(if ($dbCtx) { $dbCtx } else { '(none detected)' })" -ForegroundColor DarkGray
Write-Host "  DbProvider: $DbProvider" -ForegroundColor DarkGray
Write-Host ''

# ── Pattern A: Scan code-behinds ──
Write-Host '── Pattern A: FormView/DataBound → ComponentBase + DI ──' -ForegroundColor Cyan
$codeBehindFiles = Get-ChildItem -Path $Path -Filter '*.razor.cs' -Recurse -ErrorAction SilentlyContinue
$patternACount = 0
foreach ($cb in $codeBehindFiles) {
    if (Test-PatternA -CodeBehindPath $cb.FullName) {
        $razorPath = $cb.FullName -replace '\.cs$', ''
        Invoke-PatternA -CodeBehindPath $cb.FullName -RazorPath $razorPath -Namespace $ns -DbCtxName $dbCtx
        $patternACount++
    }
}
if ($patternACount -eq 0) {
    Write-Host '  No Pattern A candidates detected.' -ForegroundColor DarkGray
}

# ── Pattern B: Scan auth pages ──
Write-Host ''
Write-Host '── Pattern B: Auth Form Simplification ──' -ForegroundColor Green
$razorFiles = Get-ChildItem -Path $Path -Filter '*.razor' -Recurse -ErrorAction SilentlyContinue |
    Where-Object { $_.Name -notmatch '\.razor\.cs$' }
$patternBCount = 0
foreach ($rf in $razorFiles) {
    if (Test-PatternB -RazorPath $rf.FullName) {
        Invoke-PatternB -RazorPath $rf.FullName -Namespace $ns
        $patternBCount++
    }
}
if ($patternBCount -eq 0) {
    Write-Host '  No Pattern B candidates detected.' -ForegroundColor DarkGray
}

# ── Pattern C: Program.cs ──
Write-Host ''
Write-Host '── Pattern C: Program.cs Bootstrap Generation ──' -ForegroundColor Yellow
if (Test-PatternC -ProjectRoot $Path) {
    Invoke-PatternC -ProjectRoot $Path -Namespace $ns -DbCtxName $dbCtx -Provider $DbProvider
} else {
    Write-Host '  No Pattern C candidates detected (or already transformed).' -ForegroundColor DarkGray
}

# ── Summary ──
Write-Host ''
Write-Host '╔══════════════════════════════════════════════════════════╗' -ForegroundColor Cyan
Write-Host '║  Layer 2 Summary                                        ║' -ForegroundColor Cyan
Write-Host '╠══════════════════════════════════════════════════════════╣' -ForegroundColor Cyan
Write-Host "║  Pattern A (FormView→ComponentBase):  $($script:Summary.PatternA) file(s)         ║" -ForegroundColor Cyan
Write-Host "║  Pattern B (Auth Simplification):     $($script:Summary.PatternB) file(s)         ║" -ForegroundColor Green
Write-Host "║  Pattern C (Program.cs Bootstrap):    $($script:Summary.PatternC) file(s)         ║" -ForegroundColor Yellow
Write-Host "║  Skipped (already transformed):       $($script:Summary.Skipped) file(s)         ║" -ForegroundColor DarkGray
Write-Host '╚══════════════════════════════════════════════════════════╝' -ForegroundColor Cyan
Write-Host ''

# Export transform log
if ($script:TransformLog.Count -gt 0) {
    $logPath = Join-Path $Path 'layer2-transforms.log'
    $script:TransformLog | ForEach-Object {
        "[$($_.Timestamp.ToString('HH:mm:ss'))] [$($_.Pattern)] $($_.File) — $($_.Detail)"
    } | Set-Content -Path $logPath -Encoding UTF8
    Write-Host "Transform log written to: $logPath" -ForegroundColor DarkGray
}

Write-Host 'Layer 2 complete.' -ForegroundColor Green
