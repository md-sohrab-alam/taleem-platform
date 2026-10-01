"use client";

import { useEffect, useMemo, useState } from "react";
import { useParams, useSearchParams } from "next/navigation";
import { adminApi, IngestionJob, ReviewBook } from "@/lib/api";

type NavView = "hierarchy" | "debug";

export default function BookDetailPage() {
  const params = useParams<{ id: string }>();
  const search = useSearchParams();
  const bookId = params.id;
  const [review, setReview] = useState<ReviewBook | null>(null);
  const [job, setJob] = useState<IngestionJob | null>(null);
  const [selectedUnitId, setSelectedUnitId] = useState<string | null>(null);
  const [pageText, setPageText] = useState<string>("");
  const [confirmSlug, setConfirmSlug] = useState("");
  const [message, setMessage] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [navView, setNavView] = useState<NavView>("hierarchy");
  const [expandedChapters, setExpandedChapters] = useState<Record<string, boolean>>({});

  async function refresh() {
    const data = await adminApi.review(bookId);
    setReview(data);
    if (!selectedUnitId && data.units[0]) setSelectedUnitId(data.units[0].id);
    const expand: Record<string, boolean> = {};
    data.chapters.slice(0, 8).forEach((c) => {
      expand[c.id] = true;
    });
    setExpandedChapters((prev) => (Object.keys(prev).length ? prev : expand));
  }

  useEffect(() => {
    refresh().catch((e) => setError(e.message));
    const jobId = search.get("jobId");
    if (!jobId) return;
    const timer = setInterval(async () => {
      try {
        const j = await adminApi.getJob(jobId);
        setJob(j);
        if (j.status === "SUCCEEDED" || j.status === "FAILED") {
          clearInterval(timer);
          await refresh();
        }
      } catch {
        /* ignore poll errors */
      }
    }, 2000);
    return () => clearInterval(timer);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [bookId]);

  const selectedUnit = useMemo(
    () => review?.units.find((u) => u.id === selectedUnitId) ?? null,
    [review, selectedUnitId]
  );

  const unitsByChapter = useMemo(() => {
    const map = new Map<string, typeof review extends ReviewBook ? ReviewBook["units"] : never>();
    if (!review) return map;
    for (const u of review.units) {
      const list = map.get(u.chapterId) ?? [];
      list.push(u);
      map.set(u.chapterId, list);
    }
    return map;
  }, [review]);

  async function loadPageContext(page?: number) {
    if (!review?.sources[0] || !page) return;
    const pages = await adminApi.pages(review.sources[0].id);
    const match = pages.find((p) => p.pageNumber === page);
    setPageText(match?.normalizedText || match?.rawText || "No extracted text for this page.");
  }

  useEffect(() => {
    if (selectedUnit?.sourcePageStart) {
      loadPageContext(selectedUnit.sourcePageStart).catch(() => undefined);
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [selectedUnitId]);

  async function approve() {
    try {
      await adminApi.approve(bookId);
      setMessage("Book approved.");
      await refresh();
    } catch (e) {
      setError(e instanceof Error ? e.message : "Approve failed");
    }
  }

  async function publish() {
    try {
      await adminApi.publish(bookId);
      setMessage("Book published. Clients will see it via GET /api/v1/books.");
      await refresh();
    } catch (e) {
      setError(e instanceof Error ? e.message : "Publish failed");
    }
  }

  async function reprocess() {
    if (!review?.sources[0]) return;
    try {
      setMessage("Reprocessing with latest processor…");
      const j = await adminApi.startIngest(bookId, review.sources[0].id);
      setJob(j);
      window.history.replaceState(null, "", `/books/${bookId}?jobId=${j.id}`);
      const timer = setInterval(async () => {
        const latest = await adminApi.getJob(j.id);
        setJob(latest);
        if (latest.status === "SUCCEEDED" || latest.status === "FAILED") {
          clearInterval(timer);
          await refresh();
          setMessage(
            latest.status === "SUCCEEDED"
              ? `Reprocess complete (processor ${latest.processorVersion ?? "?"}).`
              : `Reprocess failed: ${latest.error ?? "unknown"}`
          );
        }
      }, 2000);
    } catch (e) {
      setError(e instanceof Error ? e.message : "Reprocess failed");
    }
  }

  async function remove() {
    try {
      await adminApi.deleteBook(bookId, confirmSlug);
      window.location.href = "/books";
    } catch (e) {
      setError(e instanceof Error ? e.message : "Delete failed");
    }
  }

  async function saveBlock(blockId: string, text: string, blockType: string) {
    if (!selectedUnit) return;
    await adminApi.updateBlock(blockId, {
      blockType,
      language: "en",
      text,
      orderIndex: selectedUnit.blocks.find((b) => b.id === blockId)?.orderIndex ?? 0,
      sourcePage: selectedUnit.sourcePageStart,
      sourceReference: `p.${selectedUnit.sourcePageStart}`,
    });
    setMessage("Block saved.");
    await refresh();
  }

  if (!review) {
    return <div className="panel p-6">{error ?? "Loading…"}</div>;
  }

  const report = review.ingestionReport;
  const prevJob = review.jobs.find((j) => j.processorVersion && j.processorVersion !== report?.processorVersion);
  const sections = review.sections.length ? review.sections : [{ id: "main", title: "Main Content", orderIndex: 0 }];

  return (
    <div className="space-y-6">
      <div className="flex flex-wrap items-start justify-between gap-4">
        <div>
          <h1 className="text-3xl font-semibold">{review.book.title}</h1>
          <p className="text-[var(--muted)]">
            {review.book.slug} · {review.book.status} · {review.book.chapterCount} chapters ·{" "}
            {review.book.unitCount} units
          </p>
        </div>
        <div className="flex flex-wrap gap-2">
          <button className="btn btn-secondary" onClick={reprocess}>
            Reprocess source
          </button>
          <button className="btn btn-secondary" onClick={approve}>
            Approve
          </button>
          <button className="btn btn-primary" onClick={publish}>
            Publish
          </button>
        </div>
      </div>

      {job && (
        <div className="panel p-4">
          <div className="font-semibold">
            Ingestion: {job.status}
            {job.processorVersion ? ` · v${job.processorVersion}` : ""}
          </div>
          <div className="text-sm text-[var(--muted)] mt-1">
            {job.progressPercent}% — {job.progressMessage}
          </div>
          {job.error && <div className="text-[var(--danger)] mt-2">{job.error}</div>}
          <div className="mt-3 h-2 rounded bg-[var(--accent-soft)] overflow-hidden">
            <div className="h-full bg-[var(--accent)]" style={{ width: `${job.progressPercent}%` }} />
          </div>
        </div>
      )}

      {report && (
        <div className="panel p-4 space-y-3">
          <h2 className="font-semibold">Ingestion report · processor {report.processorVersion}</h2>
          {prevJob && (
            <p className="text-sm text-[var(--muted)]">
              Previous job processor: {prevJob.processorVersion ?? "1.x"} — compare unit counts below after
              reprocess.
            </p>
          )}
          <div className="grid sm:grid-cols-2 lg:grid-cols-4 gap-3 text-sm">
            <Metric label="Pages" value={report.pageCount} />
            <Metric label="Sections" value={report.sectionCount} />
            <Metric label="Chapters" value={report.chapterCount} />
            <Metric label="TaleemUnits" value={report.unitCount} />
            <Metric label="ContentBlocks" value={report.contentBlockCount} />
            <Metric label="SearchChunks" value={report.searchChunkCount} />
            <Metric label="TOC entries" value={report.tocEntryCount} />
            <Metric label="Excluded front matter" value={report.excludedFrontMatterCount} />
            <Metric label="Heading candidates" value={report.headingCandidateCount} />
            <Metric label="Low confidence" value={report.lowConfidenceItemCount} />
            <Metric label="Heading-only units" value={report.headingOnlyUnitCount} />
            <Metric label="Units / page (avg)" value={report.unitsPerPageAverage} />
          </div>
          {report.hasStructuralAnomalies && (
            <div className="text-sm text-[var(--danger)]">
              Anomalies: {report.anomalyFlags.join(", ") || "flagged"}
            </div>
          )}
          <details className="text-sm">
            <summary className="cursor-pointer font-medium">Document zones</summary>
            <ul className="mt-2 space-y-1 text-[var(--muted)]">
              {report.documentZones.map((z) => (
                <li key={z.pageNumber}>
                  p.{z.pageNumber}: {z.zoneRole} ({Math.round(z.confidence * 100)}%)
                </li>
              ))}
            </ul>
          </details>
          <details className="text-sm">
            <summary className="cursor-pointer font-medium">TOC entries ({report.tocEntries.length})</summary>
            <ul className="mt-2 space-y-1 text-[var(--muted)] max-h-40 overflow-auto">
              {report.tocEntries.map((t, i) => (
                <li key={`${t.title}-${i}`}>
                  {t.title}
                  {t.pageNumber != null ? ` → p.${t.pageNumber}` : ""}
                </li>
              ))}
            </ul>
          </details>
          <details className="text-sm">
            <summary className="cursor-pointer font-medium">
              Excluded elements ({report.excludedFrontMatterCount})
            </summary>
            <ul className="mt-2 space-y-1 text-[var(--muted)] max-h-40 overflow-auto">
              {report.excludedFrontMatter.slice(0, 80).map((e, i) => (
                <li key={i}>
                  p.{e.pageNumber} [{e.reason}] {e.text}
                </li>
              ))}
            </ul>
          </details>
        </div>
      )}

      {message && <div className="panel p-3 text-[var(--accent)]">{message}</div>}
      {error && <div className="panel p-3 text-[var(--danger)]">{error}</div>}

      <div className="grid lg:grid-cols-3 gap-4">
        <div className="panel p-4 lg:col-span-1 max-h-[70vh] overflow-auto">
          <div className="flex items-center justify-between mb-3 gap-2">
            <h2 className="font-semibold">
              {navView === "hierarchy" ? "Hierarchy" : "Debug source"}
            </h2>
            <div className="flex gap-1 text-xs">
              <button
                className={`px-2 py-1 rounded ${navView === "hierarchy" ? "bg-[var(--accent-soft)]" : ""}`}
                onClick={() => setNavView("hierarchy")}
              >
                Structure
              </button>
              <button
                className={`px-2 py-1 rounded ${navView === "debug" ? "bg-[var(--accent-soft)]" : ""}`}
                onClick={() => setNavView("debug")}
              >
                Debug
              </button>
            </div>
          </div>

          {navView === "hierarchy" &&
            sections.map((section) => {
              const sectionChapters = review.chapters.filter(
                (c) => !c.sectionId || c.sectionId === section.id || sections.length === 1
              );
              return (
                <div key={section.id} className="mb-4">
                  <div className="text-xs uppercase tracking-wide text-[var(--muted)] mb-2">
                    Section · {section.title}
                  </div>
                  <ul className="space-y-2">
                    {(sectionChapters.length ? sectionChapters : review.chapters).map((ch) => {
                      const units = unitsByChapter.get(ch.id) ?? [];
                      const open = expandedChapters[ch.id] ?? false;
                      return (
                        <li key={ch.id}>
                          <button
                            className="w-full text-left rounded-lg px-2 py-1.5 hover:bg-[var(--bg)]"
                            onClick={() =>
                              setExpandedChapters((s) => ({ ...s, [ch.id]: !open }))
                            }
                          >
                            <div className="font-medium text-sm">
                              {open ? "▾" : "▸"} {ch.title}
                            </div>
                            <div className="text-xs text-[var(--muted)]">
                              {units.length} units
                              {ch.sourcePageStart != null
                                ? ` · p.${ch.sourcePageStart}-${ch.sourcePageEnd}`
                                : ""}
                            </div>
                          </button>
                          {open && (
                            <ul className="ml-3 mt-1 space-y-1 border-l border-[var(--border)] pl-2">
                              {units.map((u) => (
                                <li key={u.id}>
                                  <button
                                    className={`w-full text-left rounded-lg px-2 py-1.5 text-sm ${
                                      selectedUnitId === u.id
                                        ? "bg-[var(--accent-soft)]"
                                        : "hover:bg-[var(--bg)]"
                                    }`}
                                    onClick={() => setSelectedUnitId(u.id)}
                                  >
                                    {u.title}
                                  </button>
                                </li>
                              ))}
                            </ul>
                          )}
                        </li>
                      );
                    })}
                  </ul>
                </div>
              );
            })}

          {navView === "debug" && (
            <div className="space-y-4 text-sm">
              <div>
                <div className="font-medium mb-1">Source pages</div>
                <p className="text-[var(--muted)]">
                  Select a unit to load page text on the right. Zones/TOC/excluded lists are in the report
                  above.
                </p>
              </div>
              {report?.suspiciousUnitTitles?.length ? (
                <div>
                  <div className="font-medium mb-1">Suspicious / heading-only titles</div>
                  <ul className="text-[var(--muted)] space-y-1 max-h-48 overflow-auto">
                    {report.suspiciousUnitTitles.map((t) => (
                      <li key={t}>{t}</li>
                    ))}
                  </ul>
                </div>
              ) : null}
              <div>
                <div className="font-medium mb-1">Jobs</div>
                <ul className="text-[var(--muted)] space-y-1">
                  {review.jobs.map((j) => (
                    <li key={j.id}>
                      {j.status} · v{j.processorVersion ?? "?"} · {j.progressMessage}
                    </li>
                  ))}
                </ul>
              </div>
            </div>
          )}
        </div>

        <div className="panel p-4 lg:col-span-1 max-h-[70vh] overflow-auto">
          <h2 className="font-semibold mb-3">Original page context</h2>
          <pre className="whitespace-pre-wrap text-sm text-[var(--muted)]">
            {pageText || "Select a unit."}
          </pre>
        </div>

        <div className="panel p-4 lg:col-span-1 max-h-[70vh] overflow-auto">
          <h2 className="font-semibold mb-3">Structured content</h2>
          {!selectedUnit && <p className="text-sm text-[var(--muted)]">Select a unit.</p>}
          {selectedUnit && (
            <div className="space-y-4">
              <div className="text-sm text-[var(--muted)]">
                {selectedUnit.chapterTitle} · Topics:{" "}
                {selectedUnit.topics.map((t) => t.displayName).join(", ") || "—"}
              </div>
              {selectedUnit.blocks.map((b) => (
                <div key={b.id} className="border border-[var(--border)] rounded-lg p-3">
                  <div className="text-xs text-[var(--muted)] mb-2">
                    {b.blockType} · {b.verificationStatus} · p.{b.sourcePage}
                  </div>
                  <textarea
                    className="input min-h-28"
                    defaultValue={b.text}
                    onBlur={(e) => saveBlock(b.id, e.target.value, b.blockType)}
                  />
                </div>
              ))}
            </div>
          )}
        </div>
      </div>

      <div className="panel p-5 space-y-3">
        <h2 className="font-semibold text-[var(--danger)]">Delete book</h2>
        <p className="text-sm text-[var(--muted)]">
          Permanently removes source PDF, pages, structure, chunks, and embeddings. Shared topics used by
          other books are preserved. Type the book slug to confirm.
        </p>
        <input
          className="input max-w-md"
          placeholder={review.book.slug}
          value={confirmSlug}
          onChange={(e) => setConfirmSlug(e.target.value)}
        />
        <div>
          <button className="btn btn-danger" onClick={remove}>
            Delete permanently
          </button>
        </div>
      </div>
    </div>
  );
}

function Metric({ label, value }: { label: string; value: string | number }) {
  return (
    <div className="rounded-lg border border-[var(--border)] px-3 py-2">
      <div className="text-xs text-[var(--muted)]">{label}</div>
      <div className="font-semibold">{value}</div>
    </div>
  );
}
