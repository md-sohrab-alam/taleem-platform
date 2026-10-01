"use client";

export default function Error({ reset }: { error: Error; reset: () => void }) {
  return (
    <div className="space-y-4 max-w-xl">
      <h1 className="text-3xl font-semibold">Something went wrong</h1>
      <p className="text-[var(--muted)] font-sans">
        We could not load this page. Check that the API is running, then try again.
      </p>
      <div className="flex gap-3 font-sans">
        <button type="button" className="btn" onClick={() => reset()}>
          Try again
        </button>
        <a href="/" className="card-link px-4 py-2">
          Library
        </a>
      </div>
    </div>
  );
}
