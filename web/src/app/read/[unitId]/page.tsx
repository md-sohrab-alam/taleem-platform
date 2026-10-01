import { ContentBlocks } from "@/components/ContentBlocks";
import { ReaderNav } from "@/components/ReaderNav";
import { ReadingProgressTracker } from "@/components/ReadingProgressTracker";
import { publicApi } from "@/lib/api";
import { formatMinutes, loadUnit } from "@/lib/load";
import { loadUnitsInBookOrder } from "@/lib/units";

function titlesMatch(a?: string, b?: string) {
  if (!a || !b) return false;
  return a.trim().localeCompare(b.trim(), undefined, { sensitivity: "accent" }) === 0;
}

export default async function ReaderPage({ params }: { params: Promise<{ unitId: string }> }) {
  const { unitId } = await params;
  const unit = await loadUnit(unitId);
  let bookSlug: string | null = null;
  try {
    const book = await publicApi.book(unit.bookId);
    bookSlug = book.slug;
  } catch {
    bookSlug = null;
  }

  const unitsInOrder = await loadUnitsInBookOrder(unit.bookId);
  const sameChapterTitle = titlesMatch(unit.chapterTitle, unit.title);

  return (
    <article className="max-w-3xl mx-auto space-y-6">
      <ReadingProgressTracker bookId={unit.bookId} unitId={unit.id} unitTitle={unit.title} />
      <div className="space-y-2">
        <div className="font-sans text-sm flex flex-wrap gap-x-4 gap-y-1">
          <a href="/" className="text-[var(--accent)]">
            ← Library
          </a>
          {bookSlug && (
            <>
              <a href={`/books/${bookSlug}`} className="text-[var(--accent)]">
                Book home
              </a>
              {!sameChapterTitle && (
                <a href={`/books/${bookSlug}/chapters/${unit.chapterId}`} className="text-[var(--accent)]">
                  Chapter
                </a>
              )}
            </>
          )}
        </div>
        {!sameChapterTitle && unit.chapterTitle && (
          <div className="font-sans text-sm text-[var(--muted)]">{unit.chapterTitle}</div>
        )}
        <h1 className="text-4xl font-semibold">{unit.title}</h1>
        <div className="font-sans text-sm text-[var(--muted)]">
          {formatMinutes(unit.estimatedReadingSeconds)}
          {unit.topics.length > 0 ? ` · ${unit.topics.map((t) => t.displayName).join(", ")}` : ""}
        </div>
      </div>
      <ContentBlocks blocks={unit.blocks} unitTitle={unit.title} />
      <ReaderNav units={unitsInOrder} currentUnitId={unit.id} />
    </article>
  );
}
