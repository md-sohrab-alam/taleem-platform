# Ingestion Quality Audit — Processor v2.0.0

**Book:** Salah: Islamic Prayer (`ac0f5a18-5d34-43b2-bcd6-15fe43df9bb9`)  
**SourceDocument:** `ea0d4071-474f-4820-bed0-1fc00b067c50` (20 pages, reused — no re-upload)  
**Job:** latest SUCCEEDED · `ProcessorVersion=2.0.0` · status `NEEDS_REVIEW`  
**Audit date:** 2026-09-30  
**Method:** Admin API review/report/pages + PostgreSQL integrity queries + source-page comparison. No ingestion code changes. No approve/publish.

---

## Overall status: **NOT READY**

v2.0.0 is a large improvement over v1.0.0 over-segmentation (310 → 13 units) and does **not** invent source text. It is **not** structurally correct enough to proceed to publishing / public API / Web / Android testing as a representative production book.

Primary blockers: line-level ContentBlock fragmentation (broken reading continuity), TOC pollution driving bad chapter titles, back-matter leaked into the last unit, and confidence metrics that are not meaningfully persisted.

---

## Summary table

| Area | Verdict | Notes |
|------|---------|--------|
| Document zones | WARNING | p.2/p.20 mis-zoned; main reading mostly OK |
| TOC | FAIL | 60 raw vs ~15 logical; catalog/publisher noise |
| Chapters | WARNING | Core titles OK; 2–3 fragment titles; prophets hierarchy wrong |
| TaleemUnits | WARNING | Meaningful bodies; last unit includes back-matter |
| ContentBlocks | FAIL | Severe line-wrap fragmentation; weak typing |
| Language/script | WARNING | All `en`/Latin; no Arabic script present in PDF extract |
| Source fidelity | PASS | Blocks match extracted source; no invented text |
| Excluded elements | WARNING | Front matter mostly excluded; TOC over-marked |
| SearchChunks | WARNING | Wired correctly; inherit unit/title issues; one tiny-ish chunk |
| Keyword retrieval | PASS* | Chunk text contains expected terms (*admin/DB; public gated) |
| Semantic pipeline | WARNING | Pipeline exists; **FakeEmbeddingService** active |
| Confidence scoring | FAIL | Report “0 low confidence” is not meaningful |
| Database integrity | PASS | No orphans; orders contiguous; embeddings present |
| Large-book readiness | WARNING | Background + batch embeddings OK; reprocess wipes book structure |

\*Keyword checks run against SearchChunks in DB. Public keyword search returns empty for unpublished books (correct gating for content).

---

## Report counts (v2.0.0)

| Metric | Value |
|--------|------:|
| Pages | 20 |
| Sections | 1 |
| Chapters | 13 |
| TaleemUnits | 13 |
| ContentBlocks | 181 |
| SearchChunks | 23 |
| TOC entries (raw report) | 60 |
| Excluded elements | 82 |
| Heading candidates | 22 |
| Low confidence (report) | 0 |
| Heading-only units | 0 |
| Units/page avg | 0.65 |

Prior processor: **54 chapters / 310 units**.

---

## 1. Document zones — WARNING

| Page | Detected zone | Actual (human) | Assessment |
|------|---------------|----------------|------------|
| 1 | TITLEPAGE | Cover / title | PASS |
| 2 | TABLEOFCONTENTS | Publisher / copyright / imprint | **FAIL** (should be PUBLISHER_INFO / COPYRIGHT) |
| 3 | TABLEOFCONTENTS | Real CONTENTS | PASS |
| 4–18 | MAINCONTENT | Main teaching body | PASS |
| 19 | MAINCONTENT | Closing + “OTHER ESSENTIAL READINGS” promo | WARNING (mixed back-matter) |
| 20 | TABLEOFCONTENTS | Publisher catalog (“OTHER FREE LITERATURE”) | **FAIL** (APPENDIX / catalog, not book TOC) |

**Front matter as TaleemUnits:** author, publisher, copyright, website/email, printing lines, page numbers do **not** appear as unit titles. Good.

**Suspicious:** p.2 and p.20 inflate TOC machinery; p.19 promo text is still inside a reading unit.

---

## 2. TOC entries — FAIL

### Source TOC (page 3, logical)

Approximately **15** logical entries (some titles wrap across lines):

1. Salah, Islamic prayer → 3  
2. A Loving Relationship → 4  
3. Preparation for Salah → 6  
4. Facing Kabah, Qibla → 7  
5. Cycles of Postures- Rakah → 7  
6. How to Perform the Salah → 8  
7. Ruku → 10  
8. Sujood-Prostration → 10  
9. Tashahud → 11  
10. Timings and Rakah in Each Salah → 12  
11. Qiyam, Ruku and Sujood was always part of Prophets' Prayer → 13  
12. Qiyam or Standing → 14  
13. Ruku or Bowing down David and other prophets pbut → 14  
14. Sujood or Prostration by Jesus pbuh and all other prophets pbut → 15  
15. Tashahud or Sitting Down → 16  

### Detected

| Measure | Value |
|---------|------:|
| Raw detected count | 60 |
| Unique normalized titles | 59 |
| Duplicate title groups | 2 |
| Impossible page numbers | 3 (`2017`, `2019`, `2019` from edition years) |

**Incorrect / noise examples:** publisher catalog lines from p.20; imprint lines (`First Print October`, `Dr Irfan Jehangir`, emails); wrapped fragment `Qiyam, Ruku and Sujood was always`; `part of Prophets' Prayer`.

**Missing as clean single entries:** full wrapped titles “Ruku or Bowing down David and other prophets pbut”, “Sujood or Prostration by Jesus pbuh and all other prophets pbut”, “Qiyam… part of Prophets' Prayer”.

**Conclusion:** TOC detector is over-inclusive. Count 60 is **not** the booklet TOC; most entries are catalog/imprint/fragments. TOC is unsafe as sole hierarchy authority until cleaned.

---

## 3. Chapters — WARNING

| # | Title | Pages | Units | Blocks | Chunks | vs source |
|---|-------|------:|------:|-------:|-------:|-----------|
| 0 | Salah | 4–4 | 1 | 3 | 1 | PASS (intro) |
| 1 | A Loving Relationship | 5–7 | 1 | 19 | 3 | PASS |
| 2 | Preparation for Salah | 7–8 | 1 | 11 | 2 | PASS |
| 3 | Facing Kabah, Qibla | 8–8 | 1 | 2 | 1 | PASS (thin) |
| 4 | Cycles of Postures- Rakah | 8–9 | 1 | 16 | 2 | PASS |
| 5 | How to Perform the Salah | 9–11 | 1 | 28 | 2 | PASS |
| 6 | Ruku | 11–11 | 1 | 9 | 1 | PASS |
| 7 | Sujood-Prostration | 11–12 | 1 | 5 | 1 | PASS |
| 8 | Tashahud | 12–13 | 1 | 21 | 2 | PASS |
| 9 | Timings and Rakah in Each Salah | 13–15 | 1 | 25 | 3 | WARNING — also absorbs Prophets’ Prayer / Qiyam start |
| 10 | and other prophets pbut | 15–16 | 1 | 6 | 1 | **FAIL** truncated TOC wrap |
| 11 | and all other prophets pbut | 16–17 | 1 | 7 | 1 | **FAIL** truncated TOC wrap |
| 12 | Tashahud or Sitting Down | 17–20 | 1 | 29 | 3 | WARNING — extends into p.19–20 back-matter |

**Flags**

- No page numbers glued into titles (e.g. no `4A Loving…`) — PASS vs v1 failure mode.  
- Truncated titles: `#10`, `#11`.  
- Missing distinct chapter for “Prophets' Prayer” / “Qiyam or Standing”.  
- Ordering broadly follows booklet flow.  
- Chapter page ranges overlap at boundaries (e.g. 7–8 / 8–8) — acceptable but coarse.

---

## 4. TaleemUnits — WARNING

All 13 units: `ReviewStatus=PENDING`, heading-only = 0, each has body text.

| Title | Pages | Est. s | Blocks | Chunks | Assessment |
|-------|------:|-------:|-------:|-------:|------------|
| Salah | 4–4 | 99 | 3 | 1 | Meaningful intro |
| A Loving Relationship | 5–6* | 209 | 19 | 3 | Meaningful; over-split blocks |
| Preparation for Salah | 7–8 | 131 | 11 | 2 | Meaningful |
| Facing Kabah, Qibla | 8–8 | 30 | 2 | 1 | Thin but coherent |
| Cycles of Postures- Rakah | 8–9 | 114 | 16 | 2 | Meaningful |
| How to Perform the Salah | 9–11 | 131 | 28 | 2 | Meaningful; lists present |
| Ruku | 11–11 | 35 | 9 | 1 | Meaningful |
| Sujood-Prostration | 11–12 | 31 | 5 | 1 | Meaningful |
| Tashahud | 12–13 | 120 | 21 | 2 | Meaningful |
| Timings and Rakah… | 13–15 | 174 | 25 | 3 | **Merged unrelated following section** |
| and other prophets pbut | 15–16 | 57 | 6 | 1 | Bad title; content OK-ish |
| and all other prophets pbut | 16–17 | 54 | 7 | 1 | Bad title; content OK-ish |
| Tashahud or Sitting Down | 17–19* | 176 | 29 | 3 | **Includes “OTHER ESSENTIAL READINGS”** |

\*Unit page end in review payload vs chapter end can differ slightly.

**Not found as units:** bare metadata, TOC-only pages, publisher email as titles.

---

## 5. Deep unit inspection (selected)

### A Loving Relationship

Sequence is HEADING + many PARAGRAPH line fragments. Quran English quotes and hadith English remain `PARAGRAPH` (not `QURAN_TRANSLATION` / `HADITH_*`). Example break:  
`"Himself in the Quran as the Most Loving:"` as its own block after a cut line.  
Reference fragments: `"Hakim)"` alone.

### Preparation for Salah

HEADING + PARAGRAPH fragments; wudhu hadith split mid-sentence across p.7→p.8.

### How to Perform the Salah

HEADING + LIST/PARAGRAPH mix for steps. Transliteration lines (Fatiha / Ikhlas) stored as `PARAGRAPH`/`en` — conservative, not false TRANSLITERATION. Still line-fragmented.

### Ruku / Sujood-Prostration

Short instructional units; transliteration+translation pairs are adjacent paragraphs, not typed. Continuity OK at topic level.

### Tashahud

Long unit; Latin transliteration + English meaning as PARAGRAPH. Page break mid-instruction (`…as` / `above. And…`).

### Timings and Rakah in Each Salah

LIST items for five prayers — good. Then incorrectly continues into:

- `Qiyam, Ruku and Sujood was always part of`  
- `Prophets' Prayer`  
- `Qiyam or Standing`  

So this unit **merges two topical sections**.

---

## 6. Content continuity — FAIL

Observed pattern: PDF line wraps become separate ContentBlocks; many sentences are split.

Documented example:

1. Block: `Qiyam, Ruku and Sujood was always part of`  
2. Next block: `Prophets' Prayer`  

Also: page-boundary splits where prior line lacks terminal punctuation (Preparation wudhu hadith; Tashahud “as” / “above”; Cycles of Postures; prophets sections).

Heuristic count of suspicious page-boundary cuts: **11**.  
Same-unit mid-sentence paragraph splits (lowercase continuation): **4** (under-counts capitalized wrap continuations).

**Impact:** Admin review and clients see broken prose; SearchChunks partially repair via joining but still inherit awkward boundaries and overlap prefixes (`hat you should start…`).

---

## 7. Source fidelity — PASS

| Check | Result |
|-------|--------|
| Non-trivial ContentBlocks found in extracted source (page± / main span) | **170/170 (100%)** after skipping 11 very short fragments |
| Unique main-content words (≥4 chars) present in canonical text | **862/863 (99.9%)** |
| Invented / hallucinated canonical text | **Not detected** |
| Arabic/Urdu corruption | N/A — extract contains **no** Arabic-script runs |

Canonical text is a subset of extracted text with whitespace/line normalization. Missing coverage is almost entirely intentional exclusions (front matter / catalog) plus fragmentation (words still present, split across blocks).

---

## 8. Block classification — WARNING / FAIL (typing)

| BlockType | Count |
|-----------|------:|
| PARAGRAPH | 159 |
| HEADING | 13 |
| LIST | 9 |
| TRANSLITERATION | 0 |
| QURAN_ARABIC | 0 |
| QURAN_TRANSLATION | 0 |
| HADITH_* | 0 |
| REFERENCE | 0 |
| DUA | 0 |

**PASS:** Ordinary English is **not** mislabeled TRANSLITERATION (v1 regression fixed).  
**FAIL/WARNING:** Nearly everything is PARAGRAPH — including quotes, transliterations, references, and line fragments. Acceptable as conservative typing only if continuity is fixed; currently classification quality is too flat for “structured Islamic content” product claims.

All 181 blocks: `language=en`, `verificationStatus=UNVERIFIED`.

---

## 9. Language / script — WARNING

| Language | Blocks |
|----------|-------:|
| en | 181 |
| ar / ur / mixed / unknown | 0 |

| Script (observed in text) | Blocks |
|---------------------------|-------:|
| Latin | 181 |
| Arabic Unicode | 0 |

No reversed RTL strings observed (nothing to reverse). Marks not stripped beyond PDF extraction limitations (spaced website glyphs on cover already in source).

---

## 10. Confidence scoring — FAIL (implementation)

Report claims **Low confidence: 0**.

Investigation:

- `HeadingCandidate.Confidence` is computed at analysis time but **not persisted** on chapters/units/blocks.  
- `ContentBlock.VerificationStatus` uses extraction `pageConfidence` (`FlaggedLowConfidence` only if `< 0.6`). Extracted pages are typically `0.9` → all `UNVERIFIED`.  
- No min/max/avg heading confidence available post-ingest in DB/API.  
- Unit-level `StructureConfidence` field does not exist.

**Verdict:** “0 low confidence” means “nothing flagged by a weak gate,” **not** “high structural certainty.” Treat as an implementation gap.

---

## 11. Excluded elements — WARNING

82 excluded. Approximate categories from reason strings:

| Category | ~Count |
|----------|-------:|
| TOC / TocEntry | 61 |
| Page numbers | 16 |
| Zone (title page) | 4 |
| Contact | 1 |
| Header/footer/copyright (explicit) | 0 |

Sample inspection (20): page numbers, TOC lines, imprint/edition, catalog titles, contact URL — appropriate exclusions.

**Suspicious exclusions:** none of the main p.4–18 teaching paragraphs appear in the excluded sample.  
**Over-exclusion risk:** p.2 imprint marked as TocEntry (zone wrong); catalog on p.20 marked TOC.

Genuine main reading content does **not** appear removed from units (fidelity supports this).

---

## 12. SearchChunks — WARNING

23 chunks; all have embeddings (`HAS_EMBED`); no duplicate chunk texts; all resolve to TaleemUnits; metadata includes `chapterId`, `pageStart`/`pageEnd`, `blockIndexes`.

| Check | Result |
|-------|--------|
| Front-matter-only chunks | Not observed |
| TOC-only chunks | Not observed |
| Cross-unit chunks | Not observed |
| Size | ~283–1502 chars (Facing Kabah short but above empty) |
| Broken overlap prefix | Yes (`hat you should…`, `s in the Qur’an…`) from overlap window |
| Back-matter in chunk | Yes — last Sitting Down chunk includes “OTHER ESSENTIAL…” |

---

## 13. Retrieval — WARNING

### Keyword (chunk corpus)

Queries `Salah`, `Ruku`, `Sujood`, `Qibla`, `Tashahud` all hit SearchChunks tied to the expected topical units (verified via SQL `ILIKE` on chunks).

Public `GET /api/v1/books/{id}/search?q=Salah` → **200 `[]`** while unpublished (content gated).

### Semantic

`Embedding:Provider = fake` → **FakeEmbeddingService** (SHA256-derived vectors).

> **Semantic relevance cannot be meaningfully evaluated with fake embeddings.**

Technical pipeline exists: embed → store vector → `POST /api/v1/search/semantic` filters published books → returned empty for this NEEDS_REVIEW book (gating works).

---

## 14. API contract (unpublished book) — PASS with notes

| Endpoint | Result for this book |
|----------|----------------------|
| `GET /api/v1/books` | 200 · empty page (`totalCount=0`) |
| `GET /api/v1/books/{id}` | **404** |
| `GET /api/v1/books/{id}/home` | **404** |
| `GET /api/v1/books/{id}/chapters` | 200 `[]` |
| `GET /api/v1/books/{id}/topics` | 200 `[]` |
| `GET /api/v1/books/{id}/taleem?minutes=5` | 404 |
| `GET /api/v1/books/{id}/search?q=…` | 200 `[]` |
| `POST /api/v1/search/semantic` | 200 empty for unpublished |

**NEEDS_REVIEW content is not exposed as readable public book payloads.**  
Note: some list-shaped endpoints return empty arrays rather than 404 — acceptable but slightly inconsistent.

Unit tests (`PublishingRulesTests`) encode “public only serves Published” at rule level; full HTTP integration fixture coverage is thin.

---

## 15. Database integrity — PASS

| Check | Result |
|-------|--------|
| Orphan TaleemUnits | 0 |
| Orphan ContentBlocks | 0 |
| Orphan SearchChunks | 0 |
| Chunks without embedding | 0 |
| Blocks without SourcePage | 0 |
| Duplicate chunk texts | 0 |
| Chapter OrderIndex | 0…12 contiguous |
| Unit OrderIndex per chapter | all `0` (one unit/chapter) |

Provenance: every block has `SourcePage` + `SourceReference`.

---

## 16. Large-book readiness — WARNING

| Topic | Status |
|-------|--------|
| Ingestion runs in background (`Task.Run` after queue job) | Yes |
| HTTP need not stay open | Yes |
| Embedding batching (batch size 16) | Yes |
| Page/batch extraction architecture | Layout extract is whole-document in memory — **risk at 500–1000 pages** |
| ProcessorVersion persisted | Yes (`2.0.0`) |
| Reprocess without re-upload | Yes |
| Reprocess vs published versioning | **Clears all book structure** (sections/chapters/units/blocks/chunks) — unsafe if published; no draft/published dual-version |
| ContentVersion bump on reprocess | Not observed |

---

## Recommended fixes (do not implement until approved)

1. **Paragraph reconstruction:** merge line-wrap fragments into true paragraphs before ContentBlock emission (use geometry + mid-sentence heuristics). Highest priority.  
2. **TOC hygiene:** restrict TOC parsing to CONTENTS pages; exclude catalog/imprint; join wrapped TOC titles; validate page numbers ∈ [1, PageCount].  
3. **Chapter title repair:** prefer body heading text over truncated TOC fragments; merge wrap-tails (`and other prophets…`).  
4. **Hierarchy:** keep “Prophets' Prayer” / Qiyam / Ruku-of-prophets / Sujood-of-prophets as distinct chapters/units; stop absorbing them into Timings.  
5. **Back-matter cut:** stop reading units at APPENDIX / promo / catalog zones (p.19–20).  
6. **Zone fixes:** p.2 → PUBLISHER_INFO/COPYRIGHT; p.20 → APPENDIX/UNKNOWN catalog.  
7. **Confidence:** persist heading/structure confidences; flag truncated titles, heading-density anomalies, mid-sentence block rates; stop reporting “0 low confidence” without those signals.  
8. **Block typing (later):** only after continuity is fixed — quotes/transliteration/reference.  
9. **Versioning:** reprocess must not destroy published snapshots; bump content versions.  
10. **Embeddings:** real provider before semantic quality claims; keep fake for CI only.

---

## Final recommendation

**Do not proceed to publishing / public API / Web / Android client testing on this book yet.**

Safe next step after approval: fix continuity + TOC/chapter title issues, reprocess the same SourceDocument on a new processor version, re-audit, then consider a controlled publish test.
