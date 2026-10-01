"use client";

import { FormEvent, useState } from "react";
import { useParams } from "next/navigation";
import { ApiError, publicApi } from "@/lib/api";

export default function SemanticSearchPage() {
  const params = useParams<{ slug: string }>();
  const [query, setQuery] = useState("");
  const [results, setResults] = useState<Awaited<ReturnType<typeof publicApi.semantic>> | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);

  async function onSubmit(e: FormEvent) {
    e.preventDefault();
    setError(null);
    setLoading(true);
    try {
      const book = await publicApi.book(params.slug);
      setResults(await publicApi.semantic(book.id, query.trim()));
    } catch (err) {
      if (err instanceof ApiError && err.status === 404) {
        setError("Book not found or not published.");
      } else {
        setError(err instanceof Error ? err.message : "Semantic search failed");
      }
      setResults(null);
    } finally {
      setLoading(false);
    }
  }

  return (
    <div className="space-y-6">
      <a href={`/books/${params.slug}`} className="font-sans text-sm text-[var(--accent)]">
        ← Book home
      </a>
      <h1 className="text-3xl font-semibold">Semantic search</h1>
      <p className="font-sans text-[var(--muted)]">
        Retrieval only — results resolve to canonical ContentBlocks, never generated answers.
      </p>
      <form onSubmit={onSubmit} className="space-y-3 font-sans">
        <label className="sr-only" htmlFor="semantic-query">
          Semantic search query
        </label>
        <textarea
          id="semantic-query"
          className="input min-h-28"
          value={query}
          onChange={(e) => setQuery(e.target.value)}
          placeholder="Ask in natural language…"
        />
        <button className="btn" type="submit" disabled={loading}>
          {loading ? "Retrieving…" : "Retrieve"}
        </button>
      </form>
      {error && <p className="text-red-700 font-sans text-sm">{error}</p>}
      <div className="space-y-3">
        {results && results.length === 0 && (
          <p className="font-sans text-[var(--muted)]">No semantic matches for this query.</p>
        )}
        {results?.map((r) => {
          const sameTitle =
            r.unitTitle.trim().localeCompare(r.chapterTitle.trim(), undefined, {
              sensitivity: "accent",
            }) === 0;
          return (
            <a key={r.unitId + r.similarity} href={`/read/${r.unitId}`} className="card-link">
              <div className="font-semibold">{r.unitTitle}</div>
              <div className="text-sm text-[var(--muted)] font-sans">
                {sameTitle ? "" : `${r.chapterTitle} · `}
                similarity {(r.similarity * 100).toFixed(1)}%
              </div>
              <p className="mt-2 text-[var(--muted)]">{r.chunkExcerpt}</p>
            </a>
          );
        })}
      </div>
    </div>
  );
}
