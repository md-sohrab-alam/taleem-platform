import { BookHomeView } from "@/components/BookHomeView";
import { loadBookHome } from "@/lib/load";
import { loadFirstUnitId } from "@/lib/units";

export default async function BookHomePage({ params }: { params: Promise<{ slug: string }> }) {
  const { slug } = await params;
  const { home } = await loadBookHome(slug);
  const startUnitId = await loadFirstUnitId(home.book.id);

  return (
    <BookHomeView
      slug={slug}
      startUnitId={startUnitId}
      home={{
        book: {
          id: home.book.id,
          title: home.book.title,
          author: home.book.author,
          description: home.book.description,
          defaultLanguage: home.book.defaultLanguage,
        },
        featuredReadings: home.featuredReadings,
        chapters: home.chapters.map((c) => ({
          id: c.id,
          title: c.title,
          unitCount: c.unitCount,
        })),
        topics: home.topics.map((t) => ({ id: t.id, displayName: t.displayName })),
      }}
    />
  );
}
