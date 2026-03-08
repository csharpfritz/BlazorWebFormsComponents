# Migration Toolkit Genericization

**Date:** 2026-03-08
**Requested by:** Jeffrey T. Fritz
**Commit:** 1e9ac46 on `squad/audit-docs-perf`

## Context

The 23-finding WingtipToys audit identified hardcoded WingtipToys references
throughout migration-toolkit scripts, skills, and documentation. This session
addressed 18 of 23 findings (5 CRITICAL, 3 HIGH, 10 MEDIUM); the remaining
5 LOW cosmetic items were left as-is.

## Who Worked

| Agent   | Layer / Scope | Summary |
|---------|---------------|---------|
| Forge   | Layer 1 (`bwfc-migrate.ps1`) | Preserved SelectMethod, genericized ProductContext template, dynamic GetRouteUrl hint, broadened payment detection |
| Cyclops | Layer 2 (`bwfc-migrate-layer2.ps1`) | Find-EntityType cascading detection, Get-PluralName / Find-DbSetName helpers, generic data-page heuristics, broadened seed detection |
| Beast   | 8 skill/doc files | Replaced WingtipToys examples with generic placeholders across all migration-toolkit skills and documentation |

## Key Outcomes

- Migration toolkit is now store-agnostic — no WingtipToys assumptions remain in code paths
- Layer 1 and Layer 2 scripts use dynamic entity/context detection instead of hardcoded names
- All skill and documentation files use generic placeholders
- 18 / 23 audit findings resolved; 5 LOW cosmetic findings deferred
