import { publicApi } from "@/lib/api";

export default async function LibraryPage() {
  let books: Awaited<ReturnType<typeof publicApi.books>>["items"] = [];
  let error: string | null = null;
  try {
    const res = await publicApi.books();
    books = res.items;
  } catch (e) {
    error = e instanceof Error ? e.message : "Failed to load library";
  }

  return (
    <div className="space-y-8">
      <section className="space-y-4 max-w-2xl">
        <h1 className="text-4xl font-semibold tracking-tight">Taleem</h1>
        <p className="text-[var(--muted)] text-lg font-sans">
          Learn from trusted Islamic books, one reading at a time.
        </p>
        <p className="text-[var(--muted)] font-sans">
          Read complete books, explore topics, or choose a short 5, 10 or 15-minute learning session.
        </p>
      </section>

      {error && (
        <div className="rounded-xl border border-red-200 bg-red-50 p-4 font-sans text-sm text-red-800">
          Unable to load the library right now. Please try again in a moment.
        </div>
      )}

      <div className="grid gap-4 sm:grid-cols-2">
        {books.map((book) => (
          <a key={book.id} href={`/books/${book.slug}`} className="card-link">
            <div className="text-xl font-semibold">{book.title}</div>
            {book.author && <div className="text-[var(--muted)] font-sans text-sm mt-1">{book.author}</div>}
            {book.defaultLanguage && (
              <div className="text-[var(--muted)] font-sans text-xs mt-1 uppercase tracking-wide">
                {book.defaultLanguage}
              </div>
            )}
            {book.description && (
              <p className="mt-3 text-[var(--muted)] font-sans text-sm line-clamp-3">{book.description}</p>
            )}
            <div className="mt-4 font-sans text-sm font-semibold text-[var(--accent)]">Open book</div>
          </a>
        ))}
        {books.length === 0 && !error && (
          <div className="font-sans text-[var(--muted)]">No published books yet.</div>
        )}
      </div>
    </div>
  );
}
