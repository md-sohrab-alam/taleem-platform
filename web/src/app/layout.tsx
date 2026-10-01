import type { Metadata } from "next";
import "./globals.css";

export const metadata: Metadata = {
  title: "Taleem",
  description: "Learn from trusted Islamic books, one reading at a time.",
};

export default function RootLayout({ children }: Readonly<{ children: React.ReactNode }>) {
  return (
    <html lang="en">
      <body>
        <header className="border-b border-[var(--border)] bg-white/80 backdrop-blur sticky top-0 z-10">
          <div className="container py-4 flex items-center justify-between">
            <a href="/" className="text-2xl font-semibold tracking-tight">
              Taleem
            </a>
            <nav className="font-sans text-sm text-[var(--muted)] flex gap-4">
              <a href="/">Library</a>
            </nav>
          </div>
        </header>
        <main className="container py-8">{children}</main>
      </body>
    </html>
  );
}
