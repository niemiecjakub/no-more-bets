/**
 * GET api/Database/memories returns an array of these (MemoryListItemDto).
 */
export interface MemoryListItem {
  id: number;
  name: string;
  description: string | null;
  content: string;
  createdAt: string;
  updatedAt: string;
}
