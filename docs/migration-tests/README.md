# Migration Test Runs

Benchmarked migrations of real Web Forms applications using the BWFC migration toolkit.

## Run History

| Run | Source App | Date | Score | Render Mode | Key Outcome |
|-----|-----------|------|-------|-------------|-------------|
| [Run 1](wingtiptoys-2026-03-04/report.md) | WingtipToys 2013 | 2026-03-04 | ✅ Complete | — | Initial benchmark |
| [Run 2](wingtiptoys-run2-2026-03-04/report.md) | WingtipToys 2013 | 2026-03-04 | ✅ 11/11 PASS | — | Feature validation |
| [Run 3](wingtiptoys-run3-2026-03-04/report.md) | WingtipToys 2013 | 2026-03-04 | ✅ 11/11 PASS | — | From-scratch validation |
| [Run 4](wingtiptoys-run4-2026-03-04/report.md) | WingtipToys 2013 | 2026-03-04 | ✅ 11/11 PASS | — | Enhanced script |
| [Run 8](wingtiptoys-run8.md) | WingtipToys 2013 | 2026-03-06 | ✅ 14/14 (100%) | InteractiveServer | First 100% acceptance pass |
| [Run 9](wingtiptoys-run9.md) | WingtipToys 2013 | 2026-03-06 | ❌ FAIL | InteractiveServer | Visual regression — no CSS/images |
| [Run 10](wingtiptoys-run10.md) | WingtipToys 2013 | 2026-03-07 | ❌ FAIL (20/25) | InteractiveServer | Coordinator process violation |
| [Run 11](wingtiptoys-run11.md) | WingtipToys 2013 | 2026-03-07 | ❌ 17/25 (68%) | InteractiveServer | ListView placeholder + Scripts/ gaps |
| [Run 12](wingtiptoys-run12.md) | WingtipToys 2013 | 2026-03-08 | ✅ 25/25 (100%) | InteractiveServer | First perfect score |
| [Run 13](wingtiptoys-run13.md) | WingtipToys 2013 | 2026-03-08 | ✅ 25/25 (100%) | **SSR** | SSR default, 22 min total, 3 manual fixes |

## Pipeline Convergence

Across 13 iterative runs, the automated pipeline improved dramatically:

- **Pass rate:** 56% → 100%
- **Total time:** ~2 hours → ~22 minutes
- **Manual fixes:** 8+ → 3 (trending toward 0)
- **Script automation:** ~40% → ~60% of total migration work

The key architectural decision was switching from InteractiveServer to **SSR (Static Server Rendering)** as the default render mode in Run 13. SSR preserves the Web Forms server-rendering model, giving pages full access to `HttpContext`, cookies, and session — eliminating an entire class of migration problems.
