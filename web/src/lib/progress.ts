export type LocalReadingProgress = {
  bookId: string;
  unitId: string;
  unitTitle: string;
  lastOpenedAt: string;
};

const STORAGE_KEY = "taleem.readingProgress.v1";

type ProgressMap = Record<string, LocalReadingProgress>;

function readAll(): ProgressMap {
  if (typeof window === "undefined") return {};
  try {
    const raw = window.localStorage.getItem(STORAGE_KEY);
    if (!raw) return {};
    const parsed = JSON.parse(raw) as ProgressMap;
    return parsed && typeof parsed === "object" ? parsed : {};
  } catch {
    return {};
  }
}

function writeAll(map: ProgressMap) {
  window.localStorage.setItem(STORAGE_KEY, JSON.stringify(map));
}

export function getBookProgress(bookId: string): LocalReadingProgress | null {
  const entry = readAll()[bookId];
  if (!entry?.unitId) return null;
  return entry;
}

export function setBookProgress(progress: LocalReadingProgress) {
  const map = readAll();
  map[progress.bookId] = progress;
  writeAll(map);
}
