"use client";

import { FormEvent, useState } from "react";
import { useParams } from "next/navigation";
import { ApiError, publicApi } from "@/lib/api";
import { highlightMatches } from "@/lib/highlight";

export default function SearchPage() {
  const params = useParams<{ slug: string }>();
  const [q, setQ] = useState("");
  const [results, setResults] = useState<Awaited<ReturnType<typeof publicApi.search>> | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);
  const [submitted, setSubmitted] = useState("");

  async function onSubmit(e: FormEvent) {
    e.preventDefault();
    setError(null);
    setLoading(true);
    const query = q.trim();
    try {
      const book = await publicApi.book(params.slug);
      setResults(await publicApi.search(book.id, query));
      setSubmitted(query);
    } catch (err) {
      if (err instanceof ApiError && err.status === 404) {
        setError("Book not found or not published.");
      } else {
        setError(err instanceof Error ? err.message : "Search failed");
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
      <h1 className="text-3xl font-semibold">Search</h1>
      <form onSubmit={onSubmit} className="flex flex-col gap-2 sm:flex-row font-sans">
        <label className="sr-only" htmlFor="keyword-search">
          Search keywords
        </label>
        <input
          id="keyword-search"
          className="input"
          value={q}
          onChange={(e) => setQ(e.target.value)}
          placeholder="e.g. prayer, bowing, prostration"
          autoComplete="off"
        />
        <button className="btn shrink-0" type="submit" disabled={loading}>
          {loading ? "Searching…" : "Search"}
        </button>
      </form>
      {error && <p className="text-red-700 font-sans text-sm">{error}</p>}
      <div className="space-y-3">
        {results && results.length === 0 && (
          <p className="font-sans text-[var(--muted)]">No results for “{submitted}”.</p>
        )}
        {results?.map((r) => {
          const sameTitle =
            r.unitTitle.trim().localeCompare(r.chapterTitle.trim(), undefined, {
              sensitivity: "accent",
            }) === 0;
          let excerpt = r.excerpt.trim();
          if (excerpt.toLowerCase().startsWith(r.unitTitle.trim().toLowerCase())) {
            excerpt = excerpt.slice(r.unitTitle.trim().length).replace(/^[\s:.\-–—]+/, "").trim();
          }
          return (
            <a key={r.unitId + r.excerpt.slice(0, 12)} href={`/read/${r.unitId}`} className="card-link">
              <div className="font-semibold">{r.unitTitle}</div>
              {!sameTitle && (
                <div className="text-sm text-[var(--muted)] font-sans">{r.chapterTitle}</div>
              )}
              {excerpt && (
                <p className="mt-2 text-[var(--muted)]">{highlightMatches(excerpt, submitted)}</p>
              )}
            </a>
          );
        })}
      </div>
    </div>
  );
}
