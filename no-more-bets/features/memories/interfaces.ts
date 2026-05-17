/** GET api/memories */
export interface MemoriesPage {
  items: MemoryListItem[];
  hasMore: boolean;
  nextCursorUpdatedAt: string | null;
  nextCursorId: number | null;
}

/**
 * GET api/memories list item (MemoryListItemDto).
 */
export interface MemoryListItem {
  id: number;
  name: string;
  description: string | null;
  content: string;
  createdAt: string;
  updatedAt: string;
}
