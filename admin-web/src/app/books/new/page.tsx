"use client";

import { FormEvent, useState } from "react";
import { useRouter } from "next/navigation";
import { adminApi } from "@/lib/api";

export default function NewBookPage() {
  const router = useRouter();
  const [title, setTitle] = useState("");
  const [author, setAuthor] = useState("");
  const [subtitle, setSubtitle] = useState("");
  const [description, setDescription] = useState("");
  const [file, setFile] = useState<File | null>(null);
  const [sourceUrl, setSourceUrl] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);
  const [status, setStatus] = useState("");

  async function onSubmit(e: FormEvent) {
    e.preventDefault();
    if (!file) {
      setError("PDF file is required.");
      return;
    }
    setBusy(true);
    setError(null);
    try {
      setStatus("Creating book…");
      const book = await adminApi.createBook({ title, author, subtitle, description });
      setStatus("Uploading PDF…");
      const source = await adminApi.upload(book.id, file, sourceUrl || undefined);
      setStatus("Starting ingestion…");
      const job = await adminApi.startIngest(book.id, source.id);
      router.push(`/books/${book.id}?jobId=${job.id}`);
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed");
    } finally {
      setBusy(false);
    }
  }

  return (
    <div className="max-w-2xl space-y-6">
      <div>
        <h1 className="text-3xl font-semibold">Upload book</h1>
        <p className="text-[var(--muted)]">
          Upload a PDF you are authorized to process. Test PDFs must not be committed to Git.
        </p>
      </div>
      <form className="panel p-6 space-y-4" onSubmit={onSubmit}>
        <div>
          <label className="label">Title</label>
          <input className="input" required value={title} onChange={(e) => setTitle(e.target.value)} />
        </div>
        <div>
          <label className="label">Author</label>
          <input className="input" value={author} onChange={(e) => setAuthor(e.target.value)} />
        </div>
        <div>
          <label className="label">Subtitle</label>
          <input className="input" value={subtitle} onChange={(e) => setSubtitle(e.target.value)} />
        </div>
        <div>
          <label className="label">Description</label>
          <textarea className="input min-h-24" value={description} onChange={(e) => setDescription(e.target.value)} />
        </div>
        <div>
          <label className="label">Optional source URL (for your records)</label>
          <input className="input" value={sourceUrl} onChange={(e) => setSourceUrl(e.target.value)} />
        </div>
        <div>
          <label className="label">PDF file</label>
          <input
            className="input"
            type="file"
            accept="application/pdf,.pdf"
            onChange={(e) => setFile(e.target.files?.[0] ?? null)}
          />
        </div>
        {status && <p className="text-sm text-[var(--accent)]">{status}</p>}
        {error && <p className="text-sm text-[var(--danger)]">{error}</p>}
        <button className="btn btn-primary" disabled={busy}>
          {busy ? "Working…" : "Create & ingest"}
        </button>
      </form>
    </div>
  );
}
