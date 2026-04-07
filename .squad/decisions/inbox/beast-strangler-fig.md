# Decision: Strangler Fig Pattern Documentation

**Date:** 2026  
**Proposed by:** Beast (Technical Writer)  
**Status:** IMPLEMENTED  

## Summary

Created comprehensive **Strangler Fig Pattern** documentation as the overarching philosophical framework for BWFC migration strategy. The pattern explains why BWFC exists and how its three layers (Roslyn analyzers, CLI tool, runtime shims) work together to enable incremental, side-by-side Web Forms → Blazor migration with zero downtime.

## Context

Jeff Fritz requested that BWFC's migration documentation be reframed around the **Strangler Fig pattern** — the practice of incrementally replacing a legacy system while keeping it running in parallel. This is the core philosophy underlying BWFC's design:

- **Roslyn analyzers** guide migration by detecting Web Forms patterns
- **CLI tool** performs L1 mechanical transforms
- **Runtime shims** (ClientScriptShim, SessionShim, CacheShim, ServerShim) enable zero-rewrite compatibility

Current migration guides scattered this philosophy across many documents. Consolidation was needed.

## Decision

Implement Strangler Fig pattern documentation as:

1. **New standalone guide** (`docs/Migration/StranglerFigPattern.md`) covering:
   - What the pattern is (biological metaphor → software practice)
   - How BWFC enables it (4-step journey: Instrument → Strangle → Zero-Rewrite → Modernize)
   - Visual progression (Legacy → Mixed → Blazor Dominant → Modernized)
   - Why it works (zero downtime, parallel velocity, reversibility)
   - Real-world e-commerce example (6-week phased migration)
   - Comparison: Big Bang vs Strangler Fig vs Parallel Development

2. **Cross-link from existing docs:**
   - ClientScriptMigrationGuide.md: Add "Strangler Fig Pattern Context" section after recommended shim section
   - Strategies.md: Add "The Strangler Fig Pattern: Migration Philosophy" section near top
   - readme.md: Add "Migration Philosophy" section explaining the approach

3. **Update mkdocs.yml navigation:**
   - Place StranglerFigPattern.md second in Migration nav (right after "Getting Started")
   - Strategic positioning: readers learn philosophy before mechanics

## Key Technical Facts Documented

- **Roslyn analyzers are purely syntactic** — They match patterns like `IsClientScriptAccess()` in the syntax tree without requiring System.Web type resolution. This means they work in any .NET project.

- **ClientScriptShim uses queue-and-flush pattern** — Scripts are queued during component lifecycle, auto-flushed in OnAfterRenderAsync via IJSRuntime.InvokeVoidAsync("eval", script). This matches Web Forms deduplication behavior.

- **Shims are production-ready** — Zero-rewrite approach is permanent, not a transitional hack. Modernization (Phase 2 refactoring to native Blazor patterns) is optional and on the team's schedule.

- **L1 CLI handles mechanical transforms** — Directive changes (@Page → @page), markup conversion (<asp: → component names), code-behind pattern transforms (Page_Load → OnInitialized).

## Rationale

1. **Philosophy first** — Developers need to understand *why* they can migrate incrementally before diving into *how*. Strangler Fig pattern explains the "why."

2. **Reduces migration anxiety** — Framing as "both systems run in parallel" with reversible steps makes large migrations feel manageable and low-risk.

3. **Differentiates BWFC** — The zero-rewrite shim approach is what makes BWFC unique. This must be the first thing developers learn.

4. **Guides informed decisions** — Developers can identify their current phase (Legacy, Mixed, Dominant, Modernized) and know what steps come next.

5. **Consolidates scattered philosophy** — Previously, the pattern was implied across many docs. Now it's explicit and easy to reference.

## Cross-References

- **Roslyn Analyzers:** [Analyzers.md](../../docs/Migration/Analyzers.md) — Technical details on BWFC022, BWFC023, BWFC024
- **ClientScriptShim:** [ClientScriptMigrationGuide.md](../../docs/Migration/ClientScriptMigrationGuide.md) — Deep dive on JS pattern migration
- **Session State:** [Phase2-SessionShim.md](../../docs/Migration/Phase2-SessionShim.md) — Zero-rewrite session migration
- **Automated Migration:** [AutomatedMigration.md](../../docs/Migration/AutomatedMigration.md) — L1 CLI tool guide
- **Migration Strategies:** [Strategies.md](../../docs/Migration/Strategies.md) — High-level planning

## Files Created/Updated

- ✅ **Created:** `docs/Migration/StranglerFigPattern.md` (12.1K, comprehensive guide)
- ✅ **Updated:** `docs/Migration/ClientScriptMigrationGuide.md` (added Strangler Fig context section)
- ✅ **Updated:** `docs/Migration/Strategies.md` (added pattern philosophy section)
- ✅ **Updated:** `docs/Migration/readme.md` (added migration philosophy section)
- ✅ **Updated:** `mkdocs.yml` (added StranglerFigPattern to nav, strategic placement after Getting Started)
- ✅ **Updated:** `.squad/agents/beast/history.md` (appended documentation task details and learnings)

## Learnings

1. **Metaphors matter** — The Strangler Fig metaphor (tree gradually replacing another tree) immediately resonates with developers and makes the pattern memorable.

2. **Visual progression > prose** — The 4-phase diagram (Legacy → Mixed → Dominant → Modernized) conveys the journey better than narrative alone.

3. **Real-world examples > abstract patterns** — The e-commerce example (6-week phased migration of Product Search → Cart → Accounts) makes incremental migration feel achievable.

4. **Philosophy placement in nav signals importance** — Placing StranglerFigPattern right after "Getting Started" tells developers "learn this first, before mechanics."

5. **Zero-rewrite is the differentiator** — Must be stated clearly and repeatedly. It's what makes BWFC unique and why teams should choose it over "big bang" rewrites.

6. **Shims are production-ready** — Developers fear compatibility layers are "temporary hacks." Documentation must emphasize shims are permanent, well-tested, and leave room for optional Phase 2 modernization.

## Next Steps (if any)

- Monitor developer questions in issues/discussions to see if Strangler Fig framing resonates
- Use this doc as the intro reference for first-time migration planners
- Consider creating a decision tree helper ("What phase are you in? What's your next step?") based on the 4-phase model
