# Layout-aware ingestion IR (processor 2.2.0)

Books are data. Extraction must not depend on any specific book vocabulary.

## Pipeline stages

```
PDF
 → PdfPig words/letters + geometry
 → DocumentPage / DocumentElement IR
 → Document zone classification (COVER, TOC, MAIN_CONTENT, …)
 → furniture + front-matter (IncludeInReading=false)
 → TOC detection (evidence only — never reading units)
 → LogicalTextReconstructor (lines → paragraphs / list items / multi-line titles)
 → HeadingCandidate detection (≠ TaleemUnit)
 → hierarchy (Section / Chapter as warranted)
 → TaleemUnits = heading + associated body until next structural boundary
 → ContentBlocks (stable GUIDs) → SearchChunks (+ contentBlockIds) + embeddings
 → quality report → NEEDS_REVIEW
```

## Chapter page ranges

`SourcePageStart` / `SourcePageEnd` are **inclusive provenance spans**, not exclusive page ownership.
Two chapters may share a physical page when a new heading begins mid-page; the prior chapter’s
end page may equal the next chapter’s start page.

## Concept separation

| Concept | Role |
|---------|------|
| DocumentElement | Physical extracted span |
| HeadingCandidate | Possible structural heading |
| Section / Chapter | Document hierarchy |
| TaleemUnit | Meaningful readable group of ContentBlocks |
| ContentBlock | Canonical content piece |
| SearchChunk | Retrieval representation |

## Rules

- Never start from flattened `page.Text` as the structural source.
- Page numbers / repeated headers must not become titles.
- TOC / publisher / copyright / contact lines are provenance, not reader units.
- Ordinary English prose must not be labeled `TRANSLITERATION`.
- Arabic/Urdu Unicode is stored logically; clients render direction.
- Quality metrics flag statistical anomalies (not a fixed units-per-page cap).
