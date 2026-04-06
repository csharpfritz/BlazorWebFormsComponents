# Decision: ClientScript Analyzer Enhancement — Parameterized Messages

**Date:** 2026-07-30
**Author:** Cyclops
**Status:** Implemented
**Scope:** BWFC022, BWFC023, BWFC024

## Context

Phase 1 of ClientScript Migration Strategy (PRD: `dev-docs/prd-clientscript-migration-support.md`) required enhancing existing analyzers with pattern-specific TODO guidance.

## Decisions

1. **BWFC022 uses parameterized `MessageFormat` with `{0}/{1}` args** rather than separate `DiagnosticDescriptor` instances per method. This keeps a single diagnostic ID for all Page.ClientScript patterns while providing method-specific messages. The parent `MemberAccessExpressionSyntax` is inspected to determine which ClientScript method is called.

2. **BWFC024 detects only `ScriptManager.XXX` static calls**, not instance methods on local variables (e.g., `sm.RegisterAsyncPostBackControl()`). Instance detection would require semantic analysis; static call detection covers the most common migration patterns. Can be extended in a future phase if needed.

3. **BWFC023 message expanded to 3-step guidance**: remove interface → replace RaisePostBackEvent with EventCallback<T> → use @onclick. Previously just said "Use EventCallback<T>".

## Impact

- 172 analyzer tests pass (up from 159)
- No breaking changes to existing diagnostics — same IDs, same locations
- Integration test `ExpectedIds` updated to include BWFC024
- `AnalyzerReleases.Unshipped.md` updated
