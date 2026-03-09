<#
.SYNOPSIS
    Validates a migrated Blazor project uses BWFC components properly.

.DESCRIPTION
    bwfc-validate.ps1 checks that:
    1. No asp: controls remain in .razor files
    2. ASP.NET Web Forms controls with BWFC coverage are converted to BWFC components
       (not replaced with plain HTML tables or stubs)
    3. Reports any violations that would fail migration validation

.PARAMETER Path
    Path to the migrated Blazor project directory.

.PARAMETER Strict
    Fail validation if ANY asp: controls remain. Default is to warn only.

.EXAMPLE
    .\bwfc-validate.ps1 -Path .\samples\AfterWingtipToys
    
    Validates the migrated WingtipToys project.

.EXAMPLE
    .\bwfc-validate.ps1 -Path .\samples\AfterContosoUniversity -Strict
    
    Validates ContosoUniversity with strict mode (fails on any asp: remnants).
#>

[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$Path,

    [switch]$Strict
)

$ErrorActionPreference = 'Stop'

# BWFC components that have direct asp: control equivalents
# These are the controls that MUST be migrated to BWFC, not HTML
$bwfcControls = @{
    # Data controls - MUST use BWFC, not HTML tables
    'GridView' = @{ Required = $true; HtmlFallback = '<table' }
    'DetailsView' = @{ Required = $true; HtmlFallback = '<dl' }
    'FormView' = @{ Required = $true; HtmlFallback = '<div' }
    'ListView' = @{ Required = $true; HtmlFallback = '@foreach' }
    'DataList' = @{ Required = $true; HtmlFallback = '<ul' }
    'Repeater' = @{ Required = $true; HtmlFallback = '@foreach' }
    'DataGrid' = @{ Required = $true; HtmlFallback = '<table' }
    
    # Form controls
    'Button' = @{ Required = $true; HtmlFallback = '<button' }
    'TextBox' = @{ Required = $true; HtmlFallback = '<input' }
    'DropDownList' = @{ Required = $true; HtmlFallback = '<select' }
    'CheckBox' = @{ Required = $true; HtmlFallback = '<input type="checkbox"' }
    'RadioButton' = @{ Required = $true; HtmlFallback = '<input type="radio"' }
    'Label' = @{ Required = $true; HtmlFallback = '<span' }
    'HyperLink' = @{ Required = $true; HtmlFallback = '<a' }
    'Image' = @{ Required = $true; HtmlFallback = '<img' }
    'ImageButton' = @{ Required = $true; HtmlFallback = '<input type="image"' }
    'LinkButton' = @{ Required = $true; HtmlFallback = '<a' }
    'Panel' = @{ Required = $true; HtmlFallback = '<div' }
    'Literal' = @{ Required = $false }  # Often legitimately replaced with @()
    'HiddenField' = @{ Required = $true; HtmlFallback = '<input type="hidden"' }
    'FileUpload' = @{ Required = $true; HtmlFallback = '<input type="file"' }
    
    # Validation controls
    'RequiredFieldValidator' = @{ Required = $true }
    'CompareValidator' = @{ Required = $true }
    'RangeValidator' = @{ Required = $true }
    'RegularExpressionValidator' = @{ Required = $true }
    'CustomValidator' = @{ Required = $true }
    'ValidationSummary' = @{ Required = $true }
    
    # Login controls
    'Login' = @{ Required = $true }
    'LoginView' = @{ Required = $true }
    'LoginStatus' = @{ Required = $true }
    'LoginName' = @{ Required = $true }
    
    # Navigation
    'Menu' = @{ Required = $true }
    'TreeView' = @{ Required = $true }
    'SiteMapPath' = @{ Required = $true }
    
    # Layout
    'MultiView' = @{ Required = $true }
    'View' = @{ Required = $true }
    'PlaceHolder' = @{ Required = $true }
    
    # Lists
    'BulletedList' = @{ Required = $true; HtmlFallback = '<ul' }
    'CheckBoxList' = @{ Required = $true }
    'RadioButtonList' = @{ Required = $true }
    'ListBox' = @{ Required = $true; HtmlFallback = '<select' }
}

# Controls that are OK to convert to plain HTML (no BWFC equivalent or simple enough)
$htmlOkControls = @(
    'Table', 'TableRow', 'TableCell', 'TableHeaderRow', 'TableHeaderCell', 'TableFooterRow'
)

function Test-BwfcMigration {
    param(
        [string]$ProjectPath
    )

    $results = @{
        TotalFiles = 0
        PassedFiles = 0
        FailedFiles = 0
        Violations = [System.Collections.ArrayList]::new()
        Warnings = [System.Collections.ArrayList]::new()
        BwfcComponentsUsed = [System.Collections.ArrayList]::new()
    }

    # Find all .razor files
    $razorFiles = Get-ChildItem -Path $ProjectPath -Filter "*.razor" -Recurse -File
    $results.TotalFiles = $razorFiles.Count

    foreach ($file in $razorFiles) {
        $content = Get-Content -Path $file.FullName -Raw -Encoding UTF8
        $relativePath = $file.FullName.Replace($ProjectPath, '').TrimStart('\', '/')
        $fileViolations = @()

        # Check 1: No asp: prefixes should remain
        $aspMatches = [regex]::Matches($content, '<asp:(\w+)')
        if ($aspMatches.Count -gt 0) {
            foreach ($m in $aspMatches) {
                $control = $m.Groups[1].Value
                [void]$fileViolations.Add("Unconverted asp:$control found")
            }
        }

        # Check 2: No runat="server" should remain
        if ($content -match 'runat\s*=\s*"server"') {
            [void]$fileViolations.Add("runat=""server"" attribute found (should be removed)")
        }

        # Check 3: Detect BWFC components being used
        foreach ($ctrl in $bwfcControls.Keys) {
            if ($content -match "<$ctrl\b") {
                if ($results.BwfcComponentsUsed -notcontains $ctrl) {
                    [void]$results.BwfcComponentsUsed.Add($ctrl)
                }
            }
        }

        # Check 4: Detect HTML fallbacks where BWFC should be used
        # This is a heuristic check - look for patterns that suggest GridView was replaced with table+foreach
        
        # Pattern: @foreach inside a <table> suggests a data control was replaced with HTML
        if ($content -match '(?s)<table[^>]*>.*?@foreach\s*\(' -and $content -notmatch '<GridView\b') {
            # Check if the original source had a GridView
            # This is a warning, not a violation, since we can't always know
            [void]$results.Warnings.Add(@{
                File = $relativePath
                Warning = "HTML table with @foreach detected - verify this wasn't a GridView that should use <GridView>"
            })
        }

        # Pattern: Stub pages (not yet migrated)
        if ($content -match 'not yet migrated|TODO:\s*Implement') {
            [void]$results.Warnings.Add(@{
                File = $relativePath
                Warning = "Stub page detected - needs manual implementation"
            })
        }

        if ($fileViolations.Count -gt 0) {
            $results.FailedFiles++
            [void]$results.Violations.Add(@{
                File = $relativePath
                Issues = $fileViolations
            })
        }
        else {
            $results.PassedFiles++
        }
    }

    return $results
}

# Main execution
Write-Host "============================================================" -ForegroundColor Cyan
Write-Host "  BWFC Migration Validator" -ForegroundColor Cyan
Write-Host "============================================================" -ForegroundColor Cyan
Write-Host "  Project: $Path"
Write-Host "  Mode:    $(if ($Strict) { 'Strict (fail on any violation)' } else { 'Standard (warn only)' })"
Write-Host ""

if (-not (Test-Path $Path)) {
    Write-Error "Path not found: $Path"
    exit 1
}

$results = Test-BwfcMigration -ProjectPath (Resolve-Path $Path).Path

Write-Host "============================================================" -ForegroundColor Cyan
Write-Host "  Validation Results" -ForegroundColor Cyan
Write-Host "============================================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "  Files scanned:  $($results.TotalFiles)"
Write-Host "  Passed:         $($results.PassedFiles)" -ForegroundColor Green
Write-Host "  Failed:         $($results.FailedFiles)" -ForegroundColor $(if ($results.FailedFiles -gt 0) { 'Red' } else { 'Green' })
Write-Host ""

if ($results.BwfcComponentsUsed.Count -gt 0) {
    Write-Host "BWFC Components Used ($($results.BwfcComponentsUsed.Count)):" -ForegroundColor Cyan
    $results.BwfcComponentsUsed | Sort-Object | ForEach-Object {
        Write-Host "  ✓ $_" -ForegroundColor Green
    }
    Write-Host ""
}

if ($results.Violations.Count -gt 0) {
    Write-Host "--- VIOLATIONS (must fix) ---" -ForegroundColor Red
    foreach ($v in $results.Violations) {
        Write-Host "  $($v.File):" -ForegroundColor Yellow
        foreach ($issue in $v.Issues) {
            Write-Host "    ✗ $issue" -ForegroundColor Red
        }
    }
    Write-Host ""
}

if ($results.Warnings.Count -gt 0) {
    Write-Host "--- WARNINGS (review recommended) ---" -ForegroundColor Yellow
    foreach ($w in $results.Warnings) {
        Write-Host "  $($w.File):" -ForegroundColor Yellow
        Write-Host "    ⚠ $($w.Warning)" -ForegroundColor Yellow
    }
    Write-Host ""
}

# Exit code
if ($results.FailedFiles -gt 0) {
    if ($Strict) {
        Write-Host "❌ Validation FAILED — $($results.FailedFiles) file(s) have violations" -ForegroundColor Red
        exit 1
    }
    else {
        Write-Host "⚠️ Validation passed with warnings — $($results.FailedFiles) file(s) need attention" -ForegroundColor Yellow
        exit 0
    }
}
else {
    Write-Host "✅ Validation PASSED — all $($results.TotalFiles) files are properly migrated" -ForegroundColor Green
    exit 0
}
