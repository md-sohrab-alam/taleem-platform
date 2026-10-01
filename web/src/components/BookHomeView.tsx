"use client";

import { useEffect, useState } from "react";
import { TaleemUnitSummary } from "@/lib/api";
import { formatMinutes, formatReadingCount } from "@/lib/format";
import { getBookProgress, LocalReadingProgress } from "@/lib/progress";

export type BookHomeClientModel = {
  book: {
    id: string;
    title: string;
    author?: string;
    description?: string;
    defaultLanguage: string;
  };
  featuredReadings: TaleemUnitSummary[];
  chapters: Array<{
    id: string;
    title: string;
    unitCount: number;
  }>;
  topics: Array<{ id: string; displayName: string }>;
};

export function BookHomeView({
  slug,
  home,
  startUnitId,
}: {
  slug: string;
  home: BookHomeClientModel;
  startUnitId: string | null;
}) {
  const [progress, setProgress] = useState<LocalReadingProgress | null>(null);

  useEffect(() => {
    setProgress(getBookProgress(home.book.id));
  }, [home.book.id]);

  const lang = home.book.defaultLanguage?.toUpperCase();
  const continueTarget =
    progress && progress.bookId === home.book.id ? progress : null;
  const startHref = startUnitId ? `/read/${startUnitId}` : `/books/${slug}/chapters`;

  return (
    <div className="space-y-10">
      <section className="space-y-3">
        <h1 className="text-4xl font-semibold">{home.book.title}</h1>
        {home.book.author && <p className="text-[var(--muted)] font-sans">{home.book.author}</p>}
        {lang && <p className="font-sans text-sm text-[var(--muted)]">Language: {lang}</p>}
        {home.book.description && (
          <p className="max-w-3xl text-lg text-[var(--muted)]">{home.book.description}</p>
        )}
      </section>

      {continueTarget && (
        <section className="space-y-3">
          <h2 className="text-2xl font-semibold">Continue reading</h2>
          <a href={`/read/${continueTarget.unitId}`} className="card-link">
            <div className="font-semibold">{continueTarget.unitTitle}</div>
            <div className="text-sm text-[var(--muted)] font-sans mt-1">Pick up where you left off</div>
          </a>
        </section>
      )}

      <section className="space-y-3">
        <h2 className="text-2xl font-semibold">Start reading</h2>
        <p className="font-sans text-[var(--muted)] max-w-2xl">
          Begin with the first reading in this book.
        </p>
        <a href={startHref} className="btn inline-flex">
          {continueTarget ? "Start from the beginning" : "Start reading"}
        </a>
      </section>

      <section className="space-y-3">
        <h2 className="text-2xl font-semibold">Short Ta&apos;leem</h2>
        <p className="font-sans text-[var(--muted)] max-w-2xl">
          Choose a reading session based on the time you have. Each session is a short set of
          readings from the start of this book.
        </p>
        <div className="grid gap-3 sm:grid-cols-3 font-sans">
          {[5, 10, 15].map((m) => (
            <a key={m} href={`/books/${slug}/taleem?minutes=${m}`} className="card-link text-center">
              <div className="font-semibold">~{m} min</div>
            </a>
          ))}
        </div>
      </section>

      <section className="space-y-3">
        <h2 className="text-2xl font-semibold">Search</h2>
        <p className="font-sans text-[var(--muted)]">Find a word or topic inside this book.</p>
        <a href={`/books/${slug}/search`} className="btn inline-flex">
          Search this book
        </a>
      </section>

      {home.featuredReadings.length > 0 && (
        <section className="space-y-3">
          <h2 className="text-2xl font-semibold">Suggested readings</h2>
          <div className="space-y-3">
            {home.featuredReadings.map((u) => (
              <a key={u.id} href={`/read/${u.id}`} className="card-link">
                <div className="font-semibold">{u.title}</div>
                <div className="text-sm text-[var(--muted)] font-sans mt-1">
                  {formatMinutes(u.estimatedReadingSeconds)}
                </div>
              </a>
            ))}
          </div>
        </section>
      )}

      <details className="rounded-2xl border border-[var(--border)] bg-[var(--panel)] open:pb-4">
        <summary className="cursor-pointer list-none px-4 py-4 font-semibold text-2xl flex items-center justify-between gap-3">
          <span>Browse chapters</span>
          <span className="font-sans text-sm font-normal text-[var(--muted)]">
            {home.chapters.length}
          </span>
        </summary>
        <div className="space-y-3 px-4">
          {home.chapters.map((c) => (
            <a key={c.id} href={`/books/${slug}/chapters/${c.id}`} className="card-link">
              <div className="font-semibold">{c.title}</div>
              {c.unitCount !== 1 && (
                <div className="text-sm text-[var(--muted)] font-sans mt-1">
                  {formatReadingCount(c.unitCount)}
                </div>
              )}
            </a>
          ))}
        </div>
      </details>

      {home.topics.length > 0 && (
        <details className="rounded-2xl border border-[var(--border)] bg-[var(--panel)] open:pb-4">
          <summary className="cursor-pointer list-none px-4 py-4 font-semibold text-2xl flex items-center justify-between gap-3">
            <span>Browse topics</span>
            <span className="font-sans text-sm font-normal text-[var(--muted)]">{home.topics.length}</span>
          </summary>
          <div className="flex flex-wrap gap-2 font-sans px-4">
            {home.topics.map((t) => (
              <a
                key={t.id}
                href={`/books/${slug}/topics/${t.id}`}
                className="rounded-full bg-[var(--soft)] px-3 py-1 text-sm text-[var(--accent)]"
              >
                {t.displayName}
              </a>
            ))}
          </div>
        </details>
      )}
    </div>
  );
}
