"use client";

import { useEffect, useState } from "react";
import { adminApi, AdminBook } from "@/lib/api";

export default function DashboardPage() {
  const [books, setBooks] = useState<AdminBook[]>([]);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    adminApi
      .listBooks()
      .then(setBooks)
      .catch((e) => setError(e.message));
  }, []);

  const published = books.filter((b) => b.status === "PUBLISHED").length;
  const review = books.filter((b) => b.status === "NEEDSREVIEW" || b.status === "NEEDS_REVIEW").length;

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-3xl font-semibold">Dashboard</h1>
        <p className="text-[var(--muted)] mt-1">Upload → ingest → review → publish. Books are data.</p>
      </div>

      {error && (
        <div className="panel p-4 text-[var(--danger)]">
          {error.includes("401") || error.toLowerCase().includes("unauthorized")
            ? "Please login first."
            : error}
        </div>
      )}

      <div className="grid gap-4 sm:grid-cols-3">
        <div className="panel p-5">
          <div className="text-sm text-[var(--muted)]">Books</div>
          <div className="text-3xl font-semibold mt-1">{books.length}</div>
        </div>
        <div className="panel p-5">
          <div className="text-sm text-[var(--muted)]">Published</div>
          <div className="text-3xl font-semibold mt-1">{published}</div>
        </div>
        <div className="panel p-5">
          <div className="text-sm text-[var(--muted)]">Needs review</div>
          <div className="text-3xl font-semibold mt-1">{review}</div>
        </div>
      </div>

      <div className="flex gap-3">
        <a className="btn btn-primary" href="/books/new">
          Upload book
        </a>
        <a className="btn btn-secondary" href="/books">
          Manage books
        </a>
      </div>
    </div>
  );
}
