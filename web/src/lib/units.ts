import { publicApi, TaleemUnitSummary } from "@/lib/api";

/** Canonical reading order: chapter order, then unit order within each chapter. */
export async function loadUnitsInBookOrder(bookId: string): Promise<TaleemUnitSummary[]> {
  const chapters = await publicApi.chapters(bookId);
  const ordered = [...chapters].sort((a, b) => a.orderIndex - b.orderIndex);
  const nested = await Promise.all(ordered.map((c) => publicApi.chapter(c.id)));
  return nested.flatMap((ch) =>
    [...ch.units].sort((a, b) => a.orderIndex - b.orderIndex)
  );
}

export async function loadFirstUnitId(bookId: string): Promise<string | null> {
  const units = await loadUnitsInBookOrder(bookId);
  return units[0]?.id ?? null;
}
