"use client";

import { useEffect, useState } from "react";
import { adminApi, AdminBook } from "@/lib/api";

export default function BooksPage() {
  const [books, setBooks] = useState<AdminBook[]>([]);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    adminApi.listBooks().then(setBooks).catch((e) => setError(e.message));
  }, []);

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-3xl font-semibold">Books</h1>
          <p className="text-[var(--muted)]">Canonical content inventory (all lifecycle states).</p>
        </div>
        <a href="/books/new" className="btn btn-primary">
          New book
        </a>
      </div>
      {error && <div className="panel p-4 text-[var(--danger)]">{error}</div>}
      <div className="panel overflow-hidden">
        <table className="w-full text-sm">
          <thead className="bg-[var(--accent-soft)] text-left">
            <tr>
              <th className="p-3">Title</th>
              <th className="p-3">Slug</th>
              <th className="p-3">Status</th>
              <th className="p-3">Units</th>
              <th className="p-3"></th>
            </tr>
          </thead>
          <tbody>
            {books.map((b) => (
              <tr key={b.id} className="border-t border-[var(--border)]">
                <td className="p-3 font-medium">{b.title}</td>
                <td className="p-3 text-[var(--muted)]">{b.slug}</td>
                <td className="p-3">{b.status}</td>
                <td className="p-3">{b.unitCount}</td>
                <td className="p-3 text-right">
                  <a className="text-[var(--accent)] font-semibold" href={`/books/${b.id}`}>
                    Open
                  </a>
                </td>
              </tr>
            ))}
            {books.length === 0 && !error && (
              <tr>
                <td className="p-6 text-[var(--muted)]" colSpan={5}>
                  No books yet. Create one and upload a PDF.
                </td>
              </tr>
            )}
          </tbody>
        </table>
      </div>
    </div>
  );
}
