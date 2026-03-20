# Decision: Roslyn Analyzer Documentation Placement and Structure

**Author:** Beast (Technical Writer)  
**Date:** 2026-03-XX  
**Status:** Delivered

## Context

The BWFC project has 8 Roslyn analyzers with code fixes in `BlazorWebFormsComponents.Analyzers`. These needed comprehensive documentation for developers using the analyzer package post-migration.

## Decision

1. **Placement:** `docs/Migration/Analyzers.md` under the Migration section, positioned after "Automated Migration Guide" and before "Deprecation Guidance". Rationale: analyzers are a post-migration tool — developers run automation first, then use analyzers to clean up code-behind patterns the script couldn't address.

2. **Structure:** Each analyzer gets a dedicated section with: what it detects, why it matters, before/after code, and code fix description. This matches the pattern established in DeprecationGuidance.md.

3. **Cross-references:** Links to existing utility docs (ViewState, Response.Redirect) where the analyzer topic overlaps with a BWFC compatibility shim.

4. **Migration readme update:** Added "Clean Up with Roslyn Analyzers" section to `docs/Migration/readme.md` as a follow-up step in the migration journey.

## Impact

- New file: `docs/Migration/Analyzers.md` (~17.7 KB)
- Updated: `mkdocs.yml` (nav entry), `docs/Migration/readme.md` (new section)
- MkDocs strict build passes
