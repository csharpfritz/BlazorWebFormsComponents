# ASPX Middleware — Milestone Plans

**Created:** 2026-03-21  
**Author:** Forge (Lead / Web Forms Reviewer)  
**Based on:** `dev-docs/aspx-middleware-migration-landscape.md`  
**Branch scope:** `experiment/aspx-middleware` (all work stays on experimental branch)

---

## Overview

This document defines the milestone roadmap for the ASPX middleware experiment. The landscape evaluation identified 2 critical parser bugs, 3 blocking missing features (master pages, code-behind, URL rewriting), and 3 medium-priority gaps. These milestones address them in priority order.

**Milestone summary:**

| Milestone | Name | Priority | Effort | Unblocks |
|-----------|------|----------|--------|----------|
| **A** | Parser Hardening | P0 | 2–3 weeks | All subsequent milestones |
| **B** | Master Page Composition | P0 | 3–4 weeks | All realistic app testing |
| **C** | URL Rewriting & Link Translation | P1 | 2–3 weeks | Round-trip navigation |
| **D** | Eval Expression Evaluation | P1 | 2–3 weeks | Read-only data pages |
| **E** | Code-Behind Diagnostics | P1 | 1–2 weeks | Developer UX clarity |
| **F** | User Control Resolution | P2 | 3–4 weeks | Modular / enterprise apps |

All milestones target the `experiment/aspx-middleware` branch. No changes are promoted to `dev` until the Go/No-Go criteria in the landscape document are met.

---

## Milestone A — Parser Hardening

**Branch:** `experiment/aspx-middleware`  
**Estimated effort:** 2–3 weeks  
**Priority:** P0 — must complete before any other milestone  
**Depends on:** Nothing (this is the foundation)

### Goal

Fix the two known parser bugs so that the `AspxParser` can process any syntactically valid ASPX file without crashing or silently producing wrong output. Add regression tests that permanently guard against regressions.

### Background

The landscape evaluation (§4.1, §4.2) identified two bugs in `AspxParser.cs`:
1. Whitespace-only text nodes are silently dropped, breaking layout and spacing
2. The `SelfClosingAspTagRegex` prematurely terminates on `>` inside attribute values, causing malformed parse trees for any page with data-binding expressions or HTML comment-wrapped expression placeholders

These are foundation bugs — every subsequent feature (master pages, expressions, user controls) builds on top of the parser. Fixing them first prevents incorrect behavior from masking issues in later milestones.

### Deliverables

| ID | Deliverable | Description | Priority | Size |
|----|-------------|-------------|----------|------|
| A-01 | **Fix whitespace text node suppression** | In `AspxParser.cs`, remove or narrow the `IsNullOrWhiteSpace` check (around line 177) so that whitespace-only nodes between control tags are preserved as `AspxTextNode` instances. Preserve `\n`, `\r\n`, single spaces, and runs of spaces. Only discard nodes that are completely empty (`""`). | P0 | S |
| A-02 | **Fix self-closing tag regex for `>` in attribute values** | Replace the `SelfClosingAspTagRegex` (around line 326) with a stateful character-by-character attribute parser that tracks quote depth. When inside a quoted attribute value, `>` characters must not be treated as tag boundaries. This also covers HTML comment-wrapped expression placeholders (e.g., `<!-- <%# SomeValue %> -->`), where the `>` inside the comment content is a known additional trigger for the same premature termination. | P0 | M |
| A-03 | **Add whitespace preservation regression tests** | Write unit tests that parse ASPX fragments where whitespace between controls is significant and verify that `AspxTextNode` instances with whitespace content are present in the resulting AST. Cover: single space between inline elements, newline between block elements, multiple spaces (should be preserved as-is). | P0 | S |
| A-04 | **Add attribute value `>` regression tests** | Write unit tests for ASPX tags whose attribute values contain `>` characters (comparison operators in `<%# ... %>` expressions, `> 0` patterns, HTML entities `&gt;`, and HTML comment-wrapped expressions). Verify parse tree is correct and not truncated in each case. | P0 | S |
| A-05 | **Real-file parse smoke test** | Write an integration test that attempts to parse every `.aspx` file from the `samples/BeforeWebForms` project without throwing an exception. Failures are captured and reported (not asserted) so the test suite continues. This provides a baseline for parser stability. | P1 | M |
| A-06 | **Parser benchmark baseline** | Record parse time and allocation for the 10 largest `.aspx` files in `BeforeWebForms`. Document results in `dev-docs/benchmarks/`. This baseline is the reference point for detecting performance regressions in subsequent milestones. | P2 | S |

### Acceptance Criteria

1. All ~92 existing tests still pass
2. A-03 and A-04 regression tests pass
3. A-05 smoke test runs without uncaught exceptions (parse errors are acceptable as diagnostic output, not test failures)
4. No new `[Skip]` or `[Fact(Skip=...)]` annotations added to existing tests

### File Targets

- `src/BlazorWebFormsComponents.AspxMiddleware/AspxParser.cs` — bug fixes
- `src/BlazorWebFormsComponents.AspxMiddleware.Test/AspxParserWhitespaceTests.cs` — new test file (A-03)
- `src/BlazorWebFormsComponents.AspxMiddleware.Test/AspxParserAttributeValueTests.cs` — new test file (A-04)
- `src/BlazorWebFormsComponents.AspxMiddleware.Test/AspxParserSmokeTests.cs` — new test file (A-05)
- `dev-docs/benchmarks/aspx-parser-baseline.md` — new benchmark document (A-06)

---

## Milestone B — Master Page Composition

**Branch:** `experiment/aspx-middleware`  
**Estimated effort:** 3–4 weeks  
**Priority:** P0  
**Depends on:** Milestone A complete

### Goal

Implement master page (`.master` file) support so that ASPX pages using `@Page MasterPageFile="..."` render with the complete layout shell (header, navigation, footer, scripts) rather than as isolated content fragments.

### Background

The landscape evaluation (§4.3) identified missing master page support as the single highest-severity blocker. In any production Web Forms application, virtually every page uses a master page for shared layout. Without this, the middleware produces incomplete fragments that are not useful for testing or demonstration against real applications.

The `@Page MasterPageFile="~/Site.master"` directive is already parsed — the `MasterPageFile` attribute is captured in the `AspxDirectiveNode`. The tree builder currently ignores it. This milestone extends the tree builder to:
1. Locate the `.master` file relative to the web root
2. Parse the master page markup (which may itself have a master page)
3. Identify `<asp:ContentPlaceHolder ID="...">` regions in the master
4. Identify `<asp:Content ContentPlaceHolderID="...">` regions in the page
5. Compose them into a unified render tree where page content is injected into the master layout at the correct insertion points

### Deliverables

| ID | Deliverable | Description | Priority | Size |
|----|-------------|-------------|----------|------|
| B-01 | **`AspxMasterPageResolver`** | New class responsible for: (a) resolving `~/Site.master` paths relative to the configured web root, (b) reading and parsing the `.master` file using `AspxParser`, (c) building a `MasterPageDescriptor` (list of `ContentPlaceHolder` IDs and their default content). | P0 | M |
| B-02 | **Content composition in `AspxComponentTreeBuilder`** | Extend `AspxComponentTreeBuilder` to detect `@Page MasterPageFile=` and invoke `AspxMasterPageResolver`. Build the final `RenderFragment` as: master page HTML with `<asp:ContentPlaceHolder>` regions replaced by the corresponding `<asp:Content>` blocks from the page. Fallback: use the `ContentPlaceHolder`'s inner default content if the page has no matching `Content` block. | P0 | L |
| B-03 | **Nested master page support** | Handle the case where `Site.master` itself references a parent master page (`@Master MasterPageFile="~/Root.master"`). The resolver must detect the `@Master` directive and apply the same composition recursively. Limit recursion depth to 5 levels (beyond that, emit a diagnostic and stop). | P1 | M |
| B-04 | **Master page caching** | Cache parsed `MasterPageDescriptor` objects in a `ConcurrentDictionary<string, MasterPageDescriptor>` keyed by file path. Invalidate on `IFileProvider` change notifications. Do not re-parse the master file on every request. | P1 | S |
| B-05 | **Integration tests with `Site.Master`** | Write integration tests using the `BeforeWebForms` sample `Site.Master` (or a representative copy in the test project). Verify: (a) navigation markup from master appears in output, (b) `ContentPlaceHolder` regions are populated with page-specific content, (c) pages with no matching content block use the default placeholder content. | P0 | M |
| B-06 | **Fragment fallback test** | Verify that pages without a `MasterPageFile` directive continue to render as they do today (content fragment only, no regression). | P0 | S |
| B-07 | **Developer documentation** | Add a section to the middleware's `README.md` explaining master page support: what is supported, what is not, known limitations (e.g., code-behind in master page is not executed). | P2 | S |

### Acceptance Criteria

1. All Milestone A tests still pass
2. B-05 integration tests pass with `BeforeWebForms` `Site.Master`
3. B-06 regression test passes (no breakage to existing page rendering)
4. Master page is parsed once per file, not per request (verify via test that file is only read once)
5. Circular master page references (A references B references A) do not cause infinite recursion — depth limit kicks in and logs a warning

### File Targets

- `src/BlazorWebFormsComponents.AspxMiddleware/AspxMasterPageResolver.cs` — new class (B-01)
- `src/BlazorWebFormsComponents.AspxMiddleware/AspxComponentTreeBuilder.cs` — extend for composition (B-02, B-03)
- `src/BlazorWebFormsComponents.AspxMiddleware/AspxRenderingOptions.cs` — add `MasterPageCacheEnabled` option
- `src/BlazorWebFormsComponents.AspxMiddleware.Test/AspxMasterPageTests.cs` — new test file (B-05, B-06)
- `src/BlazorWebFormsComponents.AspxMiddleware/README.md` — developer documentation (B-07)

---

## Milestone C — URL Rewriting and Link Translation

**Branch:** `experiment/aspx-middleware`  
**Estimated effort:** 2–3 weeks  
**Priority:** P1  
**Depends on:** Milestone A complete (Milestone B recommended but not required)

### Goal

Ensure that all links and form actions in rendered pages stay within the middleware's zone of control. Rewrite `.aspx` URLs to route-based equivalents and strip Web Forms-specific hidden fields that have no meaning in the middleware context.

### Background

The landscape evaluation (§5, URL rewriting row) identified this as a round-trip usability requirement. Without it, a user can view a page through the middleware but the moment they click a link, they leave the middleware. The linked page's `.aspx` URL may 404 (if the physical file doesn't exist on the Blazor host) or loop back through the middleware as a new cold request.

Additionally, the rendered HTML currently contains Web Forms artifacts (`__VIEWSTATE`, `__EVENTVALIDATION` hidden fields, `href="javascript:__doPostBack(...)"` links) that have no meaning in the middleware context and may confuse the browser.

### Deliverables

| ID | Deliverable | Description | Priority | Size |
|----|-------------|-------------|----------|------|
| C-01 | **Post-render HTML rewriter** | Implement `AspxHtmlRewriter` — a post-render pass over the middleware's HTML output. Uses `HtmlAgilityPack` (or a lightweight string-based approach) to: (a) rewrite all `href="*.aspx"` attributes to equivalent middleware-routed URLs, (b) rewrite all `action="*.aspx"` form targets. The rewrite must handle both relative and absolute URLs, and must not modify `href` attributes on non-`.aspx` URLs. | P0 | M |
| C-02 | **ViewState / EventValidation field removal** | In the post-render pass, remove `<input type="hidden" name="__VIEWSTATE" ...>`, `<input type="hidden" name="__EVENTVALIDATION" ...>`, `<input type="hidden" name="__EVENTTARGET" ...>`, and `<input type="hidden" name="__EVENTARGUMENT" ...>` elements. These fields have no meaning in the middleware context and may cause confusion if submitted in forms. | P0 | S |
| C-03 | **`__doPostBack` JavaScript handling** | Detect and handle `href="javascript:__doPostBack('controlId', 'arg')"` links. In the short term, replace with `href="#"` and log a diagnostic indicating that postback-based navigation is not supported. Long term (post-M-C), translate to Blazor event callbacks. | P1 | S |
| C-04 | **`WebResource.axd` URL removal** | Remove or replace `<script src="WebResource.axd?...">` and `<link href="WebResource.axd?...">` references in rendered output. These are Web Forms infrastructure URLs that do not exist on a Blazor host. Replace with `<!-- WebResource.axd removed by ASPX middleware -->` comment. | P2 | S |
| C-05 | **URL rewriting configuration** | Add `AspxUrlRewritingOptions` to `AspxRenderingOptions`: `bool EnableHrefRewriting` (default `true`), `bool StripViewState` (default `true`), `bool StripDoPostBack` (default `true`), `string AspxRoutePrefix` (default `""`). Allow consumers to disable individual rewriting behaviors. | P2 | S |
| C-06 | **Integration tests** | Write integration tests that render a page, verify all `href` attributes pointing to `.aspx` URLs have been rewritten, verify `__VIEWSTATE` fields are absent, verify `__doPostBack` links have been neutralized. | P0 | M |

### Acceptance Criteria

1. All prior milestone tests still pass
2. Rendered pages contain no `href="*.aspx"` attributes (verified by test)
3. Rendered pages contain no `__VIEWSTATE` / `__EVENTVALIDATION` hidden fields (verified by test)
4. `WebResource.axd` references are removed or replaced
5. URL rewriting can be disabled via configuration (verified by test with rewriting disabled)

### File Targets

- `src/BlazorWebFormsComponents.AspxMiddleware/AspxHtmlRewriter.cs` — new class (C-01 through C-04)
- `src/BlazorWebFormsComponents.AspxMiddleware/AspxUrlRewritingOptions.cs` — new options class (C-05)
- `src/BlazorWebFormsComponents.AspxMiddleware/AspxRenderingOptions.cs` — add `UrlRewriting` property
- `src/BlazorWebFormsComponents.AspxMiddleware/AspxRenderingMiddleware.cs` — wire rewriter into pipeline
- `src/BlazorWebFormsComponents.AspxMiddleware.Test/AspxUrlRewritingTests.cs` — new test file (C-06)

---

## Milestone D — Eval Expression Evaluation

**Branch:** `experiment/aspx-middleware`  
**Estimated effort:** 2–3 weeks  
**Priority:** P1  
**Depends on:** Milestone A complete

### Goal

Implement `<%# Eval("Field") %>` and `<%# Bind("Field") %>` expression evaluation so that data-bound ASPX pages render with actual data values rather than literal expression text.

### Background

The landscape evaluation (§4.6) identified Eval expression evaluation as the key unlock for read-only data-bound pages. This is distinct from the Blazor `@bind` mechanism — these are Web Forms data-binding expressions that are evaluated server-side against a `DataItem` context object during the control's `DataBind()` lifecycle.

In the middleware pipeline, the evaluation must be threaded through the tree builder: when inside a data-bound control's item template, a `DataItem` context is available. The tree builder must capture this context and evaluate `AspxExpressionNode` instances against it using `System.Web.UI.DataBinder.Eval()` (or a compatible implementation).

### Deliverables

| ID | Deliverable | Description | Priority | Size |
|----|-------------|-------------|----------|------|
| D-01 | **`AspxDataContext`** | New class representing the data-binding context at a point in the render tree. Contains: `object DataItem`, `int ItemIndex`, `ListItemType ItemType`. Passed down through the tree builder when rendering inside data-bound templates (ItemTemplate, AlternatingItemTemplate, etc.). | P0 | S |
| D-02 | **`AspxExpressionEvaluator`** | New class that evaluates a raw expression string from `AspxExpressionNode.Expression` against an `AspxDataContext`. Supports: `Eval("Field")`, `Eval("Field", "format")`, `Bind("Field")` (read path), `Container.ItemIndex`, `Container.DataItem`. Uses reflection-based property access compatible with `DataBinder.Eval()`. | P0 | M |
| D-03 | **Thread `AspxDataContext` through `AspxComponentTreeBuilder`** | Extend `AspxComponentTreeBuilder` to accept and propagate an `AspxDataContext` parameter. When building ItemTemplate content, construct the context from the data item and pass it to child node builders. Null context outside of data-bound template regions. | P0 | M |
| D-04 | **`AspxExpressionNode` rendering** | In the tree builder, when an `AspxExpressionNode` is encountered: (a) if `AspxDataContext` is non-null, evaluate the expression and render the result as a text node, (b) if context is null (not inside a data-bound template), render an empty string and log a diagnostic. | P0 | S |
| D-05 | **Format string support** | `Eval("Price", "{0:C}")` must apply the format string using `string.Format()`. `Eval("Date", "MM/dd/yyyy")` with a date format string. | P1 | S |
| D-06 | **Unit tests for `AspxExpressionEvaluator`** | Test against plain objects, anonymous types, `DataRow`/`DataTable`, and format strings. | P0 | M |
| D-07 | **Integration tests with `Repeater` and `GridView`** | Write integration tests that set up a `<asp:Repeater>` with an `ItemTemplate` containing `<%# Eval("Name") %>` expressions, bind a `List<T>` data source, and verify rendered HTML contains the actual field values. | P0 | M |

### Acceptance Criteria

1. All prior milestone tests still pass
2. `<%# Eval("Field") %>` inside a `Repeater` ItemTemplate renders the actual field value
3. `<%# Eval("Price", "{0:C}") %>` applies the format string
4. `<%# Bind("Field") %>` renders the field value (write path not required)
5. `<%# Eval("Field") %>` outside any data-bound context renders as empty string (not as literal expression text)
6. All D-06 unit tests pass
7. All D-07 integration tests pass

### File Targets

- `src/BlazorWebFormsComponents.AspxMiddleware/AspxDataContext.cs` — new class (D-01)
- `src/BlazorWebFormsComponents.AspxMiddleware/AspxExpressionEvaluator.cs` — new class (D-02)
- `src/BlazorWebFormsComponents.AspxMiddleware/AspxComponentTreeBuilder.cs` — thread context (D-03, D-04)
- `src/BlazorWebFormsComponents.AspxMiddleware.Test/AspxExpressionEvaluatorTests.cs` — new test file (D-06)
- `src/BlazorWebFormsComponents.AspxMiddleware.Test/AspxDataBindingIntegrationTests.cs` — new test file (D-07)

---

## Milestone E — Code-Behind Diagnostics

**Branch:** `experiment/aspx-middleware`  
**Estimated effort:** 1–2 weeks  
**Priority:** P1  
**Depends on:** Milestone A complete

### Goal

Replace silent code-behind failures with clear, actionable diagnostics so developers understand exactly what is and is not supported by the middleware. This milestone does not implement code-behind execution — it makes the absence of code-behind execution transparent and guides developers toward the correct migration path.

### Background

The landscape evaluation (§4.4) identified code-behind as a critical missing feature. The `@Page CodeFile="Dashboard.aspx.cs"` directive is parsed and ignored, leading to pages that render with no data and no event handlers — behavior that is indistinguishable from a page that has no code-behind (but is actually wrong).

This milestone adds an explicit diagnostic layer that detects when code-behind is present and could not be executed, and communicates this to the developer clearly. It also provides guidance on how to convert code-behind logic to a Blazor model.

### Deliverables

| ID | Deliverable | Description | Priority | Size |
|----|-------------|-------------|----------|------|
| E-01 | **Code-behind detection** | In `AspxComponentTreeBuilder`, detect when the `@Page` directive has a `CodeFile` or `CodeBehind` attribute that references a `.cs` file. Log a structured warning using `ILogger<AspxComponentTreeBuilder>` that includes the page path, the code-behind file path, and the reason execution was skipped. | P0 | S |
| E-02 | **Developer-mode warning overlay** | In development environments (`IHostEnvironment.IsDevelopment()`), append a styled `<div>` to the rendered page output indicating that code-behind was not executed. Include: the code-behind file path, a list of lifecycle events that would have run (`Page_Load`, `Page_Init`, `Page_PreRender`), and a link to the migration guide. The overlay must be visually distinct (banner-style, dismissable) and must not appear in production. | P1 | M |
| E-03 | **Structured diagnostic event** | Publish a `CodeBehindSkippedDiagnostic` event through an `IMiddlewareDiagnosticsPublisher` interface. This decouples the diagnostic from the rendering path and allows consumers to subscribe to diagnostics (e.g., for test assertions, telemetry). | P2 | M |
| E-04 | **`Page_Load` stub detection** | Scan the code-behind `.cs` file (using simple `StreamReader` line scanning, not Roslyn) to detect method signatures matching `void Page_Load(...)`, `void Page_Init(...)`, etc. Include the detected methods in the diagnostic message so the developer knows exactly what was skipped. | P1 | S |
| E-05 | **Unit tests** | Test that: (a) pages with `CodeFile` directive emit the structured log warning, (b) developer-mode overlay appears in development environment, (c) overlay does not appear in production environment, (d) pages without code-behind produce no diagnostic. | P0 | S |
| E-06 | **Migration guide section** | Add a "Code-Behind Migration" section to the middleware's `README.md`. Explain: (1) why code-behind is not executed, (2) how to convert a simple `Page_Load` data-binding pattern to a Blazor `OnInitializedAsync` pattern, (3) how to use `@inject` to replace `Page`-level service access. | P1 | S |

### Acceptance Criteria

1. All prior milestone tests still pass
2. Pages with `CodeFile`/`CodeBehind` directives produce a structured log warning
3. Developer-mode overlay appears in development, not in production
4. Pages without code-behind produce no diagnostic (no regression)
5. E-05 tests all pass

### File Targets

- `src/BlazorWebFormsComponents.AspxMiddleware/AspxComponentTreeBuilder.cs` — add diagnostic emission (E-01)
- `src/BlazorWebFormsComponents.AspxMiddleware/IMiddlewareDiagnosticsPublisher.cs` — new interface (E-03)
- `src/BlazorWebFormsComponents.AspxMiddleware/AspxRenderingMiddleware.cs` — overlay injection (E-02)
- `src/BlazorWebFormsComponents.AspxMiddleware.Test/AspxCodeBehindDiagnosticsTests.cs` — new test file (E-05)
- `src/BlazorWebFormsComponents.AspxMiddleware/README.md` — migration guide section (E-06)

---

## Milestone F — User Control Resolution

**Branch:** `experiment/aspx-middleware`  
**Estimated effort:** 3–4 weeks  
**Priority:** P2  
**Depends on:** Milestones A and B complete

### Goal

Implement user control (`.ascx` file) resolution so that `<uc:Breadcrumb>` and similar custom tag references in ASPX pages are resolved, parsed, and inlined into the render tree.

### Background

The landscape evaluation (§4.7) identified user control resolution as a P2 requirement needed for modular enterprise applications. User controls (`<%@ Register %>` + `<uc:*>` usage) are the Web Forms equivalent of Blazor components — they encapsulate reusable UI. Without resolution, any page that uses user controls will have missing content.

The parsing infrastructure already captures `Register` directives. This milestone extends the tree builder to resolve the registered controls and recursively inline their markup.

### Deliverables

| ID | Deliverable | Description | Priority | Size |
|----|-------------|-------------|----------|------|
| F-01 | **`AspxUserControlResolver`** | New class responsible for: (a) building a map from `TagPrefix:TagName` to `.ascx` file path, (b) locating `.ascx` files relative to the web root using `~/` path resolution, (c) parsing `.ascx` files using `AspxParser`, (d) caching parsed user control descriptors (same caching strategy as master pages in B-04). | P0 | M |
| F-02 | **User control attribute mapping** | When a `<uc:Breadcrumb Title="Home">` tag is resolved, map its attributes to the user control's parameters. If the user control has a code-behind with matching public properties, emit a diagnostic (E-01 pattern) and use the attribute values as default parameter values without executing code-behind logic. | P1 | M |
| F-03 | **Extend `AspxComponentTreeBuilder` for user control tags** | When the tree builder encounters a tag whose prefix matches a registered user control's `TagPrefix` and `TagName`, invoke the resolver, get the parsed user control, and inline its render fragment in place of the user control tag. Pass attribute values as the render context. | P0 | L |
| F-04 | **Nested user control support** | User controls that themselves reference other user controls (one additional level of nesting) must be resolved recursively. Limit recursion depth to 10 levels. If depth is exceeded, emit a diagnostic and render a placeholder comment. | P1 | M |
| F-05 | **User control caching** | Cache parsed user control descriptors (same approach as B-04 master page caching). Invalidate on file change. | P1 | S |
| F-06 | **Integration tests** | Write integration tests using a sample page with 2–3 registered user controls. Verify: (a) user control markup is inlined at the correct position, (b) attributes passed to user control are reflected in its rendered output, (c) nested user controls render correctly. | P0 | L |
| F-07 | **Error handling for missing user control files** | If the `.ascx` file referenced by a `Register` directive does not exist, emit a clear error diagnostic and render a placeholder `<!-- Missing user control: ~/Controls/Breadcrumb.ascx -->` comment in place of the tag. Do not throw an exception that would take down the entire page render. | P0 | S |

### Acceptance Criteria

1. All prior milestone tests still pass
2. Pages using registered user controls render with the user control's markup inlined
3. Attributes on `<uc:*>` tags are passed through to the user control (verified by test)
4. Missing user control files produce a placeholder comment, not an exception
5. User controls are parsed once per file, not per request
6. All F-06 integration tests pass

### File Targets

- `src/BlazorWebFormsComponents.AspxMiddleware/AspxUserControlResolver.cs` — new class (F-01)
- `src/BlazorWebFormsComponents.AspxMiddleware/AspxComponentTreeBuilder.cs` — extend for user controls (F-02, F-03, F-04)
- `src/BlazorWebFormsComponents.AspxMiddleware/AspxRenderingOptions.cs` — add `UserControlCacheEnabled` option (F-05)
- `src/BlazorWebFormsComponents.AspxMiddleware.Test/AspxUserControlTests.cs` — new test file (F-06, F-07)

---

## Cross-Cutting Concerns

### Security

**`<script runat="server">` must never be rendered as HTML.**

Server-side script blocks contain C# source code. If rendered as raw HTML, they expose application logic (including connection strings, business rules, and SQL queries) to the browser. The parser must detect `<script runat="server">` (case-insensitive on the attribute) and:
1. Emit a structured log warning
2. Emit a developer-mode diagnostic (if applicable, per E-02)
3. Render `<!-- Server-side script block removed by ASPX middleware -->` as a comment
4. Never pass the content through to the HTML output

This behavior must be enforced in `AspxParser.cs` and verified by a dedicated security regression test that asserts the script block content does not appear in rendered output.

### Caching Strategy

Both `AspxMasterPageResolver` (B-04) and `AspxUserControlResolver` (F-05) use the same caching pattern:
- `ConcurrentDictionary<string, WeakReference<T>>` keyed by absolute file path
- `PhysicalFileProvider`-based `IChangeToken` for invalidation
- `IMemoryCache` optional layer for production deployments with high request volume

This should be factored into a shared `AspxFileCache<T>` helper to avoid duplication.

### Observability

All middleware components should use structured logging via `ILogger<T>`. Log levels:
- `Debug` — per-request lifecycle events (parse start, tree build complete)
- `Information` — feature activations (master page resolved, user control inlined)
- `Warning` — unsupported features detected (code-behind skipped, unknown control tag)
- `Error` — recoverable failures (user control file missing, parse error on individual tag)
- `Critical` — unrecoverable failures (middleware pipeline crash)

---

*For the full landscape evaluation that motivated these milestones, see `dev-docs/aspx-middleware-migration-landscape.md`.*
