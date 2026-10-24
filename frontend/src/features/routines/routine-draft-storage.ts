import type { RoutineDraft, RoutineDraftDay } from "./context/routine-draft-context";
import type { RoutineDraftExercise } from "./types/routine-draft-exercise";

export const ROUTINE_DRAFT_STORAGE_VERSION = 1;
export const ROUTINE_DRAFT_TTL_MS = 30 * 24 * 60 * 60 * 1000;

export type StoredRoutineDraft = {
  version: typeof ROUTINE_DRAFT_STORAGE_VERSION;
  updatedAt: string;
  draft: RoutineDraft;
};

export function getRoutineDraftKey(userId: string): string {
  return `entrenate:routine-draft:v1:${userId}`;
}

export function readRoutineDraft(
  userId: string,
  now: number = Date.now(),
): StoredRoutineDraft | null {
  const storage = getLocalStorage();
  if (!storage) return null;

  const key = getRoutineDraftKey(userId);

  try {
    const rawDraft = storage.getItem(key);
    if (!rawDraft) return null;

    const parsed: unknown = JSON.parse(rawDraft);
    if (!isStoredRoutineDraft(parsed) || isRoutineDraftExpired(parsed.updatedAt, now) || !hasMeaningfulRoutineDraft(parsed.draft)) {
      storage.removeItem(key);
      return null;
    }

    return parsed;
  } catch {
    try {
      storage.removeItem(key);
    } catch {
      // Storage may be unavailable; the in-memory form remains usable.
    }
    return null;
  }
}

export function writeRoutineDraft(userId: string, draft: RoutineDraft): StoredRoutineDraft | null {
  const storage = getLocalStorage();
  if (!storage) return null;

  const storedDraft: StoredRoutineDraft = {
    version: ROUTINE_DRAFT_STORAGE_VERSION,
    updatedAt: new Date().toISOString(),
    draft,
  };

  try {
    storage.setItem(getRoutineDraftKey(userId), JSON.stringify(storedDraft));
    return storedDraft;
  } catch {
    return null;
  }
}

export function clearRoutineDraft(userId: string): void {
  try {
    getLocalStorage()?.removeItem(getRoutineDraftKey(userId));
  } catch {
    // A storage failure must not interrupt the routine form.
  }
}

export function isRoutineDraftExpired(updatedAt: string, now: number = Date.now()): boolean {
  const updatedAtMs = Date.parse(updatedAt);
  return Number.isNaN(updatedAtMs) || now - updatedAtMs > ROUTINE_DRAFT_TTL_MS;
}

export function hasMeaningfulRoutineDraft(draft: RoutineDraft): boolean {
  const initialDay = draft.dias[0];

  return draft.nombre.trim().length > 0 ||
    Boolean(draft.descripcion?.trim()) ||
    draft.dias.length !== 1 ||
    !initialDay ||
    initialDay.nombre !== "Día 1" ||
    Boolean(initialDay.descripcion?.trim()) ||
    initialDay.orden !== 1 ||
    draft.dias.some((day) => day.exercises.length > 0);
}

export function serializeRoutineDraft(draft: RoutineDraft): string {
  return JSON.stringify(draft);
}

export function formatRoutineDraftUpdatedAt(updatedAt: string, now: Date = new Date()): string {
  const updated = new Date(updatedAt);
  if (Number.isNaN(updated.getTime())) return "Fecha no disponible";

  const dayDifference = getLocalCalendarDayDifference(updated, now);
  const time = new Intl.DateTimeFormat("es-AR", {
    hour: "2-digit",
    minute: "2-digit",
  }).format(updated);

  if (dayDifference <= 0) return `Hoy · ${time}`;
  if (dayDifference === 1) return `Ayer · ${time}`;

  const date = new Intl.DateTimeFormat("es-AR", {
    day: "2-digit",
    month: "short",
    year: updated.getFullYear() === now.getFullYear() ? undefined : "numeric",
  }).format(updated);

  return `${date} · ${time}`;
}

function getLocalStorage(): Storage | null {
  if (typeof window === "undefined") return null;

  try {
    return window.localStorage;
  } catch {
    return null;
  }
}

function isStoredRoutineDraft(value: unknown): value is StoredRoutineDraft {
  if (!isRecord(value) || value.version !== ROUTINE_DRAFT_STORAGE_VERSION || typeof value.updatedAt !== "string") {
    return false;
  }

  return !Number.isNaN(Date.parse(value.updatedAt)) && isRoutineDraft(value.draft);
}

function isRoutineDraft(value: unknown): value is RoutineDraft {
  return isRecord(value) &&
    typeof value.nombre === "string" &&
    isNullableString(value.descripcion) &&
    Array.isArray(value.dias) &&
    value.dias.length > 0 &&
    value.dias.every(isRoutineDraftDay) &&
    new Set(value.dias.map((day) => day.id)).size === value.dias.length;
}

function isRoutineDraftDay(value: unknown): value is RoutineDraftDay {
  return isRecord(value) &&
    typeof value.id === "string" && value.id.length > 0 &&
    typeof value.nombre === "string" &&
    isNullableString(value.descripcion) &&
    isPositiveInteger(value.orden) &&
    Array.isArray(value.exercises) &&
    value.exercises.every(isRoutineDraftExercise) &&
    new Set(value.exercises.map((exercise) => exercise.exerciseId)).size === value.exercises.length;
}

function isRoutineDraftExercise(value: unknown): value is RoutineDraftExercise {
  return isRecord(value) &&
    typeof value.exerciseId === "string" && value.exerciseId.length > 0 &&
    typeof value.nombre === "string" &&
    (value.grupoMuscularPrincipal === undefined || typeof value.grupoMuscularPrincipal === "string") &&
    (value.equipamientos === undefined || (
      Array.isArray(value.equipamientos) && value.equipamientos.every((equipment) =>
        isRecord(equipment) &&
        typeof equipment.id === "string" &&
        typeof equipment.nombre === "string" &&
        typeof equipment.categoria === "string",
      )
    )) &&
    isPositiveInteger(value.cantidadSeries) &&
    isPositiveInteger(value.repeticionesMinimas) &&
    isPositiveInteger(value.repeticionesMaximas) &&
    isNonNegativeInteger(value.rirObjetivoMinimo) &&
    isNonNegativeInteger(value.rirObjetivoMaximo) &&
    isPositiveInteger(value.descansoSegundos) &&
    isNullableString(value.notas);
}

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === "object" && value !== null && !Array.isArray(value);
}

function isNullableString(value: unknown): value is string | null {
  return value === null || typeof value === "string";
}

function isPositiveInteger(value: unknown): value is number {
  return typeof value === "number" && Number.isInteger(value) && value > 0;
}

function isNonNegativeInteger(value: unknown): value is number {
  return typeof value === "number" && Number.isInteger(value) && value >= 0;
}

function getLocalCalendarDayDifference(first: Date, second: Date): number {
  const firstDay = Date.UTC(first.getFullYear(), first.getMonth(), first.getDate());
  const secondDay = Date.UTC(second.getFullYear(), second.getMonth(), second.getDate());
  return Math.floor((secondDay - firstDay) / (24 * 60 * 60 * 1000));
}
