# Ingestion Quality Audit — Processor v2.1.1

**Book:** Salah: Islamic Prayer (`ac0f5a18-5d34-43b2-bcd6-15fe43df9bb9`)  
**SourceDocument:** `ea0d4071-474f-4820-bed0-1fc00b067c50` (20 pages, reused — no re-upload)  
**ContentVersion:** 5 · status `NEEDS_REVIEW`  
**Report:** `ProcessorVersion=2.1.1` · 15 chapters · 15 units · 113 ContentBlocks · 23 SearchChunks · 15 TOC · 95 excluded · 16 heading candidates · **3 low-confidence items**  
**Structure confidence:** min 0.840 · avg 0.969 · max 1.000  
**Heading confidence:** min 0.480 · avg 0.765 · max 0.880  
**Audit date:** 2026-10-01  
**Method:** Admin API review/report/pages + PostgreSQL integrity queries + source-page comparison. **No ingestion code changes. No approve/publish/reprocess.**

---

## Overall status: **NOT READY FOR PUBLISH TEST**

v2.1.1 is a large improvement over v2.0.0: TOC is clean (15), zones are correct, Prophets’ Prayer hierarchy is distinct from Timings, Sitting Down is present, back-matter is cut, and source fidelity is strong (no invented text).  

It is **not** ready for a publish / public-client reading test yet because **paragraph and list reconstruction is still incomplete**: multiple instructional units still emit mid-sentence ContentBlocks (especially *How to Perform the Salah* and *Timings*), search chunks inherit mid-word overlap fragments, and block typing remains almost entirely `PARAGRAPH`/`LIST` with no Quran/transliteration labels.

---

## Summary table

| Area | Verdict | Notes |
|------|---------|--------|
| Document zones | PASS | p.1 title, p.2 publisher, p.3 TOC, p.4–18 main, p.19–20 appendix |
| TOC | PASS | 15 logical entries; wraps joined; OCR split-page repaired; catalog excluded |
| Chapters vs TOC/headings | WARNING | 15/15 topical coverage; opener title truncated (`Salah` vs `Salah, Islamic prayer`); OCR `pbut` retained |
| TaleemUnits / boundaries | WARNING | Bodies meaningful; chapter end pages often include next heading page; unit ranges tighter/correct |
| Ruku ↔ Sujood on p.11 | PASS | No cross-leak of exclusive Ruku/Sujood instructional text |
| Paragraph/list reconstruction | FAIL | Mid-sentence LIST wraps and prose wraps remain in several units |
| Excluded elements (95) | PASS | No genuine p.4–18 body prose excluded; TOC/imprint/catalog/page numbers only |
| Source fidelity | PASS | Unit text matches extract; no invented/reordered body; OCR quirks preserved |
| ContentBlock classifications (113) | WARNING | 15 HEADING / 90 PARAGRAPH / 8 LIST; religious/transliteration typing unused |
| SearchChunks (23) + IDs | WARNING | Wired to units; metadata uses `blockIndexes` not block GUIDs; overlap mid-word starts |
| TOC not in reading/search | PASS | CONTENTS / catalog / imprint absent from units and chunks |
| Confidence scoring | PASS | 3 flagged items are real residual fragments; structure conf persisted |
| Semantic / embeddings | WARNING | `FakeEmbeddingService` active — not evidence of semantic quality |
| Database integrity | PASS | 15/15/113/23; no orphans; contiguous block orders; embeddings present |

---

## Report counts (v2.1.1 vs v2.0.0)

| Metric | v2.0.0 | v2.1.1 |
|--------|------:|------:|
| Chapters | 13 | **15** |
| TaleemUnits | 13 | **15** |
| ContentBlocks | 181 | **113** |
| SearchChunks | 23 | 23 |
| TOC entries | 60 | **15** |
| Excluded elements | 82 | 95 |
| Heading candidates | 22 | 16 |
| Low confidence (report) | 0 (meaningless) | **3** (real flags) |
| Structure confidence avg | n/a | **0.969** |
| Units/page avg | 0.65 | 0.75 |
| Heading-only units | 0 | 0 |
| Structural anomalies | false | false |

---

## 1. Document zones — PASS

| Page | Detected zone | Actual (human) | Assessment |
|------|---------------|----------------|------------|
| 1 | TITLEPAGE | Cover / title | PASS |
| 2 | PUBLISHERINFO | Publisher / editions / contact | PASS |
| 3 | TABLEOFCONTENTS | Real CONTENTS | PASS |
| 4–18 | MAINCONTENT | Main teaching body | PASS |
| 19 | APPENDIX | “OTHER ESSENTIAL READINGS” promo | PASS |
| 20 | APPENDIX | “OTHER FREE LITERATURE & PUBLICATIONS” catalog | PASS |

**Front matter as TaleemUnits:** author, publisher, copyright, website/email, printing lines, page numbers do **not** appear as unit titles. Catalog pages do not become reading units.

---

## 2. TOC entries — PASS

### Source TOC (page 3, logical ~15)

Printed page numbers in the booklet are **PDF page − 1** (e.g. printed “4” appears on PDF page 5). Detected TOC page numbers match the printed scale.

| # | Detected title | TOC page | Body chapter start (PDF) | Assessment |
|---|----------------|---------:|--------------------------|------------|
| 1 | Salah, Islamic prayer | 3 | Chapter `Salah` @ PDF 4 | WARNING — title truncated in chapter |
| 2 | A Loving Relationship | 4 | PDF 5 | PASS |
| 3 | Preparation for Salah | 6 | PDF 7 | PASS |
| 4 | Facing Kabah, Qibla | 7 | PDF 8 | PASS |
| 5 | Cycles of Postures- Rakah | 7 | PDF 8 | PASS |
| 6 | How to Perform the Salah | 8 | PDF 9 | PASS |
| 7 | Ruku | 10 | PDF 11 | PASS |
| 8 | Sujood-Prostration | 10 | PDF 11 | PASS |
| 9 | Tashahud | 11 | PDF 12 | PASS |
| 10 | Timings and Rakah in Each Salah | 12 | PDF 13 | PASS |
| 11 | Qiyam, Ruku and Sujood was always part of Prophets' Prayer | 13 | PDF 14 | PASS (wrap joined) |
| 12 | Qiyam or Standing | 14 | PDF 15 | PASS (OCR `1 4` → 14) |
| 13 | Ruku or Bowing down David and other prophets pbut | 14 | PDF 15 | PASS (wrap joined; OCR `pbut` from source) |
| 14 | Sujood or Prostration by Jesus pbuh and all other prophets pbut | 15 | PDF 16 | PASS |
| 15 | Tashahud or Sitting Down | 16 | PDF 17 | PASS |

**Not present:** catalog/imprint years, publisher lines, wrap-tail fragments as standalone TOC rows.

---

## 3. Chapters vs TOC / source headings — WARNING

| # | Chapter title (body-preferred) | PDF pages (chapter) | Unit pages | Blocks | vs TOC / heading |
|---|-------------------------------|---------------------|------------|-------:|------------------|
| 0 | Salah | 4–5 | 4–4 | 3 | WARNING — TOC/source opener is “Salah, Islamic prayer”; “Islamic prayer” is a separate flagged block |
| 1 | A Loving Relationship | 5–7 | 5–6 | 12 | PASS |
| 2 | Preparation for Salah | 7–8 | 7–7 | 5 | PASS |
| 3 | Facing Kabah, Qibla | 8–8 | 8–8 | 2 | PASS (thin) |
| 4 | Cycles of Postures- Rakah | 8–9 | 8–9 | 9 | PASS |
| 5 | How to Perform the Salah | 9–11 | 9–11 | 18 | PASS topical; continuity WARNING (see §6) |
| 6 | Ruku | 11–11 | 11–11 | 6 | PASS |
| 7 | Sujood-Prostration | 11–12 | 11–12 | 5 | PASS |
| 8 | Tashahud | 12–13 | 12–13 | 14 | PASS |
| 9 | Timings and Rakah in Each Salah | 13–14 | 13–14 | 12 | PASS topical; list wraps FAIL (see §6) |
| 10 | Qiyam, Ruku and Sujood was always part of Prophets' Prayer | 14–15 | 14–14 | 2 | PASS — **distinct from Timings** |
| 11 | Qiyam or Standing | 15–15 | 15–15 | 5 | PASS |
| 12 | Ruku or Bowing down David and other prophets pbut | 15–16 | 15–16 | 4 | PASS title (OCR `pbut` is source) |
| 13 | Sujood or Prostration by Jesus pbuh and all other prophets pbut | 16–17 | 16–16 | 5 | PASS |
| 14 | Tashahud or Sitting Down | 17–18 | 17–18 | 11 | PASS — **present; ends before appendix** |

**Flags**

- All 15 logical TOC topics are represented as chapters/units.  
- Prophets’ Prayer / Qiyam / prophetic Ruku / prophetic Sujood / Sitting Down are **not** absorbed into Timings (v2.0.0 failure fixed).  
- Chapter `sourcePageEnd` often equals the **next** heading’s page (range overlap). Unit page ranges track actual belonging blocks more tightly — acceptable for navigation but coarse for chapter page metadata.  
- OCR title noise (`pbut`) is fidelity-preserving, not invention.

---

## 4. TaleemUnits / source locations — WARNING

All 15 units: `ReviewStatus=PENDING`, heading-only = 0, each has body text, `StructureConfidence` persisted (0.84–1.00).

| Title | Unit pages | Est. note | Assessment |
|-------|------------|-----------|------------|
| Salah | 4–4 | Short opener | WARNING — subtitle line split |
| A Loving Relationship | 5–6 | 12 blocks | Meaningful; one mid-sentence wrap remains |
| Preparation for Salah | 7–7 | 5 blocks | Meaningful |
| Facing Kabah, Qibla | 8–8 | 2 blocks | Thin but coherent |
| Cycles of Postures- Rakah | 8–9 | 9 blocks | Meaningful; bullet `?` OCR as prose |
| How to Perform the Salah | 9–11 | 18 blocks | Correct span; LIST wraps incomplete |
| Ruku | 11–11 | 6 blocks | Correct start/end on p.11 |
| Sujood-Prostration | 11–12 | 5 blocks | Starts after Ruku on p.11; no Ruku leak |
| Tashahud | 12–13 | 14 blocks | Correct |
| Timings… | 13–14 | 12 blocks | Correct span; LIST wraps incomplete |
| Prophets' Prayer | 14–14 | 2 blocks | Thin intro section — correct separation |
| Qiyam or Standing | 15–15 | 5 blocks | Continuity includes Job quote |
| Prophetic Ruku… | 15–16 | 4 blocks | Correct |
| Prophetic Sujood… | 16–16 | 5 blocks | Correct; does not steal Sitting Down |
| Tashahud or Sitting Down | 17–18 | 11 blocks | Correct; **no p.19–20 catalog** |

**Not found as units:** CONTENTS rows, publisher/email, “OTHER ESSENTIAL…”, “OTHER FREE LITERATURE…”.

---

## 5. Ruku vs Sujood-Prostration (shared page 11) — PASS

**Ruku unit blocks (all p.11):** heading `Ruku` → bowing instruction → `Subhaana Rabbi-yal Azeem` → standing from ruku → `Rabbanaa lakal hamdu` → straighten back.  
No “forehead on the floor” / Sujood heading text.

**Sujood-Prostration unit:** heading on p.11 → prostration instruction → `Subhaana Rabbiyal Aalaa` → p.12 sitting between prostrations → bridge sentence naming Tashahud (source text before next heading).  
No “hands over the knees” / `Subhaana Rabbi-yal Azeem` Ruku-only content.

Boundary is at the body heading on the shared page — correct.

---

## 6. Paragraph / list reconstruction — FAIL

Reconstruction improved aggregate block count (181 → 113) and many multi-line paragraphs (e.g. Qiyam Job quote is one block). **Residual wrap failures remain.**

### Confirmed mid-sentence ContentBlocks (lowercase continuation)

| Unit | Page | Text (prefix) |
|------|-----:|---------------|
| How to Perform the Salah | 9 | `facing forward, up to our shoulders and say Allahu Akbar…` |
| How to Perform the Salah | 10 | `and in each cycle or Rakah whilst standing (Qiyam)…` |
| Timings… | 13 | `sunrise and there are 2 compulsory Rakah…` |
| Timings… | 13 | `passes its zenith till the shadow…` |
| Timings… | 14 | `till just before sunset and it has 4 compulsory Rakah` |
| Timings… | 14 | `and it has 3 compulsory Rakah` |
| Timings… | 14 | `although offering up till midnight is preferred…` |

### List wrap pattern (LIST + orphan PARAGRAPH)

Example (*Timings*):

1. `LIST` `1) Morning or Fajr prayer can be performed between dawn and`  
2. `PARAGRAPH` `sunrise and there are 2 compulsory Rakah in this Salah`  

Same pattern for steps 2/4/5 and for *How to Perform* steps 1–2. Numbered steps are **not** fully reconstructed into single list items across line wraps.

### Other continuity defects

- *A Loving Relationship* still has `Himself in the Quran as the Most Loving…` as its own paragraph after a cut prior sentence.  
- *How to Perform*: Fatiha translation still split across blocks (`…Praise is only for` / `Compassionate, the Most Merciful…`).  
- *Sujood-Prostration* ends with source bridge “this is called Tashahud” (fidelity OK; slightly soft unit end before heading).

**Page-boundary continuity:** several merges succeed across pages, but the failures above dominate instructional reading quality.

---

## 7. Excluded elements (95) — PASS

| Bucket | Count | Assessment |
|--------|------:|------------|
| `zone:Appendix` (+ contact) | 46 | p.19–20 promo/catalog — correct |
| `element-type:TocEntry` | 19 | Real CONTENTS page lines — correct |
| `element-type:PageNumber` | 16 | Printed numbers on p.4–18 — correct |
| `zone:PublisherInfo` (+ meta/contact) | 10 | p.2 imprint — correct |
| `zone:TitlePage` | 4 | p.1 cover lines — correct |

**Reading-zone (p.4–18) exclusions:** only page numbers. No body teaching paragraphs from main content were found in the excluded list.

**Spot-check:** “OTHER ESSENTIAL READINGS”, “OTHER FREE LITERATURE”, `CONTENTS`, publisher email/URL, edition lines — excluded and **absent** from unit/chunk text.

---

## 8. Source fidelity — PASS

Method: normalize alphanumeric tokens from extracted pages 4–18 vs concatenated unit block text.

| Check | Result |
|-------|--------|
| Invented body text | **None observed** |
| Substantive source line prefixes missing from units | **0 / 266** sampled |
| Duplicate invented paragraphs | **None** |
| Reordered units vs book flow | **No** — order follows booklet |
| OCR quirks preserved | Yes (`pbut`, `?` bullets, `24434!` in Timings source line) |
| Back-matter / imprint in units | **No** |

Canonical reading text is a reconstructed view of the extract, not a hallucination. Remaining gaps are **segmentation**, not missing/invented content.

---

## 9. ContentBlock classifications (113) — WARNING

| BlockType | Count | Share |
|-----------|------:|------:|
| HEADING | 15 | 13.3% |
| PARAGRAPH | 90 | 79.6% |
| LIST | 8 | 7.1% |
| TRANSLITERATION / QURAN_* / HADITH_* / INSTRUCTION / REFERENCE | 0 | 0% |

**Observations**

- Every unit has exactly one leading `HEADING` — good.  
- Numbered salah steps and prayer timings correctly use `LIST` when the line starts with `N)`. Continuations after wraps are mis-typed as `PARAGRAPH`.  
- Latin dua / Fatiha / Ikhlas / thana lines remain `PARAGRAPH`/`en` (conservative; not false transliteration). Acceptable caution, but weak for client typography.  
- Quran English quotations and hadith English remain `PARAGRAPH`.  
- Three blocks `FLAGGEDLOWCONFIDENCE` (see §11). All others `UNVERIFIED`.

Not a fidelity failure; typing is too coarse for production reading UX.

---

## 10. SearchChunks (23) and canonical IDs — WARNING

| Check | Result |
|-------|--------|
| Chunk count | 23 |
| Orphan chunks | 0 |
| Chunks without embedding vector | 0 |
| TOC/catalog text in chunks | **Absent** |
| Metadata linkage | `chapterId`, `pageStart`, `pageEnd`, `unitTitle`, **`blockIndexes`** (unit-local order indices) |

**Canonical IDs:** chunks do **not** store ContentBlock GUIDs. Provenance is via `TaleemUnitId` + `blockIndexes` in `MetadataJson`. That is usable but not a stable canonical block ID graph.

**Chunk quality issues**

- Overlap prefixing yields mid-word chunk starts, e.g. `zeena an amta…`, `us Allaahumma…`, `s in the Qur…`, `rciful Lord…`.  
- Chunks correctly exclude appendix/TOC.  
- Distribution: most units have 1 chunk; longer units (Loving, How to Perform, Tashahud, Sitting Down, Cycles, Preparation) have 2–3.

Keyword presence in chunk text for core terms (Salah, Ruku, Sujood, Tashahud, Qiyam, Wudhu) — **present**. Public keyword search remains gated until publish (not tested as public).

---

## 11. Low-confidence items (exactly 3) — PASS (signals meaningful)

Report `lowConfidenceItemCount = 3` = three ContentBlocks with `VerificationStatus=FLAGGEDLOWCONFIDENCE` (rule: `PARAGRAPH`, length &lt; 50, not ending with `.`, not evidence `line-wrap-merge`). No units with `StructureConfidence &lt; 0.55`.

| # | Unit | Page | Text | Why flagged |
|---|------|-----:|------|-------------|
| 1 | Salah | 4 | `Islamic prayer` | Short subtitle/orphan line from title wrap; should join with heading `Salah` / TOC “Salah, Islamic prayer” |
| 2 | Tashahud | 13 | `This completes our Salah!` | Short paragraph &lt; 50 chars ending with `!` (not `.`); heuristic flags it even though it is a legitimate closing sentence |
| 3 | Timings… | 14 | `and it has 3 compulsory Rakah` | Mid-sentence list-wrap orphan (continuation of Magrib LIST line); correctly suspicious |

**Note:** Item 2 is a **false-ish positive** of the short-fragment heuristic (exclamation not period). Items 1 and 3 are real reconstruction defects.

---

## 12. TOC / front matter not in reading or search — PASS

| Probe | In units? | In chunks? |
|-------|-----------|------------|
| `CONTENTS` | No | No |
| `OTHER ESSENTIAL` | No | No |
| `OTHER FREE LITERATURE` | No | No |
| `learnislam4free` / `askme@` | No | No |
| `First Print` / `Dr Irfan` | No | No |
| Exact TOC row strings as non-heading body | No | No |

TOC titles appear only as **chapter/unit headings** when they also exist as body headings — correct dual use of TOC as evidence, not as reading rows from page 3.

---

## 13. Embeddings / semantic quality — WARNING

`appsettings.Development.json` → `"Embedding": { "Provider": "fake" }`.  
`FakeEmbeddingService` produces deterministic SHA256-derived vectors. All 23 chunks have embeddings stored.

**Do not treat any semantic-search ranking, similarity, or “embedding quality” metric from this environment as evidence.** Only keyword/`SearchVector` text presence is meaningful until a real embedding provider is configured.

---

## 14. Database integrity — PASS

| Check | Result |
|-------|--------|
| Chapters / units / blocks / chunks | 15 / 15 / 113 / 23 |
| Orphan blocks / chunks | 0 / 0 |
| Block `OrderIndex` contiguous per unit | Yes (`0…n-1`) |
| Unit↔chapter ownership | Intact |
| StructureConfidence column populated | Yes |

---

## 15. Large-book / lifecycle notes — WARNING (process, not this result)

- Reprocess of **published** books is blocked in pipeline (protects published structure) — good.  
- This book is `NEEDS_REVIEW`; reprocess still replaces draft structure (expected).  
- Whole-document layout extract remains in-memory — still a scale risk for 500–1000 page books (unchanged architectural note).

---

## Remaining defects (do not fix in this audit)

1. **List/paragraph wrap reconstruction** still leaves mid-sentence blocks in *How to Perform* and *Timings* (primary publish blocker).  
2. Residual prose wrap in *A Loving Relationship* (`Himself in the Quran…`) and Fatiha translation split.  
3. Opener title should be `Salah, Islamic prayer` (or merged heading), not `Salah` + orphan `Islamic prayer`.  
4. Short-fragment flag treats `This completes our Salah!` as low confidence.  
5. Block typing does not label transliteration / Quran translation / instruction.  
6. SearchChunk overlap produces mid-word prefixes; metadata lacks ContentBlock GUIDs.  
7. Fake embeddings only — no semantic quality claims.  
8. Chapter page-end metadata overlaps next chapter start page.

---

## Final recommendation

**NOT READY FOR PUBLISH TEST**

Safe next step after this audit (when approved): tighten wrap/list reconstruction and opener-title merge on a new processor version, reprocess the same SourceDocument, re-audit continuity on *How to Perform* + *Timings* + *Loving Relationship*, then consider a controlled publish test. Do not use fake-embedding similarity as a gate.
