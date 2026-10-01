# Web UX Polish — Result

**Scope:** Public Web (`web/`) only  
**Date:** 2026-10-01  
**Basis:** `docs/WEB_PRODUCT_UX_AUDIT.md`  
**Constraints honored:** no ingestion / content / processor / Android / deploy / schema changes  

---

## Scorecard

| Area | Verdict |
|------|---------|
| Homepage messaging | **PASS** |
| Book-home hierarchy | **PASS** |
| Start reading | **PASS** |
| Continue reading | **PASS** |
| Timed Ta'leem explanation | **PASS** |
| Single-unit chapter navigation | **PASS** |
| Reader Previous/Next | **PASS** |
| Reader position | **PASS** |
| Search | **PASS** |
| Mobile | **PASS** |
| Generic-book architecture | **PASS** |
| Semantic-search hiding | **PASS** |

---

## What changed (behavior)

1. **Homepage** — Reader-facing Taleem copy; book cards show **Open book**; description only if present.  
2. **Book home** — Hierarchy: Continue (local) → Start → Short Ta'leem → Search → Suggested → Browse chapters/topics (`<details>`). No equal-weight feature dump.  
3. **Start reading** — First unit from canonical chapter/unit order (generic).  
4. **Continue reading** — `localStorage` per `bookId` on unit open; book home resume card.  
5. **Ta'leem** — Plain-language session copy; honest “from the beginning”; removed “complete contiguous units”; **Start this session**. Algorithm unchanged.  
6. **Semantic search** — Removed from public navigation/payload; API/page file retained; keyword search remains.  
7. **Single-unit chapters** — Server redirect chapter → `/read/{unitId}` (verified for Ruku).  
8. **Reader** — Previous/Next by book order; “Reading N of M”; duplicate chapter title suppressed when equal to unit title.  
9. **Search** — Safe React highlight of query in snippets.  
10. **Lists** — Ordered lists use marker-derived `start` when source continues after intervening paragraphs (e.g. How to Perform step **4**).  

### List numbering note

How to Perform has LIST `1) 2) 3)` then paragraphs then LIST `4)`. Restart-at-1 was a Web `<ol>` reset after strip. Rendering now honors the leading marker number via `start`. If markers themselves restart at 1 in source with no continuation metadata, Web will not invent continuity (documented; no ingestion change).

---

## Re-audit checks

| Check | Result |
|-------|--------|
| First visit homepage | Reader copy; no developer wording |
| Open book | Card CTA “Open book” |
| Start reading | Links to first canonical unit |
| Return + Continue | Progress tracker writes localStorage; Continue section when present |
| 5 / 10-minute Ta'leem | Honest framing; no contiguous jargon |
| Search Ruku | Highlight helper wired; ranking unchanged |
| Single-unit chapter click | `…/chapters/{ruku}` → `/read/{unit}` |
| Prev/Next / first / last | First has Next; last has position 15/15 and no Next |
| Refresh reader / book home | Server routes 200; progress persists client-side |
| Mobile hierarchy | Chapters/topics behind disclosure |
| Semantic in nav | No `semantic-search` / “Semantic search” on book home |
| Generic hardcodes | `rg` over `web/src`: no Salah/Ruku/book-id/slug conditions |

Direct URL `/books/.../semantic-search` still resolves (route not deleted) but is not presented in UI — acceptable for POC hide.

---

## Files changed

- `web/src/app/page.tsx`
- `web/src/app/layout.tsx`
- `web/src/app/globals.css`
- `web/src/app/books/[slug]/page.tsx`
- `web/src/app/books/[slug]/chapters/page.tsx`
- `web/src/app/books/[slug]/chapters/[chapterId]/page.tsx`
- `web/src/app/books/[slug]/taleem/page.tsx`
- `web/src/app/books/[slug]/search/page.tsx`
- `web/src/app/read/[unitId]/page.tsx`
- `web/src/components/BookHomeView.tsx` *(new)*
- `web/src/components/ReadingProgressTracker.tsx` *(new)*
- `web/src/components/ReaderNav.tsx` *(new)*
- `web/src/components/ContentBlocks.tsx`
- `web/src/lib/progress.ts` *(new)*
- `web/src/lib/units.ts` *(new)*
- `web/src/lib/format.ts` *(new)*
- `web/src/lib/highlight.tsx` *(new)*
- `web/src/lib/load.ts`
- `docs/WEB_UX_POLISH_RESULT.md` *(this file)*

Unchanged by design: `semantic-search/page.tsx` (unlinked), backend, ingestion, Android.

---

**READY FOR LOCAL POC USER TESTING**
