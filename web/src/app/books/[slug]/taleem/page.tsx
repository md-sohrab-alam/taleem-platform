import { formatMinutes, loadBook } from "@/lib/load";
import { isNotFoundError, publicApi } from "@/lib/api";
import { notFound } from "next/navigation";

export default async function TaleemPage({
  params,
  searchParams,
}: {
  params: Promise<{ slug: string }>;
  searchParams: Promise<{ minutes?: string }>;
}) {
  const { slug } = await params;
  const sp = await searchParams;
  const minutes = Number(sp.minutes ?? "10");
  const book = await loadBook(slug);
  let selection;
  try {
    selection = await publicApi.taleem(book.id, minutes);
  } catch (err) {
    if (isNotFoundError(err)) notFound();
    throw err;
  }

  return (
    <div className="space-y-6">
      <a href={`/books/${slug}`} className="font-sans text-sm text-[var(--accent)]">
        ← Book home
      </a>
      <h1 className="text-3xl font-semibold">~{minutes}-minute Ta&apos;leem</h1>
      <div className="font-sans text-[var(--muted)] space-y-2 max-w-2xl">
        <p>A short reading session from this book.</p>
        <p>
          Readings are taken in order from the beginning of the book until about {minutes} minutes of
          reading time.
        </p>
      </div>
      <div className="space-y-3">
        {selection.units.map((u) => (
          <a key={u.id} href={`/read/${u.id}`} className="card-link">
            <div className="font-semibold">{u.title}</div>
            <div className="text-sm text-[var(--muted)] font-sans mt-1">
              {formatMinutes(u.estimatedReadingSeconds)}
            </div>
          </a>
        ))}
        {selection.units.length === 0 && (
          <p className="font-sans text-[var(--muted)]">No readings fit this duration yet.</p>
        )}
      </div>
      {selection.units[0] && (
        <a href={`/read/${selection.units[0].id}`} className="btn inline-flex">
          Start this session
        </a>
      )}
    </div>
  );
}
