import type { Metadata } from "next";
import "./globals.css";

export const metadata: Metadata = {
  title: "Taleem Admin",
  description: "Content ingestion and publishing for the Taleem platform",
};

export default function RootLayout({ children }: Readonly<{ children: React.ReactNode }>) {
  return (
    <html lang="en">
      <body>
        <div className="min-h-screen">
          <header className="border-b border-[var(--border)] bg-white/80 backdrop-blur sticky top-0 z-10">
            <div className="mx-auto max-w-6xl px-4 py-4 flex items-center justify-between">
              <a href="/" className="text-xl font-semibold tracking-tight">
                Taleem <span className="text-[var(--accent)]">Admin</span>
              </a>
              <nav className="flex gap-4 text-sm text-[var(--muted)]">
                <a href="/">Dashboard</a>
                <a href="/books">Books</a>
                <a href="/login">Login</a>
              </nav>
            </div>
          </header>
          <main className="mx-auto max-w-6xl px-4 py-8">{children}</main>
        </div>
      </body>
    </html>
  );
}
