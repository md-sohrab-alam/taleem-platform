import { TaleemUnitSummary } from "@/lib/api";

export function ReaderNav({
  units,
  currentUnitId,
}: {
  units: TaleemUnitSummary[];
  currentUnitId: string;
}) {
  const index = units.findIndex((u) => u.id === currentUnitId);
  if (index < 0 || units.length === 0) return null;

  const prev = index > 0 ? units[index - 1] : null;
  const next = index < units.length - 1 ? units[index + 1] : null;

  return (
    <nav className="mt-10 pt-6 border-t border-[var(--border)] space-y-4 font-sans" aria-label="Reading navigation">
      <p className="text-sm text-[var(--muted)]">
        Reading {index + 1} of {units.length}
      </p>
      <div className="grid gap-3 sm:grid-cols-2">
        {prev ? (
          <a href={`/read/${prev.id}`} className="card-link">
            <div className="text-xs uppercase tracking-wide text-[var(--muted)]">Previous</div>
            <div className="font-semibold mt-1">{prev.title}</div>
          </a>
        ) : (
          <div className="hidden sm:block" />
        )}
        {next ? (
          <a href={`/read/${next.id}`} className="card-link sm:text-end">
            <div className="text-xs uppercase tracking-wide text-[var(--muted)]">Next</div>
            <div className="font-semibold mt-1">{next.title}</div>
          </a>
        ) : null}
      </div>
    </nav>
  );
}
