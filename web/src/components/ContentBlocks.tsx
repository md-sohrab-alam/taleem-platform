import { ContentBlock } from "@/lib/api";

function isRtl(language: string, blockType: string) {
  const lang = language.toLowerCase();
  if (lang === "ar" || lang === "ur") return true;
  return blockType.includes("ARABIC");
}

function titlesMatch(a: string, b: string) {
  return a.trim().localeCompare(b.trim(), undefined, { sensitivity: "accent" }) === 0;
}

function stripListMarker(text: string): string {
  return text.replace(/^([•\-*]|\d+[.)])\s+/, "").trim();
}

function leadingMarkerNumber(text: string): number | null {
  const m = text.trim().match(/^(\d+)[.)]\s+/);
  if (!m) return null;
  const n = Number(m[1]);
  return Number.isFinite(n) && n > 0 ? n : null;
}

function splitListItems(text: string): { items: string[]; start: number | null } {
  const lines = text
    .split(/\r?\n/)
    .map((l) => l.trim())
    .filter(Boolean);
  if (lines.length > 1) {
    const start = leadingMarkerNumber(lines[0]);
    return {
      items: lines.map(stripListMarker).filter(Boolean),
      start,
    };
  }
  const numbered = text.match(/(?:^|\s)\d+[.)]\s+/g);
  if (numbered && numbered.length >= 2) {
    const parts = text
      .split(/(?=(?:^|\s)\d+[.)]\s+)/)
      .map((p) => p.trim())
      .filter(Boolean);
    const start = parts[0] ? leadingMarkerNumber(parts[0]) : null;
    return {
      items: parts.map(stripListMarker).filter(Boolean),
      start,
    };
  }
  return {
    items: [stripListMarker(text)].filter(Boolean),
    start: leadingMarkerNumber(text),
  };
}

function isOrderedList(text: string) {
  return /^\s*\d+[.)]\s+/m.test(text) || /(?:^|\s)\d+[.)]\s+/.test(text);
}

/** Consecutive LIST blocks are merged into one semantic list for reading. */
function groupBlocks(blocks: ContentBlock[]): Array<ContentBlock | ContentBlock[]> {
  const groups: Array<ContentBlock | ContentBlock[]> = [];
  let listRun: ContentBlock[] | null = null;

  for (const block of blocks) {
    const isList = block.blockType.toUpperCase().includes("LIST");
    if (isList) {
      if (!listRun) listRun = [];
      listRun.push(block);
      continue;
    }
    if (listRun) {
      groups.push(listRun);
      listRun = null;
    }
    groups.push(block);
  }
  if (listRun) groups.push(listRun);
  return groups;
}

function renderListItems(texts: string[], className: string) {
  const ordered = texts.some(isOrderedList);
  const parsed = texts.map(splitListItems);
  const items = parsed.flatMap((p) => p.items);
  const start = ordered ? parsed.find((p) => p.start != null)?.start ?? null : null;
  const ListTag = ordered ? "ol" : "ul";
  return (
    <ListTag
      className={`${className} list-outside ms-6 space-y-2 ${ordered ? "list-decimal" : "list-disc"}`}
      {...(ordered && start && start > 1 ? { start } : {})}
    >
      {items.map((item, i) => (
        <li key={i} className="leading-8 ps-1">
          {item}
        </li>
      ))}
    </ListTag>
  );
}

export function ContentBlocks({
  blocks,
  unitTitle,
}: {
  blocks: ContentBlock[];
  unitTitle?: string;
}) {
  const groups = groupBlocks(blocks);

  return (
    <div className="space-y-5">
      {groups.map((group, gi) => {
        if (Array.isArray(group)) {
          const rtl = group.some((b) => isRtl(b.language, b.blockType));
          return (
            <div key={group.map((b) => b.id).join("-") || `list-${gi}`} className={rtl ? "rtl" : ""}>
              {renderListItems(
                group.map((b) => b.text),
                "text-lg"
              )}
            </div>
          );
        }

        const block = group;
        const rtl = isRtl(block.language, block.blockType);
        const type = block.blockType.toUpperCase();
        const isHeading = type === "HEADING" || type.includes("HEADING");

        if (isHeading && unitTitle && titlesMatch(block.text, unitTitle)) {
          return null;
        }

        const emphasis =
          type.includes("QURAN") || type.includes("HADITH") ? "text-xl" : "text-lg";

        return (
          <div key={block.id} className={rtl ? "rtl" : ""}>
            {isHeading ? (
              <h2 className="text-2xl font-semibold">{block.text}</h2>
            ) : (
              <p className={`leading-8 whitespace-pre-wrap ${emphasis}`}>{block.text}</p>
            )}
          </div>
        );
      })}
    </div>
  );
}
