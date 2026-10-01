const API_BASE = process.env.NEXT_PUBLIC_API_BASE_URL ?? "http://localhost:5080";

export class ApiError extends Error {
  status: number;
  constructor(status: number, message: string) {
    super(message);
    this.name = "ApiError";
    this.status = status;
  }
}

export type BookSummary = {
  id: string;
  slug: string;
  title: string;
  subtitle?: string;
  author?: string;
  description?: string;
  coverUrl?: string;
  defaultLanguage: string;
  status: string;
};

export type ContentBlock = {
  id: string;
  blockType: string;
  language: string;
  text: string;
  orderIndex: number;
  sourcePage?: number;
  sourceReference?: string;
};

export type TaleemUnitSummary = {
  id: string;
  chapterId: string;
  title: string;
  estimatedReadingSeconds: number;
  sourcePageStart?: number;
  sourcePageEnd?: number;
  orderIndex: number;
};

export type BookHome = {
  book: BookSummary;
  continueReading?: { unitId?: string; unitTitle?: string; chapterId?: string };
  featuredReadings: TaleemUnitSummary[];
  topics: Array<{ id: string; slug: string; displayName: string }>;
  chapters: Array<{
    id: string;
    title: string;
    description?: string;
    slug?: string;
    orderIndex: number;
    unitCount: number;
  }>;
  suggestedTaleem: Array<{ minutes: number; units: TaleemUnitSummary[]; approximateSeconds: number }>;
  readingOptions: Array<{ key: string; label: string; hrefHint?: string }>;
};

async function api<T>(path: string, init?: RequestInit): Promise<T> {
  const res = await fetch(`${API_BASE}${path}`, {
    ...init,
    headers: { "Content-Type": "application/json", ...(init?.headers ?? {}) },
    next: { revalidate: 0 },
    cache: "no-store",
  });
  if (!res.ok) {
    const body = await res.text();
    throw new ApiError(res.status, body || res.statusText || `Request failed (${res.status})`);
  }
  return res.json();
}

export function isNotFoundError(err: unknown): boolean {
  return err instanceof ApiError && err.status === 404;
}

export const publicApi = {
  books: () => api<{ items: BookSummary[] }>("/api/v1/books"),
  book: (idOrSlug: string) => api<BookSummary & { editions?: unknown[] }>(`/api/v1/books/${idOrSlug}`),
  home: (bookId: string) => api<BookHome>(`/api/v1/books/${bookId}/home`),
  chapters: (bookId: string) => api<BookHome["chapters"]>(`/api/v1/books/${bookId}/chapters`),
  chapter: (chapterId: string) =>
    api<{ id: string; title: string; bookId: string; units: TaleemUnitSummary[] }>(
      `/api/v1/chapters/${chapterId}`
    ),
  unit: (unitId: string) =>
    api<{
      id: string;
      bookId: string;
      chapterId: string;
      title: string;
      estimatedReadingSeconds: number;
      chapterTitle?: string;
      blocks: ContentBlock[];
      topics: Array<{ id: string; displayName: string }>;
    }>(`/api/v1/units/${unitId}`),
  topics: (bookId: string) =>
    api<Array<{ id: string; slug: string; displayName: string }>>(`/api/v1/books/${bookId}/topics`),
  topicUnits: (bookId: string, topicId: string) =>
    api<TaleemUnitSummary[]>(`/api/v1/books/${bookId}/topics/${topicId}/units`),
  taleem: (bookId: string, minutes: number) =>
    api<{ requestedMinutes: number; approximateSeconds: number; units: TaleemUnitSummary[] }>(
      `/api/v1/books/${bookId}/taleem?minutes=${minutes}`
    ),
  search: (bookId: string, q: string) =>
    api<
      Array<{
        unitId: string;
        unitTitle: string;
        chapterTitle: string;
        excerpt: string;
        sourcePage?: number;
      }>
    >(`/api/v1/books/${bookId}/search?q=${encodeURIComponent(q)}`),
  semantic: (bookId: string, query: string) =>
    api<
      Array<{
        similarity: number;
        unitId: string;
        unitTitle: string;
        chapterTitle: string;
        chunkExcerpt: string;
        canonicalBlocks: ContentBlock[];
        sourcePageStart?: number;
      }>
    >("/api/v1/search/semantic", {
      method: "POST",
      body: JSON.stringify({ bookId, query, limit: 8 }),
    }),
};
