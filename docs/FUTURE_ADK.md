# Future ADK / agent integration

ADK is **not** required for V1.

When added, agents must operate only over **published** canonical content via tools such as:

| Tool | Backing API |
|------|-------------|
| `searchBook(bookId, query)` | keyword + semantic search |
| `getBook(bookId)` | `GET /api/v1/books/{id}` |
| `getChapter(chapterId)` | `GET /api/v1/chapters/{id}` |
| `getTaleemByTopic(bookId, topic)` | topics units endpoint |
| `getTaleemByDuration(bookId, minutes)` | `GET /api/v1/books/{id}/taleem` |
| `getRelatedReadings(unitId)` | future relational query |
| `getBookProgress(userId, bookId)` | reading progress |

Never generate Quran/Hadith text for answers; retrieve and cite canonical ContentBlocks.
