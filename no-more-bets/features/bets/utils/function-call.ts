export interface FunctionCallArgument {
  name: string;
  value?: string | null;
}

export interface FunctionCallPayload {
  name: string;
  arguments?: FunctionCallArgument[] | null;
}

export function parseJsonValue(value: string | null | undefined): unknown {
  if (value == null || value === "") return null;
  try {
    return JSON.parse(value);
  } catch {
    return value;
  }
}

export function findArgumentValue(payload: FunctionCallPayload, argumentName: string): unknown {
  const arg = payload.arguments?.find((a) => a.name === argumentName);
  return parseJsonValue(arg?.value);
}

export function asArray(value: unknown): unknown[] {
  if (Array.isArray(value)) return value;
  if (value == null) return [];
  return [value];
}

export function parseFunctionCallText(text: string): FunctionCallPayload | null {
  try {
    const parsed: unknown = JSON.parse(text);
    if (parsed != null && typeof parsed === "object" && "name" in parsed) {
      const name = (parsed as { name: unknown }).name;
      if (typeof name === "string") {
        return parsed as FunctionCallPayload;
      }
    }
  } catch {
    // ignore invalid JSON
  }
  return null;
}

export function parseNumericId(value: unknown): number | null {
  if (value == null || value === "") return null;
  const numeric = typeof value === "number" ? value : Number(value);
  if (!Number.isFinite(numeric)) return null;
  return numeric;
}
