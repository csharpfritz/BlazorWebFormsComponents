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
    Database provider for Program.cs generation. Auto-detected from source project if not specified.
    Options: SqlServer, PostgreSQL, InMemory
    NOTE: No default value — preserves source database technology per migration boundary rules.

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

    [ValidateSet('SqlServer', 'PostgreSQL', 'InMemory')]
    [string]$DbProvider,

    [string]$DbContextName,

    [string]$ProjectNamespace,

    [Parameter(HelpMessage = "Path to the original Web Forms source project (for .edmx and WebMethod detection)")]
    [string]$SourcePath,

    [Parameter(HelpMessage = "Skip automatic EF Core scaffolding (generate scaffold-command.txt only)")]
    [switch]$SkipEfScaffold
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

# ============================================================================
# Globals & Logging
# ============================================================================

$script:TransformLog = @()
$script:ManualItems = [System.Collections.Generic.List[PSCustomObject]]::new()
$script:Summary = @{ PatternA = 0; PatternB = 0; PatternC = 0; PatternD = 0; EdmxScaffold = 0; WebMethod = 0; Skipped = 0 }
$script:Layer2Marker = '// Layer2-transformed'

function Write-Layer2Log {
    param([string]$File, [string]$Pattern, [string]$Detail)
    $entry = [PSCustomObject]@{ File = $File; Pattern = $Pattern; Detail = $Detail; Timestamp = Get-Date }
    $script:TransformLog += $entry
    $color = switch ($Pattern) {
        'PatternA' { 'Cyan' }
        'PatternB' { 'Green' }
        'PatternC' { 'Yellow' }
        'PatternD' { 'Blue' }
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

function Find-ConnectionString {
    <#
    .SYNOPSIS
        Parses connection strings from Web.config.
    .DESCRIPTION
        Searches for <connectionStrings><add name="..." connectionString="..." /></connectionStrings>
        Also extracts embedded provider connection strings from EF metadata connection strings.
    .PARAMETER WebConfigPath
        Path to Web.config file.
    .PARAMETER ContextName
        Name of the DbContext (used for LocalDB fallback).
    .OUTPUTS
        Returns the first valid SQL Server connection string found, or a LocalDB default.
    #>
    param(
        [string]$WebConfigPath,
        [string]$ContextName
    )

    if (-not $WebConfigPath -or -not (Test-Path $WebConfigPath)) {
        # Return LocalDB fallback
        return "Server=(localdb)\mssqllocaldb;Database=$ContextName;Trusted_Connection=True;"
    }

    try {
        $configContent = Get-Content -Path $WebConfigPath -Raw -Encoding UTF8

        # Pattern 1: Standard connection strings
        # <add name="..." connectionString="Server=...;Database=...;..." />
        $standardPattern = '<add\s+name="[^"]*"\s+connectionString="([^"]+)"'
        $standardMatches = [regex]::Matches($configContent, $standardPattern, [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)
        
        foreach ($match in $standardMatches) {
            $connStr = $match.Groups[1].Value
            # Skip EF metadata connection strings (they wrap the real connection string)
            if ($connStr -match '^metadata=') {
                continue
            }
            # Check if it looks like a SQL Server connection string
            if ($connStr -match 'Server=|Data Source=|Initial Catalog=|Database=') {
                # Decode HTML entities
                $connStr = $connStr -replace '&quot;', '"'
                $connStr = $connStr -replace '&amp;', '&'
                return $connStr
            }
        }

        # Pattern 2: EF connection strings with embedded provider connection string
        # connectionString="metadata=...;provider connection string=&quot;Server=...&quot;"
        $efPattern = 'provider connection string="([^"]+)"'
        $efMatches = [regex]::Matches($configContent, $efPattern, [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)
        
        foreach ($match in $efMatches) {
            $connStr = $match.Groups[1].Value
            # Decode HTML entities
            $connStr = $connStr -replace '&quot;', '"'
            $connStr = $connStr -replace '&amp;', '&'
            if ($connStr -match 'Server=|Data Source=|Initial Catalog=|Database=') {
                return $connStr
            }
        }

        # Pattern 3: HTML-encoded EF connection strings
        # provider connection string=&quot;Data Source=...&quot;
        $efEncodedPattern = 'provider connection string=&quot;([^&]+)&quot;'
        $efEncodedMatches = [regex]::Matches($configContent, $efEncodedPattern, [System.Text.RegularExpressions.RegexOptions]::IgnoreCase)
        
        foreach ($match in $efEncodedMatches) {
            $connStr = $match.Groups[1].Value
            if ($connStr -match 'Server=|Data Source=|Initial Catalog=|Database=') {
                return $connStr
            }
        }
    }
    catch {
        Write-Verbose "Could not parse Web.config: $_"
    }

    # Fallback to LocalDB default
    return "Server=(localdb)\mssqllocaldb;Database=$ContextName;Trusted_Connection=True;"
}

function Find-EdmxNamespace {
    <#
    .SYNOPSIS
        Extracts the original namespace from EDMX CSDL Schema element.
    .PARAMETER EdmxContent
        Raw content of the EDMX file.
    .OUTPUTS
        Returns the namespace string, or $null if not found.
    #>
    param([string]$EdmxContent)

    # Look for the CSDL Schema Namespace attribute
    # The CSDL section has xmlns with /edm patterns and comes after SSDL
    # Pattern: <Schema Namespace="ContosoUniversity.Models" ...>
    if ($EdmxContent -match '<Schema\s+Namespace="([^"]+)"[^>]*xmlns="http://schemas\.microsoft\.com/ado/\d+/\d+/edm"') {
        return $Matches[1]
    }

    # Alternative: find all Schema Namespace attributes and pick the one that looks like an app namespace
    $namespaceMatches = [regex]::Matches($EdmxContent, '<Schema\s+Namespace="([^"]+)"')
    foreach ($nm in $namespaceMatches) {
        $ns = $nm.Groups[1].Value
        # Skip SSDL-style namespaces (usually end with "Model.Store" or are database names)
        if ($ns -notmatch '\.Store$|^dbo$') {
            return $ns
        }
    }

    return $null
}

function Wait-LocalDbReady {
    <#
    .SYNOPSIS
        Ensures LocalDB is running and the database is accessible before EF scaffolding.
    .DESCRIPTION
        LocalDB databases using AttachDbFilename (MDF files) need special handling:
        1. Ensure LocalDB instance is running
        2. If MDF file exists, attach it
        3. Wait for database to become accessible (with timeout)
    .PARAMETER ConnectionString
        The SQL Server connection string (may include AttachDbFilename).
    .PARAMETER MaxWaitSeconds
        Maximum seconds to wait for database to become ready. Default: 30.
    .OUTPUTS
        Returns $true if database is ready, $false if timeout or error.
    #>
    param(
        [string]$ConnectionString,
        [int]$MaxWaitSeconds = 30
    )

    # Check if this is a LocalDB connection
    if ($ConnectionString -notmatch 'localdb|\.mdf') {
        # Not LocalDB — assume always ready
        return $true
    }

    Write-Host "    Initializing LocalDB database..." -ForegroundColor DarkGray

    # Extract LocalDB instance name (default: mssqllocaldb)
    $instanceName = 'mssqllocaldb'
    if ($ConnectionString -match '\(localdb\)\\([^;]+)') {
        $instanceName = $Matches[1]
    }

    # Step 1: Ensure LocalDB instance is running
    try {
        $sqllocaldbPath = Get-Command SqlLocalDB.exe -ErrorAction SilentlyContinue
        if ($sqllocaldbPath) {
            Write-Host "      Starting LocalDB instance '$instanceName'..." -ForegroundColor DarkGray
            $startResult = & SqlLocalDB.exe start $instanceName 2>&1
            if ($LASTEXITCODE -ne 0) {
                # Instance might not exist — create it
                Write-Host "      Creating LocalDB instance '$instanceName'..." -ForegroundColor DarkGray
                $createResult = & SqlLocalDB.exe create $instanceName 2>&1
                $startResult = & SqlLocalDB.exe start $instanceName 2>&1
            }
        }
    }
    catch {
        Write-Host "      ⚠ SqlLocalDB.exe not found — LocalDB may not be available" -ForegroundColor Yellow
        return $false
    }

    # Step 2: Check for MDF file attachment
    $mdfPath = $null
    if ($ConnectionString -match 'AttachDbFilename=([^;]+)') {
        $mdfPath = $Matches[1]
        # Resolve |DataDirectory| placeholder
        if ($mdfPath -match '\|DataDirectory\|') {
            # In Web Forms, DataDirectory is typically App_Data
            # We don't have that context here, so warn
            Write-Host "      ⚠ MDF file uses |DataDirectory| — may need manual attachment" -ForegroundColor Yellow
            $mdfPath = $null
        }
    }

    # Step 3: Wait for database connectivity with exponential backoff
    $stopwatch = [System.Diagnostics.Stopwatch]::StartNew()
    $attempt = 0
    $waitMs = 500  # Start with 500ms

    while ($stopwatch.Elapsed.TotalSeconds -lt $MaxWaitSeconds) {
        $attempt++
        
        try {
            # Use SqlClient to test connection
            # Note: This requires System.Data.SqlClient or Microsoft.Data.SqlClient
            # Fall back to sqlcmd if available
            $sqlcmdPath = Get-Command sqlcmd.exe -ErrorAction SilentlyContinue
            if ($sqlcmdPath) {
                # Build a minimal sqlcmd connection test
                $serverMatch = $ConnectionString -match 'Server=([^;]+)|Data Source=([^;]+)'
                $server = if ($Matches[1]) { $Matches[1] } else { $Matches[2] }
                
                $testResult = & sqlcmd.exe -S $server -Q "SELECT 1" -W -h -1 2>&1
                if ($LASTEXITCODE -eq 0 -and $testResult -match '1') {
                    $elapsed = [math]::Round($stopwatch.Elapsed.TotalSeconds, 1)
                    Write-Host "      ✓ LocalDB ready (took ${elapsed}s)" -ForegroundColor Green
                    return $true
                }
            }
            else {
                # No sqlcmd — try PowerShell SqlConnection
                Add-Type -AssemblyName System.Data -ErrorAction SilentlyContinue
                $conn = New-Object System.Data.SqlClient.SqlConnection
                $conn.ConnectionString = "$ConnectionString;Connect Timeout=5"
                $conn.Open()
                $conn.Close()
                
                $elapsed = [math]::Round($stopwatch.Elapsed.TotalSeconds, 1)
                Write-Host "      ✓ LocalDB ready (took ${elapsed}s)" -ForegroundColor Green
                return $true
            }
        }
        catch {
            # Connection failed — wait and retry
        }

        Write-Host "      Waiting for LocalDB (attempt $attempt)..." -ForegroundColor DarkGray
        Start-Sleep -Milliseconds $waitMs
        $waitMs = [Math]::Min($waitMs * 2, 5000)  # Exponential backoff, max 5s
    }

    Write-Host "      ⚠ LocalDB not ready after ${MaxWaitSeconds}s — scaffold may fail" -ForegroundColor Yellow
    return $false
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

function Get-PluralName {
    param([string]$Name)

    if (-not $Name -or $Name -eq 'object') { return 'Items' }

    # Words ending in 'y' preceded by a consonant → 'ies' (Category → Categories)
    if ($Name -match '[^aeiou]y$') {
        return $Name.Substring(0, $Name.Length - 1) + 'ies'
    }
    # Words ending in 's', 'x', 'z', 'ch', 'sh' → 'es' (Address → Addresses)
    if ($Name -match '(s|x|z|ch|sh)$') {
        return $Name + 'es'
    }
    # Words ending in 'fe' → 'ves' (Life → Lives)
    if ($Name -match 'fe$') {
        return $Name.Substring(0, $Name.Length - 2) + 'ves'
    }
    # Words ending in 'f' (not 'ff') → 'ves' (Leaf → Leaves)
    if ($Name -match '[^f]f$') {
        return $Name.Substring(0, $Name.Length - 1) + 'ves'
    }
    # Default: append 's'
    return $Name + 's'
}

function Find-DbSetName {
    param(
        [string]$EntityType,
        [string]$ProjectRoot
    )

    if (-not $EntityType -or $EntityType -eq 'object') { return 'Items' }

    # Try to find actual DbSet<EntityType> property name from DbContext source
    $csFiles = Get-ChildItem -Path $ProjectRoot -Filter '*.cs' -Recurse -ErrorAction SilentlyContinue
    foreach ($f in $csFiles) {
        $fc = Get-Content -Path $f.FullName -Raw -Encoding UTF8
        if ($fc -match "DbSet<${EntityType}>\s+(\w+)") {
            return $Matches[1]
        }
    }

    # Fall back to pluralization helper
    return Get-PluralName -Name $EntityType
}

function Find-EntityType {
    param(
        [string]$CodeBehindPath,
        [string]$Content
    )

    # Step 1: Check companion .razor file for ItemType or TItem
    $razorPath = $CodeBehindPath -replace '\.cs$', ''
    if (Test-Path $razorPath) {
        $razorContent = Get-Content -Path $razorPath -Raw -Encoding UTF8
        # Use word boundary (\b) to avoid partial matches like 'ShowOnlyCurrentWordInCompletionListItem="true"'
        if ($razorContent -match '\bItemType="([^"]+)"') {
            $typeName = $Matches[1]
            # Strip namespace prefix (e.g., WingtipToys.Models.Product → Product)
            if ($typeName -match '\.(\w+)$') { $typeName = $Matches[1] }
            return $typeName
        }
        if ($razorContent -match '\bTItem="([^"]+)"') {
            $typeName = $Matches[1]
            if ($typeName -match '\.(\w+)$') { $typeName = $Matches[1] }
            return $typeName
        }
    }

    # Step 2: Scan code-behind for strong type hints
    if ($Content -match 'IQueryable<(\w+)>') { return $Matches[1] }
    if ($Content -match 'List<(\w+)>') { return $Matches[1] }
    if ($Content -match 'DbSet<(\w+)>') { return $Matches[1] }

    # Step 3: Match against Model class names found in the project
    $modelsDir = Join-Path $Path 'Models'
    if (Test-Path $modelsDir) {
        $modelClasses = @()
        foreach ($f in (Get-ChildItem -Path $modelsDir -Filter '*.cs' -Recurse)) {
            $mc = Get-Content -Path $f.FullName -Raw -Encoding UTF8
            $classMatches = [regex]::Matches($mc, 'public\s+class\s+(\w+)')
            foreach ($cm in $classMatches) {
                $cn = $cm.Groups[1].Value
                # Skip infrastructure classes
                if ($cn -notmatch 'Context$|Initializer$|Seeder$|Configuration$|Migration') {
                    $modelClasses += $cn
                }
            }
        }
        foreach ($mc in $modelClasses) {
            if ($Content -match $mc) {
                return $mc
            }
        }
    }

    # Step 4: No entity type detected
    return $null
}

function Write-ManualItem {
    param(
        [string]$File,
        [string]$Category,
        [string]$Detail
    )
    $entry = [PSCustomObject]@{
        File     = $File
        Category = $Category
        Detail   = $Detail
    }
    $script:ManualItems.Add($entry)
}

# ============================================================================
# EDMX → dotnet ef dbcontext scaffold command generation
# ============================================================================
#
# Web Forms DB-first apps use .edmx files. Instead of copying EF6 models
# verbatim, detect .edmx files and generate a scaffold command for EF Core.
# With automatic scaffolding enabled (default), actually runs the command.

function Convert-EdmxToScaffold {
    param(
        [string]$SourceDir,
        [string]$OutputDir
    )

    if (-not $SourceDir -or -not (Test-Path $SourceDir)) { return }

    $edmxFiles = @(Get-ChildItem -Path $SourceDir -Filter '*.edmx' -Recurse -File -ErrorAction SilentlyContinue)
    if ($edmxFiles.Count -eq 0) { return }

    foreach ($edmxFile in $edmxFiles) {
        $relPath = $edmxFile.FullName.Substring($SourceDir.Length).TrimStart('\', '/')
        Write-Layer2Log -File $relPath -Pattern 'EdmxScaffold' -Detail "Found .edmx file: $($edmxFile.Name)"

        # Parse the .edmx XML to extract useful info
        $providerName = 'Microsoft.EntityFrameworkCore.SqlServer'
        $entityNames = @()
        $contextName = ''
        $edmxNamespace = $null

        try {
            $rawContent = Get-Content -Path $edmxFile.FullName -Raw -Encoding UTF8

            # Extract entity type names from ConceptualModels
            $entityTypeRegex = [regex]'<EntityType\s+Name="([^"]+)"'
            $entityMatches = $entityTypeRegex.Matches($rawContent)
            foreach ($em in $entityMatches) {
                $entityNames += $em.Groups[1].Value
            }

            # Try to extract EntityContainer name (becomes the DbContext name)
            # Prefer the ConceptualModels EntityContainer (CSDL), not SSDL
            $containerRegex = [regex]'<EntityContainer\s+Name="([^"]+)"'
            $containerMatches = $containerRegex.Matches($rawContent)
            if ($containerMatches.Count -gt 0) {
                # Use the last match (CSDL comes after SSDL in EDMX)
                $contextName = $containerMatches[$containerMatches.Count - 1].Groups[1].Value
            }

            # Extract the original namespace from CSDL Schema element
            $edmxNamespace = Find-EdmxNamespace -EdmxContent $rawContent

            # Detect provider from SSDL
            if ($rawContent -match 'Provider="([^"]+)"') {
                $ssdlProvider = $Matches[1]
                $providerName = switch -Wildcard ($ssdlProvider) {
                    '*SqlClient*'   { 'Microsoft.EntityFrameworkCore.SqlServer' }
                    '*SQLite*'      { 'Microsoft.EntityFrameworkCore.Sqlite' }
                    '*Npgsql*'      { 'Npgsql.EntityFrameworkCore.PostgreSQL' }
                    '*MySql*'       { 'Pomelo.EntityFrameworkCore.MySql' }
                    default         { 'Microsoft.EntityFrameworkCore.SqlServer' }
                }
            }
        }
        catch {
            Write-Layer2Log -File $relPath -Pattern 'EdmxScaffold' -Detail "Could not parse .edmx XML — generating generic scaffold command"
        }

        if (-not $contextName) {
            $contextName = [System.IO.Path]::GetFileNameWithoutExtension($edmxFile.Name)
        }

        # Find connection string from Web.config
        $webConfigPath = Join-Path $SourceDir 'Web.config'
        $connectionString = Find-ConnectionString -WebConfigPath $webConfigPath -ContextName $contextName

        # Determine if we should auto-scaffold
        $autoScaffold = -not $SkipEfScaffold -and -not $WhatIfPreference
        $scaffoldSuccess = $false

        if ($autoScaffold) {
            Write-Layer2Log -File $relPath -Pattern 'EdmxScaffold' -Detail "Attempting automatic EF Core scaffolding..."

            # Wait for LocalDB to be ready (if applicable)
            $dbReady = Wait-LocalDbReady -ConnectionString $connectionString -MaxWaitSeconds 30
            if (-not $dbReady) {
                Write-Layer2Log -File $relPath -Pattern 'EdmxScaffold' -Detail "LocalDB not ready — falling back to manual scaffold command"
                Write-Host "    ⚠ Database not ready — generating scaffold command for manual execution" -ForegroundColor Yellow
                $autoScaffold = $false
            }
        }

        if ($autoScaffold) {
            # Add prerequisite packages
            $designPackageResult = $null
            $providerPackageResult = $null
            
            Push-Location $OutputDir
            try {
                Write-Host "    Adding Microsoft.EntityFrameworkCore.Design package..." -ForegroundColor DarkGray
                $designPackageResult = & dotnet add package Microsoft.EntityFrameworkCore.Design 2>&1
                
                Write-Host "    Adding $providerName package..." -ForegroundColor DarkGray
                $providerPackageResult = & dotnet add package $providerName 2>&1

                # Build namespace argument if we found one
                $namespaceArg = ''
                if ($edmxNamespace) {
                    $namespaceArg = "--namespace `"$edmxNamespace`""
                }

                # Run scaffold command - use ORIGINAL context name (NOT suffixed with DbContext)
                Write-Host "    Running dotnet ef dbcontext scaffold..." -ForegroundColor DarkGray
                $scaffoldCmd = "dotnet ef dbcontext scaffold `"$connectionString`" $providerName --output-dir Models --context `"$contextName`" $namespaceArg --force"
                Write-Verbose "Scaffold command: $scaffoldCmd"

                $scaffoldResult = Invoke-Expression $scaffoldCmd 2>&1
                $scaffoldExitCode = $LASTEXITCODE

                if ($scaffoldExitCode -eq 0) {
                    $scaffoldSuccess = $true
                    Write-Layer2Log -File $relPath -Pattern 'EdmxScaffold' -Detail "Auto-scaffolded EF Core models: $contextName ($($entityNames.Count) entities)"
                    Write-Host "    ✓ EF Core scaffolding successful" -ForegroundColor Green

                    # Delete old EF6 model files from output directory
                    $ef6FilesToDelete = @(
                        (Join-Path $OutputDir "Models\$($edmxFile.BaseName).edmx"),
                        (Join-Path $OutputDir "Models\$($edmxFile.BaseName).Designer.cs"),
                        (Join-Path $OutputDir "Models\$($edmxFile.BaseName).Context.tt"),
                        (Join-Path $OutputDir "Models\$($edmxFile.BaseName).tt")
                    )
                    
                    # Also find any .tt or .Designer.cs files related to the EDMX
                    $edmxBaseName = $edmxFile.BaseName
                    $oldModelFiles = Get-ChildItem -Path $OutputDir -Recurse -File -ErrorAction SilentlyContinue | Where-Object {
                        $_.Name -match "^$edmxBaseName\.(edmx|Designer\.cs|Context\.tt|tt)$" -or
                        $_.Name -match "\.edmx$" -or
                        $_.Name -match "\.edmx\.diagram$"
                    }

                    foreach ($oldFile in $oldModelFiles) {
                        if (Test-Path $oldFile.FullName) {
                            Remove-Item -Path $oldFile.FullName -Force -ErrorAction SilentlyContinue
                            Write-Layer2Log -File $oldFile.Name -Pattern 'EdmxScaffold' -Detail "Deleted old EF6 file: $($oldFile.Name)"
                        }
                    }
                }
                else {
                    Write-Layer2Log -File $relPath -Pattern 'EdmxScaffold' -Detail "Scaffold failed (database may not be available) — generating manual command"
                    Write-Host "    ⚠ Scaffold failed — falling back to manual command generation" -ForegroundColor Yellow
                    Write-Verbose "Scaffold output: $scaffoldResult"
                }
            }
            catch {
                Write-Layer2Log -File $relPath -Pattern 'EdmxScaffold' -Detail "Scaffold error: $($_.Exception.Message) — generating manual command"
                Write-Host "    ⚠ Scaffold error — falling back to manual command generation" -ForegroundColor Yellow
            }
            finally {
                Pop-Location
            }
        }

        # Generate scaffold-command.txt (always, for reference or if auto-scaffold failed/skipped)
        if (-not $scaffoldSuccess) {
            # Build namespace argument for command text
            $namespaceArgText = ''
            if ($edmxNamespace) {
                $namespaceArgText = "--namespace `"$edmxNamespace`" "
            }

            $sb = [System.Text.StringBuilder]::new()
            [void]$sb.AppendLine("# ============================================================================")
            [void]$sb.AppendLine("# EF Core Scaffold Command — generated from $($edmxFile.Name)")
            [void]$sb.AppendLine("# ============================================================================")
            [void]$sb.AppendLine("#")
            [void]$sb.AppendLine("# Your Web Forms project used DB-first Entity Framework with an .edmx file.")
            [void]$sb.AppendLine("# Instead of adapting EF6 models, use 'dotnet ef dbcontext scaffold' to")
            [void]$sb.AppendLine("# generate fresh EF Core models directly from your database.")
            [void]$sb.AppendLine("#")
            if ($entityNames.Count -gt 0) {
                [void]$sb.AppendLine("# Entities found in .edmx: $($entityNames -join ', ')")
            }
            [void]$sb.AppendLine("# Original context name: $contextName")
            if ($edmxNamespace) {
                [void]$sb.AppendLine("# Original namespace: $edmxNamespace")
            }
            [void]$sb.AppendLine("#")
            [void]$sb.AppendLine("# Run this command to generate EF Core models from your database:")
            [void]$sb.AppendLine("dotnet ef dbcontext scaffold `"$connectionString`" $providerName --output-dir Models --context $contextName $namespaceArgText--force")
            [void]$sb.AppendLine("#")
            if ($entityNames.Count -gt 0) {
                [void]$sb.AppendLine("# To scaffold only specific tables, add --table flags:")
                $tableExamples = ($entityNames | Select-Object -First 3 | ForEach-Object { "--table $_ " }) -join ''
                [void]$sb.AppendLine("# dotnet ef dbcontext scaffold `"$connectionString`" $providerName --output-dir Models --context $contextName $namespaceArgText$tableExamples--force")
            }
            [void]$sb.AppendLine("#")
            [void]$sb.AppendLine("# Prerequisites:")
            [void]$sb.AppendLine("#   dotnet tool install --global dotnet-ef")
            [void]$sb.AppendLine("#   dotnet add package Microsoft.EntityFrameworkCore.Design")
            [void]$sb.AppendLine("#   dotnet add package $providerName")
            [void]$sb.AppendLine("")

            $scaffoldText = $sb.ToString()

            # Write scaffold-command.txt
            $scaffoldFile = Join-Path $OutputDir 'scaffold-command.txt'
            if ($PSCmdlet.ShouldProcess($scaffoldFile, "Write EF Core scaffold command from $($edmxFile.Name)")) {
                # Append if multiple .edmx files
                if (Test-Path $scaffoldFile) {
                    Add-Content -Path $scaffoldFile -Value $scaffoldText -Encoding UTF8
                } else {
                    Set-Content -Path $scaffoldFile -Value $scaffoldText -Encoding UTF8
                }
                Write-Layer2Log -File 'scaffold-command.txt' -Pattern 'EdmxScaffold' -Detail "Generated scaffold command for $($edmxFile.Name) ($($entityNames.Count) entities)"
            }

            # Also prepend as a comment block to Program.cs if it exists
            $programCs = Join-Path $OutputDir 'Program.cs'
            if ((Test-Path $programCs) -and $PSCmdlet.ShouldProcess($programCs, "Add scaffold command comment")) {
                $programContent = Get-Content -Path $programCs -Raw -Encoding UTF8
                if ($programContent -notmatch 'dotnet ef dbcontext scaffold') {
                    $commentBlock = "// ============================================================================`n"
                    $commentBlock += "// TODO: Generate EF Core models from your database using:`n"
                    $commentBlock += "// dotnet ef dbcontext scaffold `"$connectionString`" $providerName --output-dir Models --context $contextName $namespaceArgText--force`n"
                    $commentBlock += "// See scaffold-command.txt for full details and options.`n"
                    $commentBlock += "// ============================================================================`n`n"
                    $programContent = $commentBlock + $programContent
                    Set-Content -Path $programCs -Value $programContent -Encoding UTF8
                    Write-Layer2Log -File 'Program.cs' -Pattern 'EdmxScaffold' -Detail "Added scaffold command reference to top of Program.cs"
                }
            }

            Write-ManualItem -File $relPath -Category 'EdmxScaffold' -Detail "DB-first .edmx detected ($($entityNames.Count) entities: $($entityNames -join ', ')). Run 'dotnet ef dbcontext scaffold' — see scaffold-command.txt"
        }

        $script:Summary.EdmxScaffold++
        
        # Store scaffold status for summary
        if ($scaffoldSuccess) {
            $script:Summary['EdmxAutoScaffolded'] = ($script:Summary['EdmxAutoScaffolded'] ?? 0) + 1
        }
    }
}

# ============================================================================
# WebMethod → Minimal API Detection
# ============================================================================
#
# Web Forms pages can have [WebMethod] attributed static methods that serve as
# AJAX endpoints. These should become Minimal API endpoints in Blazor.

function Convert-WebMethodToMinimalApi {
    param(
        [string]$SourceDir,
        [string]$OutputDir
    )

    # Scan both source .aspx.cs files (if SourceDir provided) and output .razor.cs files
    # Prefer source files when available (they have the original signatures)
    # Only fall back to .razor.cs if no source is provided
    $webMethodFiles = @()

    if ($SourceDir -and (Test-Path $SourceDir)) {
        $webMethodFiles += @(Get-ChildItem -Path $SourceDir -Filter '*.aspx.cs' -Recurse -File -ErrorAction SilentlyContinue)
    } else {
        # Only scan output .razor.cs files if no source directory provided
        $webMethodFiles += @(Get-ChildItem -Path $OutputDir -Filter '*.razor.cs' -Recurse -File -ErrorAction SilentlyContinue)
    }

    $webMethodRegex = [regex]'(?ms)\[(?:System\.Web\.Services\.)?WebMethod(?:\([^\)]*\))?\]\s*public\s+static\s+(\w+(?:<[^>]+>)?)\s+(\w+)\s*\(([^\)]*)\)'
    $totalMethods = 0

    foreach ($file in $webMethodFiles) {
        $content = Get-Content -Path $file.FullName -Raw -Encoding UTF8
        if ($content -notmatch '\[(?:System\.Web\.Services\.)?WebMethod') { continue }

        $matches_ = $webMethodRegex.Matches($content)
        if ($matches_.Count -eq 0) { continue }

        $relPath = $file.FullName
        if ($SourceDir -and $file.FullName.StartsWith($SourceDir)) {
            $relPath = $file.FullName.Substring($SourceDir.Length).TrimStart('\', '/')
        } elseif ($file.FullName.StartsWith($OutputDir)) {
            $relPath = $file.FullName.Substring($OutputDir.Length).TrimStart('\', '/')
        }

        # Determine the page name for API route generation
        $pageName = [System.IO.Path]::GetFileNameWithoutExtension(
            [System.IO.Path]::GetFileNameWithoutExtension($file.Name)
        )
        # Strip .aspx or .razor from the page name
        $pageName = $pageName -replace '\.(aspx|razor)$', ''

        Write-Layer2Log -File $relPath -Pattern 'WebMethod' -Detail "Found $($matches_.Count) WebMethod(s) in $($file.Name)"

        $stubs = [System.Text.StringBuilder]::new()
        [void]$stubs.AppendLine('')
        [void]$stubs.AppendLine('// ============================================================================')
        [void]$stubs.AppendLine("// WebMethod → Minimal API Stubs (from $($file.Name))")
        [void]$stubs.AppendLine('// ============================================================================')
        [void]$stubs.AppendLine('// The following [WebMethod] static methods were detected in the original')
        [void]$stubs.AppendLine('// Web Forms page. They served as AJAX endpoints and should be converted')
        [void]$stubs.AppendLine('// to Minimal API endpoints in Program.cs.')
        [void]$stubs.AppendLine('//')

        foreach ($m in $matches_) {
            $returnType = $m.Groups[1].Value
            $methodName = $m.Groups[2].Value
            $paramsRaw = $m.Groups[3].Value.Trim()
            $totalMethods++

            # Parse parameters
            $paramList = @()
            $routeParams = @()
            if ($paramsRaw) {
                $paramParts = $paramsRaw -split '\s*,\s*'
                foreach ($pp in $paramParts) {
                    $pp = $pp.Trim()
                    if ($pp -match '(\S+)\s+(\w+)') {
                        $pType = $Matches[1]
                        $pName = $Matches[2]
                        $paramList += "$pType $pName"
                        $routeParams += "($pType $pName)"
                    }
                }
            }

            # Determine HTTP method: GET for no params or simple types, POST for complex
            $httpMethod = if ($paramList.Count -eq 0 -or $returnType -match '^(string|int|bool|double|float|decimal|long)$') { 'MapGet' } else { 'MapPost' }

            $paramSignature = if ($routeParams.Count -gt 0) { ($routeParams -join ', ') } else { '()' }

            [void]$stubs.AppendLine("// TODO: WebMethod ""$methodName"" was converted from a Web Forms AJAX endpoint.")
            [void]$stubs.AppendLine("// Original: [WebMethod] public static $returnType $methodName($paramsRaw)")
            [void]$stubs.AppendLine("// Add to Program.cs:")
            [void]$stubs.AppendLine("// app.$httpMethod(""/api/$pageName/$methodName"", $paramSignature => { /* original logic */ });")
            [void]$stubs.AppendLine('//')

            Write-ManualItem -File $relPath -Category 'WebMethod' -Detail "[WebMethod] $returnType $methodName($paramsRaw) → app.$httpMethod(""/api/$pageName/$methodName"", ...)"
        }

        # Append stubs to the corresponding .razor.cs file in the output
        $razorCsPath = $null
        if ($file.Extension -eq '.cs' -and $file.Name -like '*.razor.cs') {
            $razorCsPath = $file.FullName
        } else {
            # Map source .aspx.cs to output .razor.cs
            $razorCsName = ($file.Name -replace '\.aspx\.cs$', '.razor.cs')
            $razorCsFiles = @(Get-ChildItem -Path $OutputDir -Filter $razorCsName -Recurse -File -ErrorAction SilentlyContinue)
            if ($razorCsFiles) {
                $razorCsPath = $razorCsFiles[0].FullName
            }
        }

        if ($razorCsPath -and (Test-Path $razorCsPath)) {
            if ($PSCmdlet.ShouldProcess($razorCsPath, "Append WebMethod → Minimal API stubs")) {
                $existingContent = Get-Content -Path $razorCsPath -Raw -Encoding UTF8
                if ($existingContent -notmatch 'WebMethod.*Minimal API Stubs') {
                    Add-Content -Path $razorCsPath -Value $stubs.ToString() -Encoding UTF8
                    Write-Layer2Log -File $razorCsPath -Pattern 'WebMethod' -Detail "Appended $($matches_.Count) Minimal API stub(s)"
                }
            }
        }

        # Also generate a consolidated snippet for Program.cs
        $programCs = Join-Path $OutputDir 'Program.cs'
        if ((Test-Path $programCs) -and $PSCmdlet.ShouldProcess($programCs, "Add WebMethod API endpoint stubs")) {
            $programContent = Get-Content -Path $programCs -Raw -Encoding UTF8
            if ($programContent -notmatch "// WebMethod endpoints from $pageName") {
                $apiBlock = "`n// --- WebMethod endpoints from $pageName (converted from [WebMethod] AJAX) ---"
                foreach ($m in $matches_) {
                    $returnType = $m.Groups[1].Value
                    $methodName = $m.Groups[2].Value
                    $paramsRaw = $m.Groups[3].Value.Trim()

                    $routeParams = @()
                    if ($paramsRaw) {
                        $paramParts = $paramsRaw -split '\s*,\s*'
                        foreach ($pp in $paramParts) {
                            if ($pp.Trim() -match '(\S+)\s+(\w+)') {
                                $routeParams += "($($Matches[1]) $($Matches[2]))"
                            }
                        }
                    }
                    $httpMethod = if ($routeParams.Count -eq 0 -or $returnType -match '^(string|int|bool|double|float|decimal|long)$') { 'MapGet' } else { 'MapPost' }
                    $paramSignature = if ($routeParams.Count -gt 0) { ($routeParams -join ', ') } else { '()' }
                    $apiBlock += "`n// TODO: app.$httpMethod(""/api/$pageName/$methodName"", $paramSignature => { /* original logic from $($file.Name) */ });"
                }
                $apiBlock += "`n"

                $programContent = $programContent.Replace('app.Run();', "${apiBlock}`napp.Run();")
                Set-Content -Path $programCs -Value $programContent -Encoding UTF8
                Write-Layer2Log -File 'Program.cs' -Pattern 'WebMethod' -Detail "Added $($matches_.Count) API endpoint stub(s) for $pageName"
            }
        }

        $script:Summary.WebMethod += $matches_.Count
    }
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
    $hasSelectMethod = $content -match 'SelectMethod|IQueryable<|DataBind|ItemType'
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

    # Extract [SupplyParameterFromQuery] parameters
    # Two patterns: 
    # 1. Method parameter: [SupplyParameterFromQuery(Name = "X")] type? name,  or  type? name)
    # 2. Property: [SupplyParameterFromQuery(Name = "X")] public type? Name { get; set; }
    $queryParams = @()
    
    # Pattern 1: Method parameters - [SupplyParameterFromQuery(Name = "X")] type name
    $queryMethodParamRegex = [regex]'\[SupplyParameterFromQuery\(Name\s*=\s*"([^"]+)"\)\]\s*(\w+\??)\s+(\w+)\s*[,)]'
    $queryMethodMatches = $queryMethodParamRegex.Matches($content)
    foreach ($m in $queryMethodMatches) {
        $queryParams += @{
            Name      = $m.Groups[1].Value
            Type      = $m.Groups[2].Value
            Property  = (Get-Culture).TextInfo.ToTitleCase($m.Groups[3].Value)  # Capitalize for property
        }
    }
    
    # Pattern 2: Property declarations - [SupplyParameterFromQuery(Name = "X")] public type? Name { get; set; }
    $queryPropRegex = [regex]'\[SupplyParameterFromQuery\(Name\s*=\s*"([^"]+)"\)\]\s*(?:public|private)\s+(\w+\??)\s+(\w+)\s*\{'
    $queryPropMatches = $queryPropRegex.Matches($content)
    foreach ($m in $queryPropMatches) {
        # Avoid duplicates if both patterns matched
        $existingNames = $queryParams | ForEach-Object { $_.Name }
        if ($m.Groups[1].Value -notin $existingNames) {
            $queryParams += @{
                Name      = $m.Groups[1].Value
                Type      = $m.Groups[2].Value
                Property  = $m.Groups[3].Value
            }
        }
    }

    # Extract [Parameter] (from RouteData) parameters
    # Two patterns:
    # 1. Method parameter: [Parameter] type name,  or  type name)
    # 2. Property: [Parameter] public type? Name { get; set; }
    $routeParams = @()
    
    # Pattern 1: Method parameters - [Parameter] type name (may have newline/TODO comment between)
    $routeMethodParamRegex = [regex]'\[Parameter\]\s*(\w+\??)\s+(\w+)\s*[,)]'
    $routeMethodMatches = $routeMethodParamRegex.Matches($content)
    foreach ($m in $routeMethodMatches) {
        $routeParams += @{
            Type     = $m.Groups[1].Value
            Property = (Get-Culture).TextInfo.ToTitleCase($m.Groups[2].Value)  # Capitalize for property
        }
    }
    
    # Pattern 2: Property declarations - [Parameter] public type? Name { get; set; }
    $routePropRegex = [regex]'\[Parameter\]\s*(?:public|private)\s+(\w+\??)\s+(\w+)\s*\{'
    $routePropMatches = $routePropRegex.Matches($content)
    foreach ($m in $routePropMatches) {
        # Avoid duplicates
        $existingProps = $routeParams | ForEach-Object { $_.Property }
        if ($m.Groups[2].Value -notin $existingProps) {
            $routeParams += @{
                Type     = $m.Groups[1].Value
                Property = $m.Groups[2].Value
            }
        }
    }

    # Detect what kind of data access this page does
    $isSingleItem = $content -match 'FirstOrDefault|SingleOrDefault|Find\(' -or $className -match 'Detail'
    $isFilteredList = $content -match 'Where\s*\(' -or ($queryParams.Count -gt 0)

    # Determine the entity type dynamically
    $entityType = Find-EntityType -CodeBehindPath $CodeBehindPath -Content $content
    if (-not $entityType) {
        $entityType = 'object'
        Write-Layer2Log -File $CodeBehindPath -Pattern 'PatternA' -Detail "Could not detect entity type — falling back to 'object'"
    }

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
    if ($entityType -eq 'object') {
        $newContent += "        // TODO: Replace 'object' with the actual entity type for this page`n"
    }
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
            $dbSetName = Find-DbSetName -EntityType $entityType -ProjectRoot $Path
            $newContent += "                var found = await db.${dbSetName}.FirstOrDefaultAsync(e => e.$($qp.Name) == $($qp.Property));`n"
            $newContent += "                if (found != null)`n"
            $newContent += "                {`n"
            $newContent += "                    _item = new List<${entityType}> { found };`n"
            $newContent += "                }`n"
            $newContent += "            }`n"
        } elseif ($isFilteredList -and $queryParams.Count -gt 0) {
            $qp = $queryParams[0]
            $dbSetName = Find-DbSetName -EntityType $entityType -ProjectRoot $Path
            $newContent += "            IQueryable<${entityType}> query = db.${dbSetName};`n"
            $newContent += "`n"
            $newContent += "            if ($($qp.Property).HasValue && $($qp.Property) > 0)`n"
            $newContent += "            {`n"
            $newContent += "                query = query.Where(e => e.$($qp.Name) == $($qp.Property));`n"
            $newContent += "            }`n"
            $newContent += "`n"
            $newContent += "            ${listField} = await query.ToListAsync();`n"
        } else {
            $dbSetName = Find-DbSetName -EntityType $entityType -ProjectRoot $Path
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
            if ($content -match 'class\s+(\w+Initializer|\w+Seeder|Seed\w+)' -or
                $content -match 'class\s+(\w+)\s*[^{]*:\s*(?:DropCreateDatabase\w*|CreateDatabaseIfNotExists|IDatabaseInitializer)') {
                $hasSeedData = $true
                $seedClass = $Matches[1]
                break
            }
        }
    }

    # Build connection string based on provider
    $connString = switch ($Provider) {
        'SqlServer'  { "Server=(localdb)\\mssqllocaldb;Database=$Namespace;Trusted_Connection=True" }
        'PostgreSQL' { "Host=localhost;Database=$($Namespace.ToLower());Username=postgres;Password=postgres" }
        'InMemory'   { $Namespace }
    }

    $providerMethod = switch ($Provider) {
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
    [void]$sb.AppendLine('using Microsoft.AspNetCore.Rewrite;')
    [void]$sb.AppendLine('using Microsoft.EntityFrameworkCore;')
    [void]$sb.AppendLine("using ${Namespace}.Models;")
    [void]$sb.AppendLine('')
    [void]$sb.AppendLine('var builder = WebApplication.CreateBuilder(args);')
    [void]$sb.AppendLine('')
    [void]$sb.AppendLine('builder.Services.AddRazorComponents();')
    [void]$sb.AppendLine('')
    [void]$sb.AppendLine('builder.Services.AddHttpContextAccessor();  // Required for BWFC GridView/DetailsView')
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
    [void]$sb.AppendLine('')
    [void]$sb.AppendLine('// ASPX URL backward compatibility — redirect .aspx URLs to Blazor routes')
    [void]$sb.AppendLine('var rewriteOptions = new RewriteOptions()')
    [void]$sb.AppendLine('    .AddRedirect(@"^Default\.aspx$", "/", statusCode: 301)')
    [void]$sb.AppendLine('    .AddRedirect(@"^(.+)\.aspx$", "$1", statusCode: 301);')
    [void]$sb.AppendLine('app.UseRewriter(rewriteOptions);')
    [void]$sb.AppendLine('')
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
# Pattern D: ItemType injection for data-bound components
# ============================================================================
#
# Blazor's generic type inference cannot determine ItemType from the Items binding.
# This pattern scans .razor files for data-bound components (GridView, DetailsView,
# FormView, ListView, DataList, Repeater) that are missing ItemType and injects
# the detected entity type based on:
#   1. Original Web Forms ItemType attribute (preserved by Layer 1)
#   2. SelectMethod return type from code-behind
#   3. IQueryable<T>/List<T> type from code-behind
#   4. DbSet<T> references
#   5. Model class name heuristics
#   6. Fallback to 'object' to allow compilation

function Test-PatternD {
    param([string]$RazorPath)

    if (-not (Test-Path $RazorPath)) { return $false }
    $content = Get-Content -Path $RazorPath -Raw -Encoding UTF8

    # Check if file contains data-bound components WITHOUT ItemType
    # Components that need ItemType: GridView, DetailsView, FormView, ListView, DataList, Repeater
    $dataBoundComponents = @('GridView', 'DetailsView', 'FormView', 'ListView', 'DataList', 'Repeater')
    
    foreach ($comp in $dataBoundComponents) {
        # Match component opening tag
        if ($content -match "<$comp\b") {
            # Check if ItemType is already present
            $tagPattern = "(?si)<$comp\b[^>]*>"
            $tagMatches = [regex]::Matches($content, $tagPattern)
            foreach ($m in $tagMatches) {
                $tag = $m.Value
                if ($tag -notmatch '\bItemType\s*=') {
                    return $true  # Found a component without ItemType
                }
            }
        }
    }

    return $false
}

function Invoke-PatternD {
    param(
        [string]$RazorPath,
        [string]$Namespace,
        [string]$ProjectRoot
    )

    $content = Get-Content -Path $RazorPath -Raw -Encoding UTF8

    # Find the companion code-behind to detect entity type
    $codeBehindPath = "$RazorPath.cs"
    $cbContent = ''
    if (Test-Path $codeBehindPath) {
        $cbContent = Get-Content -Path $codeBehindPath -Raw -Encoding UTF8
    }

    # Also check for @code block in the .razor file
    $razorCodeBlock = ''
    if ($content -match '@code\s*\{') {
        $razorCodeBlock = $content
    }

    # Combine all code sources for entity detection
    $allCode = $cbContent + "`n" + $razorCodeBlock

    # Detect entity type from the code
    $entityType = $null

    # Check for IQueryable<T>, List<T>, IEnumerable<T> patterns
    if ($allCode -match 'IQueryable<(\w+)>') { $entityType = $Matches[1] }
    elseif ($allCode -match 'List<(\w+)>\s+\w+\s*=') { $entityType = $Matches[1] }
    elseif ($allCode -match 'IEnumerable<(\w+)>') { $entityType = $Matches[1] }
    elseif ($allCode -match 'private\s+(?:List|IList|IEnumerable)<(\w+)>') { $entityType = $Matches[1] }

    # If still not found, check DbSet references in Models folder
    if (-not $entityType -and $ProjectRoot) {
        $modelsDir = Join-Path $ProjectRoot 'Models'
        if (Test-Path $modelsDir) {
            $modelClasses = @()
            foreach ($f in (Get-ChildItem -Path $modelsDir -Filter '*.cs' -Recurse)) {
                $mc = Get-Content -Path $f.FullName -Raw -Encoding UTF8
                $classMatches = [regex]::Matches($mc, 'public\s+class\s+(\w+)')
                foreach ($cm in $classMatches) {
                    $cn = $cm.Groups[1].Value
                    if ($cn -notmatch 'Context$|Initializer$|Seeder$|Configuration$|Migration') {
                        $modelClasses += $cn
                    }
                }
            }
            # Match any model class mentioned in the code
            foreach ($mc in $modelClasses) {
                if ($allCode -match "\b$mc\b") {
                    $entityType = $mc
                    break
                }
            }
        }
    }

    # Fallback to 'object' if no entity type detected
    if (-not $entityType) {
        $entityType = 'object'
    }

    # Inject ItemType into data-bound components that lack it
    $dataBoundComponents = @('GridView', 'DetailsView', 'FormView', 'ListView', 'DataList', 'Repeater')
    $injectionCount = 0

    foreach ($comp in $dataBoundComponents) {
        # Match component opening tags WITHOUT ItemType
        # Pattern: <GridView followed by attributes, NOT containing ItemType, ending with >
        $pattern = "(?si)<$comp\b([^>]*)>"
        
        $tagMatches = [regex]::Matches($content, $pattern)
        foreach ($tm in $tagMatches) {
            $fullTag = $tm.Value
            $attrs = $tm.Groups[1].Value
            
            # Skip if ItemType already present
            if ($attrs -match '\bItemType\s*=') {
                continue
            }
            
            # Inject ItemType as first attribute
            $newTag = "<$comp ItemType=`"$entityType`"$attrs>"
            $content = $content.Replace($fullTag, $newTag)
            $injectionCount++
        }
    }

    if ($injectionCount -gt 0) {
        $outputPath = Get-OutputPath -OriginalPath $RazorPath
        if ($PSCmdlet.ShouldProcess($outputPath, "Pattern D: Inject ItemType into data-bound components")) {
            Set-Content -Path $outputPath -Value $content -Encoding UTF8
            $relPath = $RazorPath.Replace($ProjectRoot, '').TrimStart('\', '/')
            Write-Layer2Log -File $relPath -Pattern 'PatternD' -Detail "Injected ItemType=`"$entityType`" into $injectionCount component(s)"
            $script:Summary.PatternD++
        }
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

# Auto-detect DbProvider from source if not specified (migration boundary rule: preserve original DB technology)
if (-not $DbProvider) {
    $detectedProvider = 'SqlServer'  # Safe default — most Web Forms apps use SQL Server
    if ($SourcePath -and (Test-Path $SourcePath)) {
        # Check Web.config for connection string hints
        $webConfig = Join-Path $SourcePath 'Web.config'
        if (Test-Path $webConfig) {
            $configContent = Get-Content $webConfig -Raw -ErrorAction SilentlyContinue
            if ($configContent -match 'LocalDB|SqlServer|SQLEXPRESS|\.mdf|Initial Catalog') {
                $detectedProvider = 'SqlServer'
            }
            elseif ($configContent -match 'Npgsql|PostgreSQL') {
                $detectedProvider = 'PostgreSQL'
            }
        }
        # Check for .edmx files (implies SQL Server in most cases)
        $edmxFiles = @(Get-ChildItem -Path $SourcePath -Filter '*.edmx' -Recurse -ErrorAction SilentlyContinue)
        if ($edmxFiles.Count -gt 0) {
            $detectedProvider = 'SqlServer'
        }
    }
    $DbProvider = $detectedProvider
    Write-Host "  DbProvider: $DbProvider (auto-detected)" -ForegroundColor DarkGray
}
else {
    Write-Host "  DbProvider: $DbProvider (specified)" -ForegroundColor DarkGray
}

Write-Host "  Namespace:  $ns" -ForegroundColor DarkGray
Write-Host "  DbContext:  $(if ($dbCtx) { $dbCtx } else { '(none detected)' })" -ForegroundColor DarkGray
if ($SourcePath) {
    Write-Host "  SourcePath: $SourcePath" -ForegroundColor DarkGray
}
Write-Host ''

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

# ── Pattern D: ItemType Injection ──
Write-Host ''
Write-Host '── Pattern D: ItemType Injection for Data-Bound Components ──' -ForegroundColor Blue
$razorFilesD = Get-ChildItem -Path $Path -Filter '*.razor' -Recurse -ErrorAction SilentlyContinue |
    Where-Object { $_.Name -notmatch '\.razor\.cs$' }
$patternDCount = 0
foreach ($rf in $razorFilesD) {
    if (Test-PatternD -RazorPath $rf.FullName) {
        Invoke-PatternD -RazorPath $rf.FullName -Namespace $ns -ProjectRoot $Path
        $patternDCount++
    }
}
if ($patternDCount -eq 0) {
    Write-Host '  No Pattern D candidates detected (all components have ItemType).' -ForegroundColor DarkGray
}

# ── EDMX Scaffold: Detect DB-first .edmx files ──
# (Runs after Pattern C so Program.cs exists and won't be overwritten)
Write-Host ''
Write-Host '── EDMX → EF Core Scaffold Command ──' -ForegroundColor Magenta
if ($SourcePath -and (Test-Path $SourcePath)) {
    Convert-EdmxToScaffold -SourceDir $SourcePath -OutputDir $Path
    if ($script:Summary.EdmxScaffold -eq 0) {
        Write-Host '  No .edmx files detected in source project.' -ForegroundColor DarkGray
    }
} else {
    Write-Host '  No -SourcePath provided — skipping .edmx detection.' -ForegroundColor DarkGray
}

# ── WebMethod → Minimal API Detection ──
# (Runs after Pattern A/C so .razor.cs and Program.cs are in final form)
Write-Host ''
Write-Host '── WebMethod → Minimal API Detection ──' -ForegroundColor Magenta
Convert-WebMethodToMinimalApi -SourceDir $SourcePath -OutputDir $Path
if ($script:Summary.WebMethod -eq 0) {
    Write-Host '  No [WebMethod] methods detected.' -ForegroundColor DarkGray
}

# ── Summary ──
Write-Host ''
Write-Host '╔══════════════════════════════════════════════════════════╗' -ForegroundColor Cyan
Write-Host '║  Layer 2 Summary                                        ║' -ForegroundColor Cyan
Write-Host '╠══════════════════════════════════════════════════════════╣' -ForegroundColor Cyan

# EDMX scaffolding summary with auto/manual distinction
$autoScaffolded = $script:Summary['EdmxAutoScaffolded'] ?? 0
$manualScaffold = $script:Summary.EdmxScaffold - $autoScaffolded
if ($autoScaffolded -gt 0 -or $manualScaffold -gt 0) {
    if ($autoScaffolded -gt 0) {
        Write-Host "║  EDMX Auto-scaffolded:                $autoScaffolded file(s)         ║" -ForegroundColor Green
    }
    if ($manualScaffold -gt 0) {
        Write-Host "║  EDMX Manual scaffold needed:         $manualScaffold file(s)         ║" -ForegroundColor Yellow
    }
} else {
    Write-Host "║  EDMX Scaffold Commands:              $($script:Summary.EdmxScaffold) file(s)         ║" -ForegroundColor Magenta
}

Write-Host "║  WebMethod → Minimal API:             $($script:Summary.WebMethod) method(s)       ║" -ForegroundColor Magenta
Write-Host "║  Pattern A (FormView→ComponentBase):  $($script:Summary.PatternA) file(s)         ║" -ForegroundColor Cyan
Write-Host "║  Pattern B (Auth Simplification):     $($script:Summary.PatternB) file(s)         ║" -ForegroundColor Green
Write-Host "║  Pattern C (Program.cs Bootstrap):    $($script:Summary.PatternC) file(s)         ║" -ForegroundColor Yellow
Write-Host "║  Pattern D (ItemType Injection):      $($script:Summary.PatternD) file(s)         ║" -ForegroundColor Blue
Write-Host "║  Skipped (already transformed):       $($script:Summary.Skipped) file(s)         ║" -ForegroundColor DarkGray
Write-Host '╚══════════════════════════════════════════════════════════╝' -ForegroundColor Cyan
Write-Host ''

# Manual items report
if ($script:ManualItems.Count -gt 0) {
    Write-Host '--- Items Needing Manual Attention (Layer 2) ---' -ForegroundColor Yellow
    $grouped = $script:ManualItems | Group-Object -Property Category
    foreach ($group in $grouped) {
        Write-Host "  [$($group.Name)] ($($group.Count) item(s)):" -ForegroundColor Yellow
        foreach ($item in $group.Group) {
            Write-Host "    • $($item.File): $($item.Detail)"
        }
    }
    Write-Host ''

    # Write manual-items report to file
    $manualItemsPath = Join-Path $Path 'layer2-manual-items.md'
    $mdContent = [System.Text.StringBuilder]::new()
    [void]$mdContent.AppendLine('# Layer 2 Manual Items')
    [void]$mdContent.AppendLine('')
    [void]$mdContent.AppendLine('Items that require manual developer attention after Layer 2 transforms.')
    [void]$mdContent.AppendLine('')
    foreach ($group in $grouped) {
        [void]$mdContent.AppendLine("## $($group.Name) ($($group.Count) item(s))")
        [void]$mdContent.AppendLine('')
        foreach ($item in $group.Group) {
            [void]$mdContent.AppendLine("- **$($item.File)**: $($item.Detail)")
        }
        [void]$mdContent.AppendLine('')
    }
    Set-Content -Path $manualItemsPath -Value $mdContent.ToString() -Encoding UTF8
    Write-Host "Manual items report written to: $manualItemsPath" -ForegroundColor DarkGray
}

# Export transform log
if ($script:TransformLog.Count -gt 0) {
    $logPath = Join-Path $Path 'layer2-transforms.log'
    $script:TransformLog | ForEach-Object {
        "[$($_.Timestamp.ToString('HH:mm:ss'))] [$($_.Pattern)] $($_.File) — $($_.Detail)"
    } | Set-Content -Path $logPath -Encoding UTF8
    Write-Host "Transform log written to: $logPath" -ForegroundColor DarkGray
}

Write-Host 'Layer 2 complete.' -ForegroundColor Green
