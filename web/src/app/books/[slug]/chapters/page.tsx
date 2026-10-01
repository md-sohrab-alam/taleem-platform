import { formatReadingCount, loadBook } from "@/lib/load";
import { isNotFoundError, publicApi } from "@/lib/api";
import { notFound } from "next/navigation";

export default async function ChaptersPage({ params }: { params: Promise<{ slug: string }> }) {
  const { slug } = await params;
  const book = await loadBook(slug);
  let chapters;
  try {
    chapters = await publicApi.chapters(book.id);
  } catch (err) {
    if (isNotFoundError(err)) notFound();
    throw err;
  }

  return (
    <div className="space-y-6">
      <a href={`/books/${slug}`} className="font-sans text-sm text-[var(--accent)]">
        ← Book home
      </a>
      <h1 className="text-3xl font-semibold">Chapters</h1>
      <div className="space-y-3">
        {chapters.map((c) => (
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
    </div>
  );
}
