### 2026-07-26: Global Tool Architecture — Sequential Pipeline with Explicit Ordering

**By:** Forge (Lead / Web Forms Reviewer)

**What:** Designed the C# global tool (`webforms-to-blazor`) to replace `bwfc-migrate.ps1`. Architecture documented in `dev-docs/global-tool-architecture.md`.

**Key decisions:**

1. **Sequential pipeline** over middleware or visitor pattern. Order matters — Ajax prefixes before asp: prefixes. Explicit numeric ordering (100, 200, 300…) with gaps for future insertion.

2. **No hybrid mode** — the C# tool will NOT shell out to the PowerShell script for unported transforms. Security goal (Jeff's primary motivation) requires fully compiled code. Accept partial coverage during porting, gate on 25 L1 test pass rate.

3. **Three CLI subcommands:** `migrate` (full project), `convert` (single file), `analyze` (pre-scan). Primary flow: `webforms-to-blazor migrate -i <source> -o <output>`.

4. **Cross-file DataBind correlation** modeled as a 3-phase transform: pre-scan → code-behind rewrite → markup injection. Only transform that spans both pipelines.

5. **AI integration deferred to structured hints** — without `--use-ai`, generates `TODO(skill-name)` comments. With `--use-ai` + `GITHUB_TOKEN`, processes items via Microsoft.Extensions.AI. Tool does not directly invoke Copilot skills.

6. **25 L1 test cases ported as xUnit `[Theory]` tests** — same input/expected file pairs. Gate criteria: C# tool must pass all 25 before PowerShell script is deprecated.

7. **NuGet package:** `Fritz.WebFormsToBlazor`, command `webforms-to-blazor`, NBGV versioning, package signing.

**Why this matters:** Jeff wants security (compiled > script), distribution (`dotnet tool install`), and skill chaining (CLI invocation). This architecture maps all 41 PowerShell functions to ~30 C# classes with explicit ordering, testability, and a clean migration path.
