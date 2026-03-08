# Decision: ContosoUniversity Local Setup Configuration

**By:** Colossus
**Date:** 2026-03-08
**Status:** Implemented

## What

Configured the ContosoUniversity Web Forms sample project for local development using IIS Express + LocalDB. Three source-level fixes were required:

1. **Connection strings** → changed from `.\SQLEXPRESS` to `(localdb)\MSSQLLocalDB` in `Web.config`
2. **AjaxControlToolkit HintPath** → updated from broken Documents path to NuGet packages folder in `.csproj`
3. **NBGV inheritance block** → added empty `Directory.Build.props` at `samples/ContosoUniversity/` to prevent duplicate AssemblyVersion attributes

## Why

The sample project ships with assumptions about the original developer's environment (SQL Express, local file paths). These fixes make it reproducible on any machine with LocalDB and a compatible MSBuild. The NBGV block is necessary because this repo's root `Directory.Build.props` injects Nerdbank.GitVersioning, which conflicts with legacy .NET Framework projects that have manual `AssemblyInfo.cs` files.

## Impact

All 5 pages (Home, About, Students, Courses, Instructors) are verified working with screenshots stored in `dev-docs/contoso-screenshots/`. Setup is documented in `dev-docs/contoso-university-setup.md`.
