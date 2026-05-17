export interface PagedResponse<T> {
  items: T[];
  hasMore: boolean;
  nextCursorAt: string | null;
  nextCursorId: number | null;
}

function optionalInt(value: unknown): number | null {
  if (typeof value === "number" && Number.isFinite(value)) return value;
  return null;
}

export function normalizePagedResponse<T>(
  raw: unknown,
  normalizeItem: (item: unknown) => T,
): PagedResponse<T> {
  const record = raw as Record<string, unknown>;
  const itemsRaw = Array.isArray(record.items)
    ? record.items
    : Array.isArray(record.Items)
      ? record.Items
      : [];
  const hasMore =
    (typeof record.hasMore === "boolean" ? record.hasMore : undefined) ??
    (typeof record.HasMore === "boolean" ? record.HasMore : undefined) ??
    false;
  const nextCursorAt =
    (typeof record.nextCursorAt === "string" ? record.nextCursorAt : undefined) ??
    (typeof record.NextCursorAt === "string" ? record.NextCursorAt : undefined) ??
    null;
  const nextCursorId =
    optionalInt(record.nextCursorId) ?? optionalInt(record.NextCursorId);

  return {
    items: itemsRaw.map(normalizeItem),
    hasMore,
    nextCursorAt,
    nextCursorId,
  };
}
