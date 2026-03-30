# Psylocke — History

## 2025-07-25: Updated migration-toolkit skills for Phase 1 capabilities

**Task:** Update SKILL.md files so end-users get guidance on new Phase 1 "Just Make It Compile" shims and script capabilities.

**Files modified:**
- `migration-toolkit/skills/bwfc-migration/SKILL.md` — Added "Phase 1 Compile-Compatibility Shims" section (ConfigurationManager shim, BundleConfig/RouteConfig stubs, IsPostBack guard unwrapping, .aspx URL cleanup). Updated Layer 1 capability list, Installation section (added `UseConfigurationManagerShim()`), Common Gotchas (IsPostBack), and Per-Page Migration Checklist.
- `migration-toolkit/skills/bwfc-migration/CODE-TRANSFORMS.md` — Expanded Lifecycle Methods section with L1 auto-unwrap details and before/after examples. Added "IsPostBack Guard Handling (L1 Automated)" subsection. Added ".aspx URL Cleanup (L1 Automated)" subsection after Navigation.
- `migration-toolkit/skills/migration-standards/SKILL.md` — Added "Compile-Compatibility Shims" section (table of all shims, ConfigurationManager setup, appsettings.json mapping). Updated Layer 1 script capability list with IsPostBack unwrapping, .aspx URL cleanup, and using retention. Updated Page Lifecycle Mapping table for IsPostBack.

**Approach:** Read each implementation (ConfigurationManager.cs, BundleConfig.cs, RouteConfig.cs, Remove-IsPostBackGuards function, GAP-20 .aspx URL cleanup) to document actual APIs accurately. Matched existing formatting style in each file.

## 2026-03-30: Phase 4 Migration Skills  L1 Frozen, Strategic Restructuring

**Task:** Address remaining ~30% of migration work that L1 script cannot automate. Formalize layer 2 strategy via new Phase 4 skills.

**Analysis:** After reviewing Forge's migration-shim-analysis.md (47-item gap analysis), determined that L1 PowerShell has reached its deterministic limit at ~70% coverage. Remaining work requires:
1. Architecture decisions (Application["key"]  IMemoryCache or IDistributedCache?)
2. Cross-file reasoning (FindControl  understanding component tree structures)
3. Domain knowledge (Membership password hashes, Global.asax lifecycle, HttpModule pipeline order)

**Strategic Decision:** L1 PowerShell (wfc-migrate.ps1) is **FROZEN at Phase 3**.

**Three-Layer Migration Model (formalized):**
1. **L1 (PowerShell)**  Deterministic, automated transforms. ~70% coverage. **FROZEN.**
2. **L2 (Copilot Skills)**  AI-guided, contextual transforms. ~25% coverage. **Phase 4 active.**
3. **L3 (Developer)**  Architectural decisions. ~5% coverage. **Always requires human review.**

**Phase 4 Skills Created:**

| Skill | Gap Items | Key Decisions |
|-------|----------|---------------|
| wfc-session-state | Application[], Cache[], HttpContext.Current | Singleton vs scoped, IMemoryCache vs IDistributedCache |
| wfc-middleware-migration | HttpModule, Global.asax | Event  middleware position, pipeline order |
| wfc-usercontrol-migration | .ascx, FindControl, [Parameter] | Public property vs internal state, @ref timing |
| wfc-identity-migration (v2) | FormsAuth, Membership, Roles | Auth system detection, password hash compatibility |

**Documentation Updated:**
- migration-toolkit/skills/bwfc-migration/CODE-TRANSFORMS.md  Added Phase 4 section
- wfc-identity-migration/SKILL.md  Enhanced with FormsAuth, Membership, Roles content

**Rationale for L1 Freeze:**
- Scripts excel at regex/AST transforms but fail on context-dependent logic
- Skills can ask decision questions and provide before/after examples
- Skills iterate faster (markdown vs PowerShell)
- Prevents L1 from becoming unmaintainable catch-all for edge cases
- Codifies "What Developers Must Do Manually" explicitly

**Outcome:**
- Migration strategy clarified for next 12 months
- Team alignment on scope boundaries
- Developers get clear path: Run L1  Use L2 skills  Apply L3 judgment
- No more feature creep in L1 script (frozen)
