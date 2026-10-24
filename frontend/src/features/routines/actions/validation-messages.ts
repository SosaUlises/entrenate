export function getValidationMessages(details: unknown): string[] {
  if (!details || typeof details !== "object" || Array.isArray(details)) return [];

  return Object.values(details)
    .flatMap((value) => Array.isArray(value) ? value : [])
    .filter((message): message is string => typeof message === "string" && message.trim().length > 0)
    .slice(0, 3);
}
