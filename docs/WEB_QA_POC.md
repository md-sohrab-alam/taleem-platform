# Public Web QA — POC (local) — Post-fix re-audit

**Scope:** http://localhost:3001/ against http://localhost:5080/  
**Book under test:** Salah: Islamic Prayer (`salah-islamic-prayer-2` / `ac0f5a18-5d34-43b2-bcd6-15fe43df9bb9`) — **PUBLISHED**  
**Ingestion:** processor 2.2.0 (not modified this audit)  
**Role:** QA re-audit after Web/public API polish  
**Date:** 2026-10-01 (re-audit)  
**Baseline:** prior audit in this file (pre-fix) had Topics / Error handling / metadata FAILs  

---

## Final verdict: **READY FOR WEB FIX/POLISH PHASE**

High-severity public defects from the first audit are **fixed and verified**: clean reader (no block metadata), semantic lists, friendly 404s, filtered topics, contiguous Ta'leem, featured readings, search empty-state/labels. Remaining items are polish / content / RTL / real-embeddings — not blocking core reading.

---

## Scorecard

| Area | Pre-fix | Post-fix |
|------|----------|-----------|
| Library | PASS | **PASS** |
| Book dashboard | WARNING | **PASS** |
| Chapter navigation | PASS | **PASS** |
| Reader | WARNING | **PASS** |
| Responsive design | WARNING | **WARNING** |
| Keyword search | WARNING | **PASS** |
| Semantic pipeline | WARNING | **WARNING** |
| Ta'leem duration | WARNING | **PASS** |
| Topics | FAIL | **PASS** |
| RTL readiness | WARNING | **WARNING** |
| Error handling | FAIL | **PASS** |
| Accessibility basics | WARNING | **WARNING** |
| Performance basics | PASS | **PASS** |
| Security basics | WARNING | **PASS** |
| Generic-book architecture | PASS | **PASS** |

---

## Fixes verified (vs prior FAILs)

| Prior issue | Severity | Post-fix result |
|-------------|----------|------------------|
| F1. Reader showed `p.N · BLOCKTYPE` | High | **PASS** — no public metadata labels; duplicate unit-title HEADING suppressed |
| F2. Missing/unpublished routes → HTTP 500 | High | **PASS** — `/books/salah-islamic-prayer` and invalid unit → **404** with friendly “Not found” |
| F3. Junk topics (`Each`, `Down`, `Kabah,`) | High | **PASS** — 9 topics: Kabah, Prostration, Qibla, Qiyam, Rakah, Ruku, Salah, Sujood, Tashahud |
| LIST as `<p>` | Medium | **PASS** — Timings renders ordered `<ol>/<li>` |
| Duplicate search titles | Medium | **PASS** — chapter line hidden when same as unit; Ruku query top hit is **Ruku** |
| Empty search state | Medium | **PASS** — “No results…” copy present in UI |
| Ta'leem skip holes | Medium | **PASS** — contiguous complete units; UI notes “complete contiguous units” |
| Featured / Continue unused | Medium | **PASS** — Featured readings shown (5); Continue when progress exists |
| “1 readings” grammar | Low | **PASS** — “1 reading” |
| Search label | Low | **PASS** — `sr-only` label + `focus-visible` styles |

---

## 1. Public library — PASS

**Route:** `/`

| Check | Result |
|-------|--------|
| Page loads | Yes (200) |
| Only PUBLISHED books | Yes — unpublished twin `salah-islamic-prayer` hidden |
| Salah visible | Title, author, **EN** language |
| Click opens book | → `/books/salah-islamic-prayer-2` |
| Direct URL / refresh | Works |
| Admin/draft leak | None |

---

## 2. Book home / dashboard — PASS

**Route:** `/books/salah-islamic-prayer-2`

- Title, author, Language: EN  
- Featured readings (5) from API  
- Reading options: chapters, topics, 5/10/15 Ta'leem, search, semantic  
- Chapters: 15, “1 reading” grammar  
- Topics: 9 meaningful chips  
- Continue reading: not shown when no stored progress (expected)

---

## 3. Chapters (15) — PASS

Same canonical set as `GET /api/v1/books/{id}/chapters` (order 0–14). All chapter + unit routes load.

---

## 4. Reader quality — PASS

**Ruku** (`/read/d5a2657d-…`): clean prose; no `p.N · TYPE`; back links Library / Book home / Chapter; duplicate HEADING omitted.

**Timings** (`/read/ff72ee47-…`): numbered steps as real ordered list; no metadata; OCR artifact `24434!` still in source text (**content**, Low).

---

## 5. RTL / multilingual readiness — WARNING

Per-block RTL helper + Arabic font stack remain; `<html lang="en">` still global. **Not proven** without a published Arabic/Urdu book.

---

## 6. Responsive — WARNING

Container layout still mobile-safe; no new overflow found on re-check. Full 1440/1366/768/390 matrix not re-run exhaustively this pass — keep WARNING until device matrix is signed off.

---

## 7. Keyword search — PASS

**Route:** `/books/{slug}/search`

| Query | Count | Top result | Relevant? |
|-------|------:|------------|-----------|
| Ruku | 4 | **Ruku** | Yes |
| xyzzynomatch123 | 0 | Empty-state message | Yes |

Labeled input, loading button, no duplicate chapter line when titles match. Ranking still keyword/ILIKE (acceptable for POC).

---

## 8. Semantic search — WARNING

**Provider:** `FakeEmbeddingService`  

**SEMANTIC QUALITY NOT TESTED — FAKE EMBEDDINGS**

Plumbing unchanged: Web → `POST /api/v1/search/semantic` → unit → reader.

---

## 9. Ta'leem duration — PASS

| Minutes | Units | Notes |
|--------:|------:|-------|
| 5 | 2 | Opener + Loving — contiguous |
| 10 | 5 | Contiguous prefix; stops instead of skipping holes |
| 15 | 9 | Contiguous complete units |

UI: “complete contiguous units”; links open readers.

---

## 10. Topics — PASS

API filter `TopicQuality` + UI chips. Selecting e.g. Ruku returns related units. Still auto-inferred from titles (not curated taxonomy) — acceptable for POC; quality is now reader-usable.

---

## 11. Direct URL / refresh — PASS

Verified 200: library, book home, chapters, reader, search, taleem, topics.  
Verified **404**: unpublished slug, invalid unit.

---

## 12. API failure UX — PASS

| Scenario | Actual |
|----------|--------|
| Unknown unit / unpublished book | Friendly Not found + Back to library |
| Empty search | Explicit empty copy |
| Library API down | Red banner on `/` (unchanged) |
| Client error boundary | `error.tsx` “Something went wrong” + Try again |

---

## 13. Accessibility basics — WARNING

Improved: search labels, focus-visible, semantic lists, quieter reader for SR. Remaining: fixed `lang="en"`, no cover alts, default focus polish incomplete — not full WCAG claim.

---

## 14. Performance basics — PASS

No new duplicate-call regressions observed; local warm routes respond quickly after compile.

---

## 15. Security / data exposure — PASS

- Unpublished not in library; direct access 404 (no content leak)  
- No admin API from public web  
- Block type / source page no longer rendered publicly  
- No client secrets beyond `NEXT_PUBLIC_API_BASE_URL`

---

## 16. API ↔ Web consistency — PASS

UI driven by public DTOs; featured/topics/taleem/search match API samples.

---

## 17. Generic-book check — PASS

No Salah-specific production branching in `web/src` (placeholders only in search hint text).

---

## Open WARNINGs (non-blocking)

1. **RTL** — architecture stub; needs multilingual published book QA  
2. **Semantic quality** — fake embeddings  
3. **Responsive matrix** — not fully re-signed on all viewports this pass  
4. **A11y** — document language/dir not book-aware  
5. **Content OCR** — `24434!`, `pbut` fidelity (ingestion/source, not Web regression)

---

## What works well

1. Published-only library with language on cards  
2. Useful book home (featured + chapters + topics + Ta'leem)  
3. Clean reader with lists and no debug metadata  
4. Contiguous duration sessions  
5. Friendly 404s  
6. Keyword search with usable ranking for “Ruku”  
7. Generic books-as-data Web architecture  

## Functional bugs

None blocking found in this re-audit.

## UX problems (remaining polish)

1. Featured list can surface very long chapter-derived titles  
2. Dashboard still link-heavy vs guided “start here” coaching  
3. Continue reading depends on progress analytics (often empty locally)

## Content-rendering problems

1. Source OCR/noise shown as-is (`24434!`)  

## Search problems

1. Semantic relevance not testable (fake embeddings)  
2. Keyword still not true relevance-ranked beyond ILIKE/chunk order  

## Responsive / mobile problems

1. Re-confirm tablet/mobile matrix on real devices before release  

---

## Recommended next polish (severity order)

1. **Medium:** Real embedding provider → re-QA semantic  
2. **Medium:** Publish a short Arabic/Urdu sample book → prove RTL  
3. **Low:** Book-aware `lang`/`dir` on `<html>` or article  
4. **Low:** Exhaustive responsive pass + denser mobile nav  
5. **Later:** Curated topics vs title tokens; cover images  

---

## Screenshot notes

Browser captures from this re-audit (session): library, book dashboard (featured + topics), Ruku reader (clean), Timings reader (ordered list), search “Ruku”, 5-minute Ta'leem, unpublished 404. Prior files under `docs/web-qa/` remain useful as baseline; replace when committing new PNGs from the tool output path.

---

**READY FOR WEB FIX/POLISH PHASE**
