const API_BASE = process.env.NEXT_PUBLIC_API_BASE_URL ?? "http://localhost:5080";

export type AdminBook = {
  id: string;
  slug: string;
  title: string;
  subtitle?: string;
  author?: string;
  description?: string;
  status: string;
  contentVersion: number;
  createdAt: string;
  updatedAt: string;
  publishedAt?: string;
  sourceDocumentCount: number;
  chapterCount: number;
  unitCount: number;
};

export type IngestionJob = {
  id: string;
  sourceDocumentId: string;
  bookId: string;
  status: string;
  startedAt?: string;
  completedAt?: string;
  error?: string;
  progressMessage?: string;
  progressPercent: number;
  processorVersion?: string;
  reportJson?: string;
};

export type IngestionReport = {
  processorVersion: string;
  pageCount: number;
  sectionCount: number;
  chapterCount: number;
  unitCount: number;
  contentBlockCount: number;
  searchChunkCount: number;
  tocEntryCount: number;
  excludedFrontMatterCount: number;
  headingCandidateCount: number;
  lowConfidenceItemCount: number;
  headingOnlyUnitCount: number;
  emptyBodyUnitCount: number;
  unitsPerPageAverage: number;
  headingOnlyUnitRatio: number;
  frontMatterOriginRatio: number;
  hasStructuralAnomalies: boolean;
  anomalyFlags: string[];
  documentZones: Array<{ pageNumber: number; zoneRole: string; confidence: number; evidence: string[] }>;
  tocEntries: Array<{ title: string; pageNumber?: number; sourcePage: number; confidence: number }>;
  excludedFrontMatter: Array<{ pageNumber: number; text: string; elementType: string; reason: string }>;
  suspiciousUnitTitles: string[];
  unitsPerPageDistribution: Record<string, number>;
};

export type ReviewBook = {
  book: AdminBook;
  sources: Array<{
    id: string;
    originalFilename: string;
    pageCount: number;
    uploadedAt: string;
  }>;
  jobs: IngestionJob[];
  sections: Array<{ id: string; title: string; orderIndex: number }>;
  chapters: Array<{
    id: string;
    title: string;
    orderIndex: number;
    unitCount: number;
    sectionId?: string;
    sourcePageStart?: number;
    sourcePageEnd?: number;
  }>;
  units: Array<{
    id: string;
    chapterId: string;
    chapterTitle: string;
    title: string;
    estimatedReadingSeconds: number;
    sourcePageStart?: number;
    sourcePageEnd?: number;
    orderIndex: number;
    reviewStatus: string;
    blocks: Array<{
      id: string;
      blockType: string;
      language: string;
      text: string;
      orderIndex: number;
      sourcePage?: number;
      verificationStatus: string;
    }>;
    topics: Array<{ id: string; slug: string; displayName: string }>;
  }>;
  ingestionReport?: IngestionReport | null;
};

function authHeaders(): HeadersInit {
  const token = typeof window !== "undefined" ? localStorage.getItem("taleem_admin_token") : null;
  return token ? { Authorization: `Bearer ${token}` } : {};
}

async function api<T>(path: string, init?: RequestInit): Promise<T> {
  const res = await fetch(`${API_BASE}${path}`, {
    ...init,
    headers: {
      ...(init?.body instanceof FormData ? {} : { "Content-Type": "application/json" }),
      ...authHeaders(),
      ...(init?.headers ?? {}),
    },
  });
  if (!res.ok) {
    const body = await res.text();
    throw new Error(body || res.statusText);
  }
  if (res.status === 204) return undefined as T;
  return res.json();
}

export const adminApi = {
  login: (username: string, password: string) =>
    api<{ accessToken: string; expiresAt: string }>("/api/admin/v1/auth/login", {
      method: "POST",
      body: JSON.stringify({ username, password }),
    }),
  listBooks: () => api<AdminBook[]>("/api/admin/v1/books"),
  createBook: (payload: {
    title: string;
    subtitle?: string;
    author?: string;
    description?: string;
    slug?: string;
    defaultLanguage?: string;
  }) =>
    api<AdminBook>("/api/admin/v1/books", {
      method: "POST",
      body: JSON.stringify(payload),
    }),
  getBook: (id: string) => api<AdminBook>(`/api/admin/v1/books/${id}`),
  upload: async (bookId: string, file: File, sourceUrl?: string) => {
    const form = new FormData();
    form.append("file", file);
    if (sourceUrl) form.append("sourceUrl", sourceUrl);
    return api<{ id: string; originalFilename: string }>(`/api/admin/v1/books/${bookId}/sources`, {
      method: "POST",
      body: form,
      headers: authHeaders(),
    });
  },
  startIngest: (bookId: string, sourceId: string) =>
    api<IngestionJob>(`/api/admin/v1/books/${bookId}/sources/${sourceId}/ingest`, { method: "POST" }),
  getJob: (jobId: string) => api<IngestionJob>(`/api/admin/v1/jobs/${jobId}`),
  review: (bookId: string) => api<ReviewBook>(`/api/admin/v1/books/${bookId}/review`),
  pages: (sourceId: string) =>
    api<Array<{ pageNumber: number; rawText: string; normalizedText: string; extractionConfidence: number }>>(
      `/api/admin/v1/sources/${sourceId}/pages`
    ),
  updateUnit: (unitId: string, body: unknown) =>
    api(`/api/admin/v1/units/${unitId}`, { method: "PUT", body: JSON.stringify(body) }),
  updateBlock: (blockId: string, body: unknown) =>
    api(`/api/admin/v1/blocks/${blockId}`, { method: "PUT", body: JSON.stringify(body) }),
  approve: (bookId: string) => api<void>(`/api/admin/v1/books/${bookId}/approve`, { method: "POST" }),
  publish: (bookId: string) => api<void>(`/api/admin/v1/books/${bookId}/publish`, { method: "POST" }),
  deleteBook: (bookId: string, confirmationSlug: string) =>
    api<void>(`/api/admin/v1/books/${bookId}`, {
      method: "DELETE",
      body: JSON.stringify({ confirmationSlug }),
    }),
};
