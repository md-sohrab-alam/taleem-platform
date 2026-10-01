export default function NotFound() {
  return (
    <div className="space-y-4 max-w-xl">
      <h1 className="text-3xl font-semibold">Not found</h1>
      <p className="text-[var(--muted)] font-sans">
        This book or reading is unavailable. It may be unpublished, removed, or the link may be incorrect.
      </p>
      <a href="/" className="btn inline-flex">
        Back to library
      </a>
    </div>
  );
}
