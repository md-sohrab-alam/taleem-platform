import { formatMinutes, loadBook, loadChapter } from "@/lib/load";
import { redirect } from "next/navigation";

export default async function ChapterPage({
  params,
}: {
  params: Promise<{ slug: string; chapterId: string }>;
}) {
  const { slug, chapterId } = await params;
  await loadBook(slug);
  const chapter = await loadChapter(chapterId);

  if (chapter.units.length === 1) {
    redirect(`/read/${chapter.units[0].id}`);
  }

  return (
    <div className="space-y-6">
      <a href={`/books/${slug}`} className="font-sans text-sm text-[var(--accent)]">
        ← Book home
      </a>
      <h1 className="text-3xl font-semibold">{chapter.title}</h1>
      <div className="space-y-3">
        {chapter.units.map((u) => (
          <a key={u.id} href={`/read/${u.id}`} className="card-link">
            <div className="font-semibold">{u.title}</div>
            <div className="text-sm text-[var(--muted)] font-sans mt-1">
              {formatMinutes(u.estimatedReadingSeconds)}
            </div>
          </a>
        ))}
        {chapter.units.length === 0 && (
          <p className="font-sans text-[var(--muted)]">No readings in this chapter yet.</p>
        )}
      </div>
    </div>
  );
}
