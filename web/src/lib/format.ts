export function formatReadingCount(n: number): string {
  return n === 1 ? "1 reading" : `${n} readings`;
}

export function formatMinutes(seconds: number): string {
  const m = Math.max(1, Math.round(seconds / 60));
  return m === 1 ? "~1 min" : `~${m} min`;
}
