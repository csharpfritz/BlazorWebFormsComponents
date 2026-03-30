# Decision: Phase 4 Migration Skills — L1 PowerShell Frozen

**Date:** 2026-03-30  
**Decided by:** Psylocke (Skills Engineer), per charter from Jeffrey T. Fritz  
**Status:** ✅ Implemented

---

## Context

After analyzing Forge's 47-item gap analysis (migration-shim-analysis.md) and reviewing the Phase 1-3 L1 PowerShell script capabilities, we reached a strategic decision point: how to address the remaining ~30% of migration work that L1 cannot automate.

The L1 script (`bwfc-migrate.ps1`) successfully automates:
- File renaming (.aspx → .razor)
- Directive conversion (<%@ Page %> → @page)
- Prefix stripping (asp:, uc:, ajaxToolkit:)
- Expression conversion (<%# %>, <%= %>, <%: %> → Razor)
- Response.Redirect → NavigationManager.NavigateTo
- Master page → Layout conversion
- Event handler wiring (Phase 3)
- DataSource/DataBind detection and field creation (Phase 3)

But the remaining gaps require:
1. **Architecture decisions** — Is `Application["key"]` global or per-user? IMemoryCache or IDistributedCache?
2. **Cross-file reasoning** — Converting `FindControl("id")` requires understanding component trees
3. **Domain knowledge** — Membership password hashes, Global.asax event flow, HttpModule pipeline order

---

## Decision

**L1 PowerShell script is FROZEN at Phase 3.**

All remaining migration work becomes **Layer 2 (L2) Copilot skills** — AI-guided transforms that provide decision trees, before/after examples, and explicit "What Developers Must Do Manually" sections.

---

## Rationale

1. **L1 has reached its deterministic limit** — Regex and AST manipulation handle ~70% of migration work. Further automation risks incorrect transforms that developers will spend hours debugging.

2. **Skills provide better guidance than scripts** — For context-dependent transforms:
   - Scripts can detect patterns but not understand intent
   - Skills can ask: "Is this data global or per-user?" and provide examples for each path
   - Skills document known failure modes and edge cases

3. **Skills iterate faster than scripts** — Skills are markdown files that Copilot reads dynamically. Updating a skill takes minutes. Adding features to the L1 script requires PowerShell expertise, testing, and version management.

4. **Explicit "cannot automate" sections** — Skills codify what humans must do (e.g., "Decide between singleton and scoped service based on data lifetime"). Scripts silently fail or produce broken code.

---

## Skills Created (Phase 4)

| Skill | Items Covered | Key Decisions |
|-------|--------------|---------------|
| `bwfc-session-state` | Application[], Cache[], HttpContext.Current (#13-16) | Singleton vs scoped, IMemoryCache vs IDistributedCache |
| `bwfc-middleware-migration` | HttpModule, Global.asax (#22-24) | Event → middleware position mapping, pipeline order |
| `bwfc-usercontrol-migration` | .ascx, FindControl, [Parameter] (#8, #30-31) | Public property vs internal state, @ref timing |
| `bwfc-identity-migration` (v2) | FormsAuth, Membership, Roles (#25-27) | Auth system detection, password hash compatibility |

---

## Documentation Updated

- `CODE-TRANSFORMS.md` — Added "Phase 4: Skills-Based Transforms" section
- `bwfc-identity-migration/SKILL.md` — Added FormsAuthentication, Membership, Roles sections
- `.ai-team/agents/psylocke/history.md` — Recorded Phase 4 learnings

---

## Three-Layer Migration Strategy

1. **Layer 1 (L1) — PowerShell Script:** Automated, deterministic transforms. **Frozen at Phase 3.**  
   Coverage: ~70% of migration work.

2. **Layer 2 (L2) — Copilot Skills:** AI-guided, contextual transforms. **Active development (Phase 4).**  
   Coverage: ~25% of migration work (requires human judgment).

3. **Layer 3 (L3) — Developer Decisions:** Project-specific architecture. **Always requires human review.**  
   Coverage: ~5% (e.g., choosing database provider, service layer design).

---

## Impact

- **Developers:** Run L1 once, then invoke skills for remaining work. Skills guide decisions, not automate them.
- **Skills engineers:** Focus on skill quality, not PowerShell complexity. Skills iterate faster.
- **Migration quality:** Explicit "What Developers Must Do Manually" prevents incorrect automated transforms.

---

## Next Steps

1. ✅ Create Phase 4 skills (bwfc-session-state, bwfc-middleware-migration, bwfc-usercontrol-migration)
2. ✅ Enhance bwfc-identity-migration with FormsAuth/Membership/Roles
3. ✅ Update CODE-TRANSFORMS.md with Phase 4 section
4. 🔲 Test skills on ContosoUniversity migration benchmark
5. 🔲 Iterate skills based on developer feedback

---

## References

- `dev-docs/migration-shim-analysis.md` — Forge's 47-item gap analysis (master roadmap)
- `migration-toolkit/skills/bwfc-migration/CODE-TRANSFORMS.md` — Phase 1-4 transform catalog
- `.ai-team/agents/psylocke/charter.md` — Psylocke's role as Skills Engineer
