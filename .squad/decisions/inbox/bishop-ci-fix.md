# Decision: Add fetch-depth: 0 to Squad CI Workflow

**Date:** 2025-01-26  
**Decision Maker:** Bishop (Migration Tooling Dev)  
**Context:** PR #114 - Squad CI workflow failing with NBGV errors

## Problem

The `squad-ci.yml` GitHub Actions workflow was failing with:
```
Nerdbank.GitVersioning.GitException: Shallow clone lacks the object
```

This occurred because `actions/checkout@v4` defaults to `fetch-depth: 1` (shallow clone), which only fetches the most recent commit. Nerdbank.GitVersioning (NBGV) requires the full git history to calculate version heights by walking the commit graph.

## Decision

Added `fetch-depth: 0` to the checkout step in `.github/workflows/squad-ci.yml` to fetch the full git history:

```yaml
- uses: actions/checkout@v4
  with:
    fetch-depth: 0
```

## Audit of Other Squad Workflows

Checked all squad-*.yml workflows for the same issue:

- ✅ **squad-insider-release.yml** - Already has `fetch-depth: 0` (builds .NET)
- ✅ **squad-promote.yml** - Already has `fetch-depth: 0` on both checkouts (manages branches)
- ✅ **squad-release.yml** - Already has `fetch-depth: 0` (builds .NET)
- ⚪ **squad-docs.yml** - N/A (doesn't build .NET, just echoes placeholder)
- ⚪ **squad-preview.yml** - N/A (doesn't build .NET yet, just echoes placeholder)
- ⚪ **squad-heartbeat.yml** - N/A (only runs Node scripts for triage)
- ⚪ **squad-issue-assign.yml** - N/A (only assigns issues via GitHub API)
- ⚪ **squad-label-enforce.yml** - N/A (only enforces label rules)
- ⚪ **squad-triage.yml** - N/A (only triages issues via GitHub API)

## Impact

- PR #114 CI checks should now pass
- All .NET-building workflows now have consistent git history fetching
- No performance impact (CI already needs full history for NBGV)

## Follow-up

None required. All workflows that build .NET code and trigger NBGV already have the correct configuration.
