import { formatMinutes, loadBook } from "@/lib/load";
import { isNotFoundError, publicApi } from "@/lib/api";
import { notFound } from "next/navigation";

export default async function TopicUnitsPage({
  params,
}: {
  params: Promise<{ slug: string; topicId: string }>;
}) {
  const { slug, topicId } = await params;
  const book = await loadBook(slug);
  let units;
  let topicName = "Topic readings";
  try {
    const topics = await publicApi.topics(book.id);
    const match = topics.find((t) => t.id === topicId);
    if (!match) notFound();
    topicName = match.displayName;
    units = await publicApi.topicUnits(book.id, topicId);
  } catch (err) {
    if (isNotFoundError(err)) notFound();
    throw err;
  }

  return (
    <div className="space-y-6">
      <a href={`/books/${slug}/topics`} className="font-sans text-sm text-[var(--accent)]">
        ← Topics
      </a>
      <h1 className="text-3xl font-semibold">{topicName}</h1>
      <div className="space-y-3">
        {units.map((u) => (
          <a key={u.id} href={`/read/${u.id}`} className="card-link">
            <div className="font-semibold">{u.title}</div>
            <div className="text-sm text-[var(--muted)] font-sans mt-1">
              {formatMinutes(u.estimatedReadingSeconds)}
            </div>
          </a>
        ))}
        {units.length === 0 && (
          <p className="font-sans text-[var(--muted)]">No readings for this topic.</p>
        )}
      </div>
    </div>
  );
}
