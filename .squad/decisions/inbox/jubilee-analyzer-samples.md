# Decision: Analyzer Samples Use /MigrationTools/ Route

**Author:** Jubilee
**Date:** 2026-03-20
**Status:** Implemented

## Context
Needed a sample page for the BWFC Roslyn analyzer suite. Analyzers are IDE tools, not UI components, so they don't fit under `/ControlSamples/` or `/UtilityFeatures/`.

## Decision
Created a new `/MigrationTools/` route category with its own nav section in ComponentList.razor. The page at `/MigrationTools/Analyzers` is an informational/reference page (no interactive demos) following the educational style of the Theming page.

Standalone before/after example files use `.cs.example` extension in `samples/AnalyzerExamples/` to distinguish reference examples from compilable code.

## Impact
- New nav category 'Migration Tools' in ComponentList.razor
- Establishes pattern for future migration tooling pages (e.g., migration scripts, config helpers)
