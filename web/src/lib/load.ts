import { notFound } from "next/navigation";
import { isNotFoundError, publicApi } from "@/lib/api";

export { formatMinutes, formatReadingCount } from "@/lib/format";

export async function loadBook(slug: string) {
  try {
    return await publicApi.book(slug);
  } catch (err) {
    if (isNotFoundError(err)) notFound();
    throw err;
  }
}

export async function loadBookHome(slug: string) {
  const book = await loadBook(slug);
  try {
    const home = await publicApi.home(book.id);
    return { book, home };
  } catch (err) {
    if (isNotFoundError(err)) notFound();
    throw err;
  }
}

export async function loadUnit(unitId: string) {
  try {
    return await publicApi.unit(unitId);
  } catch (err) {
    if (isNotFoundError(err)) notFound();
    throw err;
  }
}

export async function loadChapter(chapterId: string) {
  try {
    return await publicApi.chapter(chapterId);
  } catch (err) {
    if (isNotFoundError(err)) notFound();
    throw err;
  }
}
