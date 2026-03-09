---
name: webforms-runner
description: "**WORKFLOW SKILL** - Starts ASP.NET Web Forms applications using IIS Express and captures screenshots with Playwright. USE FOR: running WebForms samples, capturing 'before' screenshots for migration documentation, testing original Web Forms applications. DO NOT USE FOR: running Blazor apps (use dotnet run), running .NET Core apps (use dotnet CLI). INVOKES: IIS Express CLI, Playwright MCP browser tools."
---

# WebForms Runner Skill

This skill enables running ASP.NET Web Forms applications using IIS Express and capturing screenshots with Playwright for documentation purposes.

## Prerequisites

- **IIS Express** installed (typically at `C:\Program Files (x86)\IIS Express\iisexpress.exe`)
- **Visual Studio Build Tools** or **Visual Studio** (for MSBuild)
- **.NET Framework 4.x** installed
- **Playwright MCP tools** available (browser_navigate, browser_snapshot, browser_take_screenshot)

## Available Web Forms Samples

This repository contains two Web Forms sample applications:

| Sample | Path | Port | Database |
|--------|------|------|----------|
| **ContosoUniversity** | `samples/ContosoUniversity/ContosoUniversity/` | 52477 | SQL Server LocalDB |
| **WingtipToys** | `samples/WingtipToys/WingtipToys/` | 44300 | SQL Server LocalDB |

## Starting a Web Forms Application

### Step 1: Build the Project

Web Forms projects require MSBuild (not `dotnet build`):

```powershell
# ContosoUniversity
msbuild "samples\ContosoUniversity\ContosoUniversity\ContosoUniversity.csproj" /p:Configuration=Debug /t:Build /v:m

# WingtipToys  
msbuild "samples\WingtipToys\WingtipToys\WingtipToys.csproj" /p:Configuration=Debug /t:Build /v:m
```

If NuGet packages are missing, restore first:
```powershell
nuget restore "samples\ContosoUniversity\ContosoUniversity.sln"
```

### Step 2: Start IIS Express

Use the `/path` option to run directly from the project folder:

```powershell
# ContosoUniversity (port 52477)
& "C:\Program Files (x86)\IIS Express\iisexpress.exe" /path:"D:\BlazorWebFormsComponents\samples\ContosoUniversity\ContosoUniversity" /port:52477

# WingtipToys (port 44300)
& "C:\Program Files (x86)\IIS Express\iisexpress.exe" /path:"D:\BlazorWebFormsComponents\samples\WingtipToys\WingtipToys" /port:44300
```

**Important:** Run IIS Express in async mode with `detach: true` since it's a server process:
```
mode: "async", detach: true
```

### Step 3: Verify the Site is Running

Wait 3-5 seconds for startup, then check with:
```powershell
curl http://localhost:52477 -UseBasicParsing | Select-Object -ExpandProperty StatusCode
```

## IIS Express Command Reference

```
iisexpress.exe [options]

Key options:
  /path:app-path         Physical path to the application folder
  /port:port-number      Port to bind (default: 8080)
  /clr:clr-version       .NET Framework version (e.g., v4.0)
  /systray:true|false    Show system tray icon (default: true)
```

## Capturing Screenshots with Playwright

Use the Playwright MCP tools to capture screenshots of the running Web Forms application.

### Workflow for Documentation Screenshots

1. **Navigate to the page:**
   ```
   playwright-browser_navigate: { url: "http://localhost:52477/Home.aspx" }
   ```

2. **Wait for page load:**
   ```
   playwright-browser_wait_for: { time: 2 }
   ```

3. **Take a snapshot (for accessibility tree):**
   ```
   playwright-browser_snapshot: {}
   ```

4. **Take a screenshot:**
   ```
   playwright-browser_take_screenshot: { 
     filename: "dev-docs/screenshots/webforms-home.png",
     type: "png"
   }
   ```

### ContosoUniversity Pages

| Page | URL | Screenshot Name |
|------|-----|-----------------|
| Home | `/Home.aspx` | `contoso-webforms-home.png` |
| Students | `/Students.aspx` | `contoso-webforms-students.png` |
| Courses | `/Courses.aspx` | `contoso-webforms-courses.png` |
| Instructors | `/Instructors.aspx` | `contoso-webforms-instructors.png` |
| About | `/About.aspx` | `contoso-webforms-about.png` |

### WingtipToys Pages

| Page | URL | Screenshot Name |
|------|-----|-----------------|
| Home | `/Default.aspx` | `wingtip-webforms-home.png` |
| Products | `/ProductList.aspx?ProductCategory=1` | `wingtip-webforms-products.png` |
| Product Detail | `/ProductDetails.aspx?ProductID=1` | `wingtip-webforms-detail.png` |
| Cart | `/ShoppingCart.aspx` | `wingtip-webforms-cart.png` |

### Screenshot Best Practices

1. **Use consistent naming:** `{app}-webforms-{page}.png` for before, `{app}-blazor-{page}.png` for after
2. **Save to documentation folder:** `dev-docs/migration-tests/{run}-screenshots/`
3. **Capture full page when needed:** Use `fullPage: true` for long pages
4. **Wait for dynamic content:** Add `browser_wait_for` before screenshots if page has AJAX

## Complete Example: ContosoUniversity Screenshots

```powershell
# 1. Build the project
msbuild "samples\ContosoUniversity\ContosoUniversity\ContosoUniversity.csproj" /p:Configuration=Debug /t:Build /v:m

# 2. Start IIS Express (async, detached)
# Use powershell tool with mode: "async", detach: true
& "C:\Program Files (x86)\IIS Express\iisexpress.exe" /path:"D:\BlazorWebFormsComponents\samples\ContosoUniversity\ContosoUniversity" /port:52477

# 3. Wait for startup
Start-Sleep -Seconds 5

# 4. Use Playwright MCP tools to capture screenshots:
#    - browser_navigate to http://localhost:52477/Home.aspx
#    - browser_wait_for time: 2
#    - browser_take_screenshot filename: "dev-docs/contoso-screenshots/webforms-home.png"
#    - Repeat for each page

# 5. Stop IIS Express when done
# Find the process ID from the async shell output
Stop-Process -Id <PID>
```

## Stopping IIS Express

Since IIS Express runs as a detached process, you need to stop it by PID:

```powershell
# Find IIS Express processes
Get-Process | Where-Object { $_.ProcessName -eq "iisexpress" }

# Stop by specific PID
Stop-Process -Id <PID>

# Or stop all IIS Express instances
Get-Process -Name "iisexpress" -ErrorAction SilentlyContinue | Stop-Process
```

## Troubleshooting

### Port Already in Use
```powershell
# Check what's using the port
Get-NetTCPConnection -LocalPort 52477 -ErrorAction SilentlyContinue

# Kill the process
Stop-Process -Id (Get-NetTCPConnection -LocalPort 52477).OwningProcess
```

### Missing NuGet Packages
```powershell
# Restore packages for the solution
nuget restore "samples\ContosoUniversity\ContosoUniversity.sln"
```

### Database Connection Issues
- Ensure SQL Server LocalDB is installed and running
- Check connection string in `Web.config`
- LocalDB instances can be started with: `sqllocaldb start MSSQLLocalDB`

### MSBuild Not Found
Add Visual Studio MSBuild to PATH or use full path:
```powershell
& "C:\Program Files\Microsoft Visual Studio\2022\Enterprise\MSBuild\Current\Bin\MSBuild.exe" project.csproj
```

## Side-by-Side Screenshots for Migration Documentation

When creating migration documentation, capture both WebForms and Blazor screenshots:

```
dev-docs/migration-tests/wingtiptoys-run15-screenshots/
├── webforms-home.png       # Before (WebForms)
├── blazor-home.png         # After (Blazor)
├── webforms-products.png
├── blazor-products.png
└── ...
```

This allows creating visual comparisons in migration reports showing the fidelity of the conversion.

## Integration with Migration Workflow

1. **Before migration:** Start WebForms app, capture baseline screenshots
2. **Run migration:** Execute bwfc-migrate.ps1 and bwfc-migrate-layer2.ps1
3. **After migration:** Start Blazor app (`dotnet run`), capture result screenshots
4. **Document:** Create side-by-side comparison in migration report

## Related Skills

- **webforms-migration** - For migrating ASPX/ASCX files to Blazor
- **documentation** - For writing migration reports with screenshots
