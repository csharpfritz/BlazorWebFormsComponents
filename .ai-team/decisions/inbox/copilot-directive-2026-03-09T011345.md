### 2026-03-09: User directive — URL Rewriting over @page directives
**By:** Jeff Fritz (via Copilot)
**What:** For backward-compatible `.aspx` URLs, use `RewriteOptions.AddRedirect` middleware in Program.cs — do NOT add duplicate `@page` directives to Razor files. This aligns with the team decision from Forge's research.
**Why:** User request — captured for team memory. The URL rewriting approach is cleaner (one place vs. every page), SEO-friendly (301 redirects), and follows the established team decision.
