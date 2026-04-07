# Project Context

- **Owner:** Jeffrey T. Fritz
- **Project:** BlazorWebFormsComponents — Blazor components emulating ASP.NET Web Forms controls for migration
- **Stack:** C#, Blazor, .NET, ASP.NET Web Forms, bUnit, xUnit, MkDocs, Playwright
- **Created:** 2026-02-10

---

📌 **Team update (2026-07-30):** ClientScript Migration Support PRD delivered — 9-section product requirements document (dev-docs/prd-clientscript-migration-support.md, 38 KB) covering analyzer improvements (BWFC022/023/024), CLI transforms (startup scripts, includes), safe automation boundaries, TODO guidance, documentation (ClientScriptMigrationGuide.md), testing (8 test cases), and 3-phase roadmap (P1: analyzers + transforms + docs, P2: samples, P3: runtime helpers). Based on Forge CLI Gap Analysis 1.2 (HIGH impact gap). Establishes BWFC position: prefer IJSRuntime over ClientScript shim; emit clear TODO for postback patterns; DO NOT emulate __doPostBack. Ready for implementation planning. — decided by Beast

## Learnings

<!-- Append new learnings below. Each entry is something lasting about the project. -->
