import { loadBook } from "@/lib/load";
import { isNotFoundError, publicApi } from "@/lib/api";
import { notFound } from "next/navigation";

export default async function TopicsPage({ params }: { params: Promise<{ slug: string }> }) {
  const { slug } = await params;
  const book = await loadBook(slug);
  let topics;
  try {
    topics = await publicApi.topics(book.id);
  } catch (err) {
    if (isNotFoundError(err)) notFound();
    throw err;
  }

  return (
    <div className="space-y-6">
      <a href={`/books/${slug}`} className="font-sans text-sm text-[var(--accent)]">
        ← Book home
      </a>
      <h1 className="text-3xl font-semibold">Topics</h1>
      {topics.length === 0 ? (
        <p className="font-sans text-[var(--muted)]">No topics available for this book yet.</p>
      ) : (
        <div className="grid gap-3 sm:grid-cols-2">
          {topics.map((t) => (
            <a key={t.id} href={`/books/${slug}/topics/${t.id}`} className="card-link">
              {t.displayName}
            </a>
          ))}
        </div>
      )}
    </div>
  );
}
