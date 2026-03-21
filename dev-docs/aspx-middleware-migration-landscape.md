# ASPX Middleware Migration — Landscape Evaluation

**Created:** 2026-03-21  
**Author:** Forge (Lead / Web Forms Reviewer)  
**Branch:** `experiment/aspx-middleware`  
**Status:** EVALUATION — Basis for milestone planning

---

## 1. Executive Summary

The ASPX middleware experiment (`experiment/aspx-middleware`) proves that ASP.NET Web Forms `.aspx` pages can be served directly from a Blazor Server application without migrating the markup first. The middleware intercepts requests for `.aspx` URLs, parses the markup, maps `<asp:*>` controls to BlazorWebFormsComponents, and renders the result server-side via Blazor's `HtmlRenderer`.

**Current State: Proof of Concept — Not Production Ready**

| Metric | Value |
|--------|-------|
| Source files | 8 |
| Registered controls | 55 |
| Tests passing (current) | ~52 |
| Tests passing (projected after Milestone A) | ~92 |
| Known critical bugs | 2 (parser) |
| Missing feature areas | 6 (see §4) |
| Production readiness | ❌ Not ready |

The pipeline works end-to-end for simple pages with straightforward `<asp:*>` markup. It breaks on common real-world patterns. The goal of this document is to characterize what must be fixed before the middleware can be used on realistic Web Forms applications.

---

## 2. Architecture Overview

The middleware pipeline has four stages:

```
.aspx file on disk
      ↓
AspxParser          (regex + XDocument — produces AST nodes)
      ↓
AspxComponentTreeBuilder   (maps AST nodes → Blazor component parameters)
      ↓
HtmlRenderer (SSR)  (renders component tree via Blazor DI)
      ↓
HTTP response
```

### 2.1 Source Files

| File | Purpose |
|------|---------|
| `AspxParser.cs` | Tokenizes `.aspx` markup into an AST. Handles `<asp:*>` tags, directives, inline expressions, and code blocks. |
| `AspxComponentRegistry.cs` | Maps Web Forms control names to BWFC component types. Handles generic types with default type arguments. |
| `AspxComponentTreeBuilder.cs` | Walks the AST and constructs a `RenderFragment` for each node, recursively resolving child content. |
| `AspxRenderingMiddleware.cs` | ASP.NET Core middleware that intercepts `.aspx` requests and coordinates the pipeline. |
| `AspxMiddlewareExtensions.cs` | `IServiceCollection` and `IApplicationBuilder` extension methods for registration. |
| `AspxNodeTypes.cs` | AST node hierarchy: `AspxNode`, `AspxTextNode`, `AspxTagNode`, `AspxDirectiveNode`, `AspxExpressionNode`, `AspxCodeBlockNode`. |
| `AspxParserOptions.cs` | Parser configuration: base path, encoding, expression handling strategy. |
| `AspxRenderingOptions.cs` | Middleware configuration: URL patterns, fallback behavior. |

### 2.2 Control Registry

55 Web Forms controls are registered. 13 require generic type parameters (defaulting to `object` when the ASPX markup does not specify `ItemType`):

- `BulletedList<object>`, `CheckBoxList<object>`, `DropDownList<object>`, `ListBox<object>`
- `RadioButtonList<object>`, `DataGrid<object>`, `DataList<object>`, `DetailsView<object>`
- `FormView<object>`, `GridView<object>`, `ListView<object>`, `Repeater<object>`, `AdRotator<object>`

Unregistered controls (no BWFC equivalent): `ScriptManager`, `ScriptManagerProxy`, `SqlDataSource`, `ObjectDataSource`, `LinqDataSource`, `AccessDataSource`, `XmlDataSource`, `SiteMapDataSource`, `Wizard`, `WizardStep`, `MultiPage`, `TabPanel`.

### 2.3 Dependency Injection Requirements

BWFC components use constructor injection. The middleware test harness requires:

```csharp
services.AddBlazorWebFormsComponents();
services.AddRouting();           // for LinkGenerator
services.AddSingleton<IJSRuntime>(new NoOpJSRuntime());
```

In production, `IHttpContextAccessor` must also be registered.

---

## 3. What Works Today

The middleware successfully handles a subset of real-world ASPX patterns:

| Scenario | Status | Notes |
|----------|--------|-------|
| Static text content between controls | ✅ | Inline text rendered as `MarkupString` |
| `<asp:Label>` basic rendering | ✅ | Text, CssClass attributes |
| `<asp:Button>` basic rendering | ✅ | Text, CssClass, OnClick attribute mapping |
| `<asp:TextBox>` basic rendering | ✅ | Text, TextMode, MaxLength |
| Nested controls (`<asp:Panel>` wrapping others) | ✅ | Recursive child rendering via `ChildContent` |
| `@Page` directive parsing | ✅ | Title, Language, MasterPageFile parsed |
| `<%@ Register %>` directive parsing | ✅ | TagPrefix, TagName, Src/Namespace captured |
| HTML pass-through (`<div>`, `<table>`, etc.) | ✅ | Non-`<asp:*>` elements rendered as raw HTML |
| SSR via `HtmlRenderer` | ✅ | Blazor component tree rendered without a browser |
| End-to-end integration test | ✅ | `AspxRenderingIntegrationTests` covers 52 scenarios |

---

## 4. Critical Issues

These issues block use of the middleware on any non-trivial Web Forms application.

### 4.1 🔴 Parser Bug — Whitespace-Only Text Nodes Dropped

**File:** `AspxParser.cs` line 177  
**Severity:** High — affects layout and spacing fidelity

The parser has an `IsNullOrWhiteSpace` check that silently discards text nodes that contain only spaces, tabs, or newlines. In real-world ASPX markup, whitespace between controls is significant: it renders as a space between inline elements, creates vertical rhythm between block elements, and is essential for preserving the original page layout.

**Impact:** Any page that relies on whitespace between controls (virtually all of them) will have compressed/merged output that does not match the original Web Forms rendering.

**Example input:**
```html
<asp:Label Text="First" runat="server" />
<asp:Label Text="Second" runat="server" />
```
The space between the closing `/>` and the next `<asp:` is dropped, producing `FirstSecond` instead of `First Second` in rendered HTML.

**Fix:** Remove the `IsNullOrWhiteSpace` guard, or replace it with a check that only drops nodes that are entirely empty strings (not whitespace). Preserve single-space nodes and newline-only nodes as `MarkupString(" ")` / `MarkupString("\n")` respectively.

---

### 4.2 🔴 Parser Bug — Self-Closing Regex Breaks on `>` in Attribute Values

**File:** `AspxParser.cs` line 326  
**Severity:** High — causes parse failures and malformed output

The `SelfClosingAspTagRegex` uses a greedy match that terminates at the first `>` character inside the tag. When an attribute value contains a `>` character, the regex prematurely closes the tag. This affects two common real-world patterns:

1. **Comparison operators in data-binding expressions:** Inline expressions like `<%# Eval("Price") > 0 ? "positive" : "zero" %>` placed in an attribute value cause the regex to split the tag at the `>` after `0`.
2. **HTML comment wrappers around expression placeholders:** Some ASPX pages wrap expression blocks in HTML comments (e.g., `<!-- <%# SomeValue %> -->`) to prevent older parsers from choking on the `<%# ... %>` syntax. The `>` inside the comment content again terminates the regex match early.

**Impact:** Pages with data-binding expressions (`<%# ... %>`) in attribute positions will either fail to parse or produce garbled output. Data-binding expressions are used on nearly every data-bound ASPX page.

**Fix:** Replace the self-closing regex with a proper attribute parser that respects quoted strings and does not treat `>` inside quotes as a tag boundary. The parser should tokenize attribute values character-by-character, tracking quote depth, similar to how XML parsers handle this.

---

### 4.3 🔴 No Master Page Support

**Severity:** Critical — blocks the vast majority of real-world ASPX pages

Almost all production Web Forms applications use Master Pages. The `@Page MasterPageFile="~/Site.master"` directive is parsed but entirely ignored. The middleware renders only the `<asp:Content>` region markup, without the layout shell provided by the master page.

**Impact:** Any page using a master page will render without navigation, header, footer, scripts, or stylesheets — an incomplete fragment, not a usable page.

**Fix:** When a `MasterPageFile` directive is detected, the middleware must:
1. Locate and parse the `.master` file
2. Identify `<asp:ContentPlaceHolder>` regions
3. Match `<asp:Content ContentPlaceHolderID="...">` blocks from the page
4. Compose the final render tree: master page shell + injected content blocks

---

### 4.4 🔴 No Code-Behind Integration

**Severity:** Critical — blocks any page with server-side logic

The `@Page CodeFile="Dashboard.aspx.cs"` / `CodeBehind="Dashboard.aspx.cs"` directive attributes are parsed but ignored. The associated `.cs` file (which contains `Page_Load`, event handlers, and data-binding logic) is never compiled or executed.

**Impact:** Pages with code-behind (virtually all real applications) produce markup with no data — `Eval()` expressions return empty strings, `DataBind()` never executes, `Page_Load` never runs.

**Fix (short-term PoC):** Detect the code-behind file and emit a clear diagnostic rather than silently rendering empty data. This prevents misleading output.

**Fix (production):** Compile the code-behind class at startup using Roslyn and invoke lifecycle methods (`Page_Load`, `Page_Init`) before rendering. This is complex but feasible using the same approach as ASP.NET Web Forms' dynamic compilation pipeline.

---

### 4.5 🟡 Generic Control Type Resolution

**Severity:** Medium — affects data controls in typed scenarios

Controls registered with generic type parameters default to `object`. When ASPX markup specifies `ItemType="MyApp.Product"`, the parser captures this as a string attribute but the registry does not resolve it to a concrete generic type argument.

**Impact:** Strongly-typed `ItemType` binding (`<%# Item.Price %>`) will fail at runtime because the component is instantiated as `GridView<object>` rather than `GridView<Product>`.

**Fix:** Extend `AspxComponentRegistry` to accept a type resolver delegate. When `ItemType` is present, resolve the type from the application's assembly list and construct the appropriate closed generic `Type` at parse time.

---

### 4.6 🟡 `<%# Eval() %>` Expression Rendering

**Severity:** Medium — affects all data-bound pages

Data-binding expressions (`<%# Eval("FieldName") %>`, `<%# Bind("FieldName") %>`) are captured as `AspxExpressionNode` objects but are not evaluated. Currently they are either discarded or rendered as literal text.

**Impact:** All data grids, repeaters, form views, and list views will show literal expression text (`<%# Eval("Name") %>`) instead of actual data values.

**Fix:** At render time, when the data item context is available (i.e., when inside a `GridView` row, `Repeater` item, etc.), evaluate `Eval()` expressions using `DataBinder.Eval()` against the current data item. This requires threading a data-item context through the component tree builder.

---

### 4.7 🟡 User Control (`.ascx`) Support

**Severity:** Medium — required for modular applications

User controls (`.ascx` files registered via `<%@ Register %>`) are parsed at the directive level but not resolved or rendered. The `<%@ Register TagPrefix="uc" TagName="Breadcrumb" Src="~/Controls/Breadcrumb.ascx" %>` directive captures the registration but the `<uc:Breadcrumb>` element in the page body is then treated as an unknown HTML element.

**Impact:** Any page using user controls (most enterprise applications) will have missing content where controls should appear.

**Fix:** After parsing the `Register` directives, build a secondary control map from user control registrations. When the tree builder encounters a `<uc:*>` tag, recursively parse the referenced `.ascx` file and inline its render fragment.

---

### 4.8 🟡 Inline `<script>` and `<style>` Blocks

**Severity:** Low — cosmetic but impactful for JS-heavy pages

`<script runat="server">` blocks and `<style>` blocks embedded in ASPX are not processed. Server-side script blocks (which contain C# code in Web Forms) are especially problematic — rendering them as raw HTML would expose server-side source code.

**Fix:** Detect `<script runat="server">` and skip it entirely (or capture it for Roslyn analysis). Pass through client-side `<script>` and `<style>` blocks as raw HTML.

---

## 5. Missing Feature Areas

Beyond the parser bugs, the following functional areas are absent from the current implementation:

| Area | Priority | Notes |
|------|----------|-------|
| Master page composition | P0 | See §4.3 |
| Code-behind execution | P0 | See §4.4 |
| URL rewriting (`.aspx` → Blazor routes) | P1 | Without this, all links on the page still point to `.aspx` URLs that round-trip through the middleware |
| Session and ViewState bridging | P1 | Web Forms session state is `HttpSessionState`; Blazor uses `ISessionFeature` |
| Postback handling | P1 | Web Forms postbacks (`__doPostBack`) need to be intercepted and translated to Blazor event callbacks |
| Form `action` / `__VIEWSTATE` hidden fields | P2 | Middleware should strip these from rendered output |
| `HttpRequest` forwarding to `Page` context | P2 | `Request.QueryString`, `Request.Form` values not available in code-behind |
| Error page / `customErrors` handling | P2 | Exceptions in middleware pipeline expose stack traces without custom error pages |
| Output caching (`@OutputCache`) | P3 | Directive parsed but not implemented |
| Theme / skin integration | P3 | `@Page Theme="..."` not processed |

---

## 6. Test Coverage Assessment

> **Note on test counts:** The figures below reflect the current state of the `experiment/aspx-middleware` branch (~52 tests total). The "Projected (post-A)" column shows the expected counts after Milestone A regression tests (A-03, A-04) are written. Readers searching the repository will find approximately 52 tests today, not 92.

| Area | Current Tests | Projected (post-A) | Coverage |
|------|----------|---------------------|----------|
| Parser unit tests (tag parsing) | ~18 | ~33 | Good — basic tokenization covered; regression tests added in A-03/A-04 |
| Parser integration (full-page parse) | ~12 | ~18 | Limited — happy-path only |
| Component registry | ~6 | ~10 | Good — control type resolution |
| Tree builder | ~8 | ~15 | Fair — simple nesting covered, no data-binding tests |
| End-to-end SSR integration | ~8 | ~12 | Limited — no master page, no code-behind |
| Benchmark tests | 4 | 4 | Basic throughput only |
| **Total** | **~52** | **~92** | **Minimal for production** |

**Notable gaps in test coverage:**
- No tests for whitespace preservation (known parser bug — untested; addressed by A-03)
- No tests for attribute values containing `>` or HTML comment-wrapped expressions (known parser bug — untested; addressed by A-04)
- No tests for `@Page MasterPageFile=` handling
- No tests for `<%# Eval() %>` expression evaluation
- No tests for user control resolution
- No performance regression tests on large pages (>500 lines ASPX)

---

## 7. Performance Characteristics

The current implementation has not been benchmarked on realistic workloads. Known characteristics:

| Factor | Assessment |
|--------|-----------|
| Parser speed | Fast for small files — regex-based, single pass |
| Parser scalability | Unknown — regex may degrade on large files with complex nesting |
| Memory per request | Unknown — AST nodes are not pooled |
| SSR render time | Tied to `HtmlRenderer` — equivalent to Blazor SSR baseline |
| Caching | None — file re-parsed on every request |

**Minimum required:** Parse-level caching (compiled page descriptor per `.aspx` file, invalidated on file change) is needed before any production testing.

---

## 8. Recommendations for Next Steps

Based on this landscape evaluation, the following milestone structure is recommended. Each milestone is independent and incrementally useful — the middleware improves after each one.

### Milestone A — Parser Hardening (2–3 weeks)

Fix the two known parser bugs (§4.1, §4.2) and add regression tests that cover the failing scenarios. Also add whitespace-preservation integration tests. This milestone makes the parser reliable enough to be tested on real ASPX files without false failures masking real issues.

**Deliverables:**
- Fix whitespace-only text node suppression
- Fix self-closing tag regex for `>` in attribute values and HTML comment-wrapped expressions
- Add 15+ regression tests covering edge cases
- Parser benchmark: parse 50+ real ASPX files from `BeforeWebForms` sample without crashing

---

### Milestone B — Master Page Composition (3–4 weeks)

Implement master page support (§4.3). This is the highest-value capability because it unblocks testing on any realistic application. Without master pages, the middleware can only handle trivial single-file demos.

**Deliverables:**
- Locate and parse `Site.master` when `@Page MasterPageFile=` is set
- Compose `ContentPlaceHolder` / `Content` pairs into a unified render tree
- Handle nested master pages (master pages that reference other master pages)
- Integration tests with `BeforeWebForms` sample `Site.Master`
- Fall back to fragment rendering (current behavior) when no master page is set

---

### Milestone C — URL Rewriting and Link Translation (2–3 weeks)

Without URL rewriting, the middleware is a dead end — users click links and leave the middleware's zone of control. This milestone adds round-trip capability: any `.aspx` URL on the page is rewritten to stay within the middleware.

**Deliverables:**
- Post-render HTML rewriting: `href="*.aspx"` → route-based URL
- `action="*.aspx"` form target rewriting
- Strip `__VIEWSTATE` and `__EVENTVALIDATION` hidden fields from rendered output
- Strip `__doPostBack` JavaScript form submit calls
- Integration tests: navigate to a page, verify all links are `.aspx`-free

---

### Milestone D — Eval Expression Evaluation (2–3 weeks)

Implement `<%# Eval("Field") %>` expression evaluation during tree building (§4.6). This milestone unlocks read-only data-bound pages — grids, repeaters, and form views showing real data pulled from a .NET data source.

**Deliverables:**
- Thread a `DataItem` context through `AspxComponentTreeBuilder`
- Implement `Eval()` expression resolution against the current data item
- Support `<%# Bind("Field") %>` (read path only — two-way binding is a later milestone)
- Support format strings: `<%# Eval("Price", "{0:C}") %>`
- Integration tests: render a `<asp:Repeater>` with a real `DataSource` and verify field output

---

### Milestone E — Code-Behind Diagnostics (1–2 weeks)

Before attempting full code-behind execution (a multi-month effort), implement clear diagnostics so developers understand what is and is not supported. This milestone replaces silent failures with actionable messages.

**Deliverables:**
- Detect `CodeFile` / `CodeBehind` directive and emit a middleware diagnostic log entry
- Surface the diagnostic in the rendered HTML as a developer-mode warning overlay (similar to Blazor's error UI)
- Document what patterns work without code-behind and which require it
- Provide a migration guide: "Convert your code-behind to a Blazor partial class using these steps"

---

### Milestone F — User Control Resolution (3–4 weeks)

Implement `.ascx` user control resolution (§4.7). After master pages, this is the most common structural feature in enterprise Web Forms applications.

**Deliverables:**
- Parse `<%@ Register %>` directives and build a user control map
- Recursively parse `.ascx` files when `<uc:*>` tags are encountered
- Pass outer attributes to the inner user control's parameters
- Handle user controls that themselves reference other user controls (one level of nesting)
- Integration tests: page with 3 user controls renders expected HTML

---

## 9. Recommended Milestone Priority Order

| Order | Milestone | Value | Effort | Priority |
|-------|-----------|-------|--------|----------|
| 1 | **A — Parser Hardening** | Foundation | Low | P0 — must do first |
| 2 | **B — Master Page Composition** | Unblocks all real tests | High | P0 |
| 3 | **C — URL Rewriting** | Round-trip usability | Medium | P1 |
| 4 | **D — Eval Expressions** | Data display | High | P1 |
| 5 | **E — Code-Behind Diagnostics** | Developer UX | Low | P1 |
| 6 | **F — User Control Resolution** | Modular apps | High | P2 |

---

## 10. Go / No-Go Criteria

The ASPX middleware should **not** be promoted from experimental to the main library until:

1. ✅ All tests from the `experiment/aspx-middleware` branch pass on CI
2. ✅ Parser Hardening (Milestone A) complete — no known parser bugs
3. ✅ Master Page Composition (Milestone B) complete — realistic pages render
4. ✅ Integration test coverage ≥ 80 tests (up from 52)
5. ✅ Parse-level caching implemented (no per-request file re-parse)
6. ✅ Security review: server-side `<script runat="server">` blocks never rendered as HTML
7. ✅ Developer documentation: README explaining what is and is not supported
8. ✅ `experiment/` branch rebased cleanly onto current `dev`

---

*Document prepared for milestone planning purposes. See `dev-docs/milestones/ASPX-MIDDLEWARE-MILESTONES.md` for the detailed milestone plans.*
