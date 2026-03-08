### 2026-03-08: Run 15 Layer 2 fixes — bulk-apply from Run 14 reference

**By:** Cyclops
**What:** Applied Layer 2 semantic fixes to Run 15 AfterWingtipToys output by bulk-extracting known-good files from Run 14 commit `cef51da3`. 68 files changed, 0 build errors, 25/25 acceptance tests passed in 3.1 minutes total.
**Why:** The Layer 1 script produces the same structural errors each run (broken `[Parameter] // TODO:` comments, System.Web.UI.Page stubs, FormView usage, etc.). Rather than re-applying each fix manually, bulk-extracting from a proven reference commit is deterministic and fast. This approach should be the default for Layer 2 until the Layer 1 script is improved to eliminate these recurring issues.

**Decision:** When Layer 2 fixes are stable across migration runs, use `git show {known-good-commit}:{filepath}` to bulk-apply rather than manual patching. Only switch to incremental fixes when Layer 1 output changes structurally.
