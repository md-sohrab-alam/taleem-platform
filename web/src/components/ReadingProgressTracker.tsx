"use client";

import { useEffect } from "react";
import { setBookProgress } from "@/lib/progress";

export function ReadingProgressTracker({
  bookId,
  unitId,
  unitTitle,
}: {
  bookId: string;
  unitId: string;
  unitTitle: string;
}) {
  useEffect(() => {
    setBookProgress({
      bookId,
      unitId,
      unitTitle,
      lastOpenedAt: new Date().toISOString(),
    });
  }, [bookId, unitId, unitTitle]);

  return null;
}
