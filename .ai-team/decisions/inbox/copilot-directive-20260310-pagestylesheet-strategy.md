### 2026-03-10: PageStyleSheet unload strategy
**By:** Jeffrey T. Fritz (via Copilot)
**What:** CSS loaded by PageStyleSheet should NOT unload on component dispose. Instead, CSS should persist until another page loads that doesn't reference that same PageStyleSheet. This is a "last page wins" model that mirrors browser behavior with full page navigation.
**Why:** User request — the dispose-based approach fails in SSR where dispose fires immediately after render.
