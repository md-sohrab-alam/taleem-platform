# Public Web — Product UX Audit (reader lens)

**Site:** http://localhost:3001/  
**Content:** Published book *Salah: Islamic Prayer* (`salah-islamic-prayer-2`)  
**Role:** Ordinary reader — not developer / not architecture review  
**Date:** 2026-10-01  
**Out of scope:** Code changes, ingestion, Android, deploy, semantic-search relevance (FakeEmbeddingService)

---

## Executive summary

Core reading works: a visitor can open the book, find chapters, search “Ruku,” and open clean readings. The product story is weak for a first-time human reader. The homepage speaks like an engineering note; “Ta'leem” and timed sessions are not explained; book home piles Featured + option tiles + full chapters + topics without a clear “start here”; Continue Reading never appears after reading; chapter pages with a single identical “reading” add a useless click; 5/10-minute sessions feel like “first N minutes of the book” rather than a purposeful short lesson.

---

## Journey findings

### 1. First-time visitor

**What I see**
- Brand “Taleem” twice (header + H1).
- Subcopy: *“Published books from the content platform. Adding a new authorized book requires no client code changes.”*
- One book card: title, author, `EN`. No blurb about what the book teaches. No “Start reading” cue.

**Can I understand the website?** Partially — it looks like a library of books, but not why, for whom, or what “Ta'leem” means.

**Can I understand the book?** Only by the title. No description of contents or who it helps.

**Is it obvious where to start?** Mildly — only one book, so click it. After that, the book home does not say “start here.”

| Severity | High |
| User impact | Visitors bounce or feel this is an internal tool, not a reading product |
| Current behavior | Developer-facing library intro; no reader value prop or primary CTA |
| Recommended UX change | Replace intro with 1–2 reader sentences (what Taleem is + short learning). On the book card, show a short blurb and a clear affordance like “Open book” / “Start reading” |

---

### 2. “I want to learn how to perform Salah”

**Best path found**
1. Homepage → book card  
2. Book home → Featured “How to Perform the Salah”  
3. Reader opens  

**Clicks:** **2** (library → featured unit) if the user notices Featured.  
**Worse path:** Browse chapters → chapter “How to Perform the Salah” → same-titled reading card → reader = **3–4 clicks**, with a useless intermediate page.

**Confusing**
- Featured leads with “A Loving Relationship,” not the how-to unit.
- “Featured readings” does not feel curated for “learn Salah”; looks like a random short list.
- Chapter list duplicates Featured titles; “1 reading” under every chapter is noise.

| Severity | Medium |
| User impact | Goal is reachable but not guided; many users will scan the long chapter list instead |
| Current behavior | How-to exists in Featured (4th) and Chapters; no “Start learning Salah” path |
| Recommended UX change | Pin one primary CTA on book home (“Start: How to perform Salah”) above Featured; demote or relabel Featured as “Popular / Short picks” |

---

### 3. “I want to know about Ruku”

**Via navigation**
1. Book home → scroll Chapters → “Ruku”  
2. Chapter page (single card “Ruku”)  
3. Reader  

Or: Topics → Ruku → pick among 3 units (practical Ruku + prophetic commentaries).

**Via search**
1. Book home → Search  
2. Type “Ruku” → top result is **Ruku** with a helpful snippet → reader  

**Which is easier?** **Search** — fewer decisions, best result first, snippet confirms relevance. Navigation requires scrolling 15 chapters or understanding Topics, then often an extra chapter hop.

| Severity | Low (search path works) / Medium (nav friction) |
| User impact | Search succeeds; browse path costs extra click and cognitive load |
| Current behavior | Search surfaces Ruku well; chapter intermediate is redundant when unitCount=1 |
| Recommended UX change | When a chapter has one reading, open the reader directly (or skip the chapter list page). Keep Topics for exploratory browsing |

---

### 4. “I have only 5 minutes”

**Path:** Book home → “5-minute Ta'leem”  

**Result:**  
1. Salah, Islamic prayer (~2 min)  
2. A Loving Relationship (~3 min)  

**Does it make sense?** Times add up. Content is a gentle intro + relationship framing — **not** a compact “learn to pray in 5 minutes” session.

**Why these readings?** Copy says *“complete contiguous units”* — opaque to a normal reader. No plain explanation (“We start from the beginning of the book and stop near 5 minutes”).

**Feel like useful short Ta'leem?** Partially as “start of the book,” weakly as “short practical Ta'leem.”

| Severity | Medium |
| User impact | User with 5 minutes may finish without learning postures/how-to |
| Current behavior | Prefix of book by duration; jargon subtitle; no session framing or “Begin session” flow |
| Recommended UX change | Reader copy: “A short session from the start of this book (~5 min).” Optional second option later: “Practical how-to (~5 min)” if product wants that. Soften/remove “contiguous units.” Add a single “Start first reading” primary button |

---

### 5. “I have 10 minutes”

**Result (contiguous prefix):** Intro → Loving Relationship → Preparation → Facing Kabah/Qibla → Cycles of Postures/Rakah. Coherent as **early curriculum**. Still **stops before** “How to Perform the Salah,” so a reader hoping for the procedure in 10 minutes leaves unsatisfied.

| Severity | Medium |
| User impact | Continuity is good; goal-match for “learn the prayer” is weak |
| Current behavior | Same prefix algorithm; same jargon |
| Recommended UX change | Same framing as 5-minute; optionally show a one-line outline of what the session covers before starting |

---

### 6. “I want to continue reading”

**Steps tried:** Open “How to Perform the Salah” reader → return to book home.

**Result:** **No Continue Reading section.** Nothing resumes the unit just opened.

| Severity | High |
| User impact | Resume journey fails silently; users re-hunt their place in a long chapter list |
| Current behavior | Continue only if backend has reading progress for a user key; public Web never records progress when a unit is opened |
| Recommended UX change | Persist last-opened unit for this browser (simple local resume is enough for POC) and show “Continue reading” at the top of book home with that title |

---

### 7. Chapter → reader journey

**Model today:** Book home → Chapter page → Reading (often same title) → Reader.

For this book nearly every chapter has **exactly one** reading with the **same title**. Distinction Chapter vs Reading is invisible and unnecessary.

| Severity | Medium |
| User impact | Extra click and duplicate labels feel like a broken or unfinished UI |
| Current behavior | Intermediate chapter page with one card mirroring the chapter title |
| Recommended UX change | Deep-link chapters with a single unit straight to `/read/{unitId}`; keep chapter page only when multiple readings exist. Soften “1 reading” microcopy or drop it when count is 1 |

---

### 8. Reader experience

**Strengths**
- Calm serif body, comfortable line length (`max-w-3xl`), good spacing.
- Lists render as real numbered steps (e.g. Timings, How to Perform).
- Back links: Library / Book home / Chapter.

**Weaknesses**
- No previous/next reading controls.
- No progress (“3 of 15” or position in book).
- Chapter label above H1 often repeats the same title.
- Numbered list can **restart at 1** after an intervening paragraph (How to Perform) — feels broken as instructions.
- Mobile (~390): readable; header + back links usable; long how-to still needs scroll with no sticky “chapter” context.

| Severity | Medium (nav/progress); Low (title repeat); Medium (list restart) |
| User impact | Reading is pleasant but session-like movement through the book is weak |
| Current behavior | Isolated unit pages with return links only |
| Recommended UX change | Add Previous / Next at bottom (by book order). Optional thin progress text. Avoid repeating identical chapter+title. Fix list continuity so step numbering doesn’t reset mid-procedure (content/render polish) |

---

### 9. Search experience

| Query | User take |
|-------|-----------|
| Ruku | Best result first; snippet matches; click lands correctly. **Good.** |
| Sujood | Top = Sujood-Prostration (practical). Secondary prophetic chapters also appear — understandable. |
| Fajr | Single hit Timings… — clear why (snippet mentions Fajr). |
| Qibla | Single hit Facing Kabah, Qibla — clear. |
| prayer | Broad list; top not obviously “best”; user must skim. Acceptable for a wide term. |

**Understandable titles/snippets?** Mostly yes. Snippets sometimes repeat the title then body.  
**Why matched?** Usually visible in snippet; not highlighted.  
**Best result easy?** Yes for specific terms; weaker for “prayer.”

| Severity | Low |
| User impact | Search is the strongest findability path today |
| Current behavior | Keyword cards with title + excerpt; empty state exists |
| Recommended UX change | Bold/highlight match terms in snippets; for broad queries, prefer chapters that look like overviews in ordering copy (“Showing mentions of …”) |

*Semantic search not judged for relevance (fake embeddings). Product note: exposing “Semantic search” beside normal Search may confuse POC users — see Should Improve.*

---

### 10. Topics (report only — no changes)

| Topic | Useful? | Notes |
|-------|---------|--------|
| Kabah | Weak alone | Same single unit as Qibla |
| Prostration | Duplicate of Sujood | Overlaps heavily with Sujood (same two+ units) |
| Qibla | Yes | Clear intent |
| Qiyam | Yes | Standing posture |
| Rakah | Yes | Cycles / timings |
| Ruku | Yes | Strong |
| Salah | Broad | Mix of intro + how-to + preparation — okay as umbrella |
| Sujood | Yes | Prefer this Arabic term over “Prostration” if only one kept |
| Tashahud | Yes | Clear |

**Conceptual duplicates:** Prostration ↔ Sujood; Kabah ↔ Qibla (same destination unit). Alphabetical topics list does not teach prayer order.

| Severity | Medium |
| User impact | Choice paralysis and “why two same things?”; Topics feel less trustworthy |
| Current behavior | Nine auto-title tokens as equal chips |
| Recommended UX change | (Later, content/curation) Merge duplicates for display; optionally order topics in pedagogical sequence rather than A–Z. Do not block POC if chapter+search remain primary |

---

### 11. Book home hierarchy

**Observed order**
1. Title / author / language (no description)  
2. Featured readings (5)  
3. Reading options grid (chapters, topics, 5/10/15 Ta'leem, Search, **Semantic search**)  
4. Full Chapters list (15)  
5. Topics chips  

Continue Reading: absent (see journey 6).

**Overload / duplication**
- Featured titles largely reappear under Chapters.
- Browse chapters tile **and** full Chapters section.
- Browse topics tile **and** Topics chips.
- “Semantic search” is technical and unproven for this POC.

**Terminology**
- “Readings” vs “Chapters” vs “Topics” never explained.
- “Ta'leem” unexplained on the page that offers 5/10/15.

| Severity | Medium |
| User impact | Scroll fatigue; unclear primary action |
| Current behavior | Everything available at once |
| Recommended UX change | Collapse hierarchy for POC: (1) Continue if any (2) one Start CTA (3) Timed Ta'leem + Search (4) Chapters. Move Topics below fold or behind “Browse topics.” Hide or label Semantic until real embeddings |

---

### 12. Mobile UX (~390×844)

| Screen | Observation |
|--------|-------------|
| Homepage | Usable single card; developer copy still harms trust |
| Book home | Long scroll; Featured + tiles + 15 chapters + topics feels heavy on a phone |
| Reader | Typography readable; back links wrap okay; no next/prev hurts one-handed flow |
| Search | Input + button stack usable; results cards fine |
| 5-minute Ta'leem | Clear list; jargon line still present |

No major overflow/overlap observed in this pass. Main mobile issue is **information density on book home**, not broken layout.

| Severity | Medium |
| User impact | Phone users work harder to find “the” starting action |
| Current behavior | Same dense book home as desktop, narrower |
| Recommended UX change | On small screens, prioritize Start + Ta'leem + Search; put full chapter list behind “Browse chapters” |

---

### 13. Product identity

| Question | Answer for a first-time visitor |
|----------|----------------------------------|
| What does this platform do? | Unclear — “content platform” / code-change note |
| What does “Ta'leem” mean? | Not explained (Islamic learning / short lesson is never stated) |
| Why 5/10/15? | Buttons exist; benefit not explained until after click, and then only with technical subtitle |
| Books vs chapters vs topics vs readings | Books OK; other terms collide without explanation |

Ignore branding polish (per brief). Identity/messaging is the largest product gap.

| Severity | High |
| User impact | Mis-set expectations; POC looks unfinished to non-technical guests |
| Current behavior | Engineering messaging + unexplained product vocabulary |
| Recommended UX change | Short glossary-free microcopy: “Short Islamic learning sessions from trusted books.” Label timed options “~5 / ~10 / ~15 minute reading sessions.” One sentence under book title describing the Salah book |

---

### 14. Semantic search

Not evaluated for relevance (FakeEmbeddingService).  
Product UX only: the option sits next to normal Search with no warning that results may be meaningless.

| Severity | Medium |
| User impact | Trust risk if users try it and get odd ranking |
| Current behavior | Full “Semantic search” entry on book home |
| Recommended UX change | Hide for public POC, or label “Experimental” until real embeddings |

---

## Issue register (severity-ranked)

1. **High — Homepage / product copy is developer-facing**  
   Impact: Visitors don’t understand Taleem.  
   Current: “content platform… no client code changes.”  
   Change: Reader value prop + clear start.

2. **High — Continue Reading never appears after reading**  
   Impact: Resume journey fails.  
   Current: No progress written from Web.  
   Change: Local last-read resume on book home.

3. **High — Product vocabulary unexplained (Ta'leem, timed sessions, readings)**  
   Impact: Features look mysterious.  
   Current: Labels without plain-language help.  
   Change: One-line explanations near first use.

4. **Medium — Book home information overload / duplication**  
   Impact: No clear primary action.  
   Change: Simplify hierarchy; demote Topics/Semantic.

5. **Medium — Chapter page often a useless intermediate**  
   Impact: Extra click, duplicate titles.  
   Change: Skip when one reading.

6. **Medium — 5/10 Ta'leem feel like book prefix, not goal-based short lessons**  
   Impact: Time-boxed users miss how-to.  
   Change: Honest framing; optional practical path later.

7. **Medium — Reader lacks prev/next and progress**  
   Impact: Weak continuous reading.  
   Change: Adjacent unit links + light progress.

8. **Medium — Topics: Prostration/Sujood and Kabah/Qibla overlap**  
   Impact: Confusing dual doors.  
   Change: Curate/merge later (report only now).

9. **Medium — Semantic search exposed while fake**  
   Impact: Trust.  
   Change: Hide or mark experimental.

10. **Medium — Featured list doesn’t coach “start learning”**  
    Impact: Wrong first click (“A Loving Relationship”).  
    Change: Explicit Start CTA; rethink Featured order/purpose.

11. **Low — Search match highlighting / broad-query guidance**  
12. **Low — “1 reading” noise; chapter title repeated in reader**  
13. **Low — Instruction list numbering restart mid-unit**  

---

## MUST FIX BEFORE PUBLIC POC

1. Replace homepage (and book-home if needed) **developer copy** with a short reader-facing explanation of Taleem and an obvious start into the Salah book.  
2. Explain **what 5/10/15-minute Ta'leem means** in plain language (and remove “complete contiguous units” from reader-facing UI).  
3. Make **Continue reading** work for a normal browser session after opening a unit (even simple last-opened resume).

---

## SHOULD IMPROVE

1. Skip single-reading chapter intermediate pages.  
2. Add Previous/Next (and light progress) in the reader.  
3. Simplify book home hierarchy; hide or demote Semantic search for POC.  
4. Add a primary “Start: How to perform Salah” (or equivalent) CTA.  
5. Frame timed sessions honestly as “from the start of the book,” or offer a practical short path.  
6. Reduce topic duplicates (Prostration/Sujood, Kabah/Qibla) when curating.  
7. Mobile: put full chapter/topic lists behind browse links.  
8. Search: highlight match terms in snippets.

---

## CAN DEFER

1. Full visual branding / illustration / covers.  
2. Rich pedagogical topic taxonomy and ordered topic curricula.  
3. Semantic search quality (wait for real embeddings).  
4. Multi-book discovery, recommendations, accounts.  
5. Deep progress sync across devices / logged-in users.  
6. Extensive empty-state illustrations and onboarding tours.  
7. Perfecting Featured algorithm beyond a clear Start CTA.
