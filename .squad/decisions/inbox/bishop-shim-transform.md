### ClientScriptTransform: Switched Default to Shim-Preserving Path

**By:** Bishop (Migration Tooling Dev)

**Date:** 2026-07-31

**Status:** IMPLEMENTED

**Requested by:** Jeffrey T. Fritz

**Affects:** Forge (CLI output changes), Cyclops (analyzer guidance may reference IJSRuntime → update to reference ClientScriptShim), Beast (docs may reference old transform behavior)

**Summary:** `ClientScriptTransform.cs` in the CLI pipeline no longer rewrites `Page.ClientScript` calls to `IJSRuntime` skeletons. Instead, it strips `Page.`/`this.` prefixes and preserves the original API calls, which are now handled at runtime by `ClientScriptShim`.

**What changed:**
1. `RegisterStartupScript`, `RegisterClientScriptInclude`, `RegisterClientScriptBlock` — prefix stripped, calls preserved
2. `ScriptManager.RegisterStartupScript(control, ...)` → `ClientScript.RegisterStartupScript(...)` (first param dropped)
3. `GetPostBackEventReference` — still TODO (shim throws NotSupportedException)
4. `ScriptManager.GetCurrent` — still TODO (no shim equivalent)
5. IJSRuntime `[Inject]` injection removed; replaced with ClientScriptShim dependency comment

**Why:** Jeff's directive: "Zero-rewrite shim approach is PRECISELY what we should be building." The ClientScriptShim makes the original Web Forms API calls work as-is in Blazor, so the CLI should preserve them rather than rewriting to a different API.

**Impact on other agents:**
- **Forge/Bishop:** CLI `migrate` command output for any code-behind with ClientScript calls will now contain preserved calls instead of IJSRuntime skeletons
- **Cyclops:** Analyzers BWFC022/023/024 guidance text may reference IJSRuntime migration — consider updating to mention ClientScriptShim as the primary path
- **Beast:** ClientScriptMigrationGuide.md may need a section on the shim-first approach
- **All agents:** When generating migrated code-behind, use `ClientScript.XXX(...)` calls (not `IJSRuntime`) for patterns the shim supports
