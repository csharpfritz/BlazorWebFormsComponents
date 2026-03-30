### 2026-03-30T10-26-16Z: User directive
**By:** Jeffrey T. Fritz (via Copilot)
**What:** Option A for the global tool - port all L1 transforms from bwfc-migrate.ps1 into the C# dotnet global tool. This becomes the primary entry point, replacing the PowerShell script. Motivation: the tool can be chained into SKILL.md as a CLI tool, and eliminates a script-based injection vector.
**Why:** User request - captured for team memory. Security consideration: compiled C# tool is safer than PowerShell script as an entry point.