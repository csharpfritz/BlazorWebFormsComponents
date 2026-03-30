### 2026-03-30T19-13-29Z: User directive
**By:** Jeffrey T. Fritz (via Copilot)
**What:** The 'analyze' command must NOT be public. Keep Prescanner/Analysis as internal modules that feed into 'migrate --report'. Only 'migrate' and 'convert' are public CLI commands.
**Why:** User request - competitive advantage. Analysis capabilities stay private.