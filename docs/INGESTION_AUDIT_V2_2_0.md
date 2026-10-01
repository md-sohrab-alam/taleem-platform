# Ingestion Quality Audit — Processor v2.2.0

**Book:** Salah: Islamic Prayer (`ac0f5a18-5d34-43b2-bcd6-15fe43df9bb9`)  
**SourceDocument:** `ea0d4071-474f-4820-bed0-1fc00b067c50` (20 pages, same upload — no re-upload)  
**Job:** SUCCEEDED · `ProcessorVersion=2.2.0` · status `NEEDS_REVIEW`  
**Audit date:** 2026-10-01  
**Method:** Admin API review + PostgreSQL + source-page comparison after reprocess. No approve/publish. No manual content edits.

---

## Overall status: **READY FOR PUBLISH TEST**

v2.2.0 fixes the v2.1.1 publish blockers: logical list/paragraph reconstruction, opener title merge, low-confidence heuristic noise, mid-word search-chunk overlap, and stable ContentBlock GUID provenance on chunks. Source fidelity remains intact. Architecture unchanged.

Residual notes (not publish blockers for this POC): conservative `PARAGRAPH` typing for transliteration/quotes; fake embeddings still configured in Development; a few intentional quote/transliteration lines remain separate blocks.

---

## Comparison: 2.1.1 → 2.2.0

| Metric | v2.1.1 | v2.2.0 |
|--------|------:|------:|
| Chapters | 15 | **15** |
| TaleemUnits | 15 | **15** |
| ContentBlocks | 113 | **99** |
| SearchChunks | 23 | 23 |
| TOC entries | 15 | 15 |
| Excluded elements | 95 | 95 |
| Low-confidence items | 3 | **0** |
| Structure confidence avg | 0.969 | **0.974** |
| Lowercase orphan body blocks | 7+ | **0** |
| Mid-word chunk starts (audit) | Yes | **No** |
| Chunk `contentBlockIds` | No | **Yes** |
| Opener title | `Salah` + orphan `Islamic prayer` | **`Salah, Islamic prayer`** |

---

## Summary table

| Area | Verdict | Notes |
|------|---------|--------|
| List reconstruction (Timings / How to Perform) | PASS | Numbered items are complete logical LIST blocks |
| Paragraph reconstruction (Loving / Fatiha) | PASS | Former wrap orphans merged |
| Opener title | PASS | Multi-line title via TOC + typography |
| Low confidence | PASS | 0 items; `!`/`?` no longer false-flag |
| SearchChunk mid-word | PASS | Overlap on Unicode-safe boundaries |
| SearchChunk provenance | PASS | `contentBlockIds[]` + indexes + processorVersion |
| Source fidelity | PASS | 266/266 sampled prefixes; no invent/lose/reorder |
| Exclusions / zones / TOC | PASS | Unchanged good behavior from 2.1.1 |
| Hierarchy (15 TOC topics) | PASS | All 15 chapters present; Prophets ≠ Timings |
| Religious block typing | WARNING | Still mostly PARAGRAPH (explicitly deferred) |
| Embeddings | WARNING | Fake provider — not semantic evidence |
| DB integrity | PASS | 15/15/99/23; no orphans |

---

## Automated checklist (A–H)

### A) How to Perform the Salah — PASS
Numbered steps 1–2 and 4 are single LIST blocks including former wrap tails (`facing forward…`, `and in each cycle…`). No lowercase orphan continuations.

### B) Timings and Rakah in Each Salah — PASS
Five complete LIST items (Fajr through Isha), including cross-line and cross-page continuations. No `sunrise…` / `and it has 3…` orphan paragraphs.

### C) A Loving Relationship — PASS
`God describes Himself in the Quran…` is inside the opening paragraph (not an orphan block).

### D) Fatiha translation — PASS
`"Praise is only for Allah, Lord of the Universe; The most Compassionate…"` is one paragraph (former wrap split gone).

### E) Opening title — PASS
Chapter/unit heading: **`Salah, Islamic prayer`**. Former low-confidence `Islamic prayer` block gone.

### F) Low confidence — PASS
**0** flagged items. Prior `This completes our Salah!` false positive resolved via Unicode sentence punctuation (`.!?۔؟…`).

### G) SearchChunks mid-word — PASS
Overlap uses whitespace/letter-boundary scanning. Former `zeena` / `rciful` / `us Allaahumma` mid-cuts not observed. ChunkIndex>0 starts are whole tokens (`to start…`, `they learn…`, `witness that…`).

### H) SearchChunk provenance — PASS
All 23 chunks have `contentBlockIds` (non-empty), plus `bookId`, `chapterId`, `taleemUnitId`, `blockIndexes`, `pageStart`/`pageEnd`, `languages`/`scripts`, `processorVersion=2.2.0`. ContentBlock rows persist the same GUIDs assigned at analysis time.

---

## Canonical unit samples (2.2.0)

### A Loving Relationship (8 blocks, p.5–6)
1. HEADING `A Loving Relationship`  
2. PARAGRAPH includes `…God describes Himself in the Quran as the Most Loving…`  
3–8. Hadith quotes and closing prose (separate quote lines preserved)

### How to Perform the Salah (14 blocks, p.9–11)
- LIST 1–2: full multi-line steps  
- LIST 3: instruction; following transliteration/translation as PARAGRAPH (not wrap orphans)  
- LIST 4: Fatiha instruction including former wrap  
- Fatiha translation: single PARAGRAPH

### Ruku (7 blocks, p.11 only)
Instructional body only; no Sujood-exclusive content.

### Sujood-Prostration (5 blocks, p.11–12)
Starts after Ruku heading on shared page; no Ruku-only bowing formula.

### Timings and Rakah in Each Salah (9 blocks, p.13–14)
LIST 1–5 complete; trailing commentary paragraphs; does **not** absorb Prophets’ Prayer.

---

## Source fidelity — PASS

| Check | Result |
|-------|--------|
| Invented body text | None |
| Missing substantive prefixes (p.4–18) | 0 / 266 |
| Duplicated invented paragraphs | None |
| Book flow reorder | None |
| Back-matter in units | None |
| Merge = join physical lines only | Yes |

---

## Remaining non-blockers

1. Transliteration / Quran English still typed as `PARAGRAPH` (by design this iteration).  
2. Development embeddings remain `fake` — do not claim semantic-search quality.  
3. Chapter page-end may equal next chapter start on shared pages — documented inclusive provenance (`docs/INGESTION_IR.md`).  
4. Occasional short quote-intro lines (`Prophet … used to say`) remain separate when hard-ended or quote-bounded — readable, not wrap orphans.

---

## Tests

35/35 unit tests passing, including new `LogicalReconstructionTests` for list/paragraph/title/chunk GUID/shared-page semantics.

---

## Final recommendation

**READY FOR PUBLISH TEST**

Proceed to controlled publish → public API → Web → Android validation. Do not further tune this PDF’s ingestion for POC; treat remaining typing/embedding work as separate tracks.
