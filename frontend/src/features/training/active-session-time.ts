export const STALE_ACTIVE_SESSION_THRESHOLD_MS = 6 * 60 * 60 * 1000;

export function parseActiveSessionStart(horaInicio: string): Date | null {
  const parsed = new Date(horaInicio);
  return Number.isNaN(parsed.getTime()) ? null : parsed;
}

export function isStaleActiveSession(
  horaInicio: string,
  now: number = Date.now(),
): boolean {
  const startedAt = parseActiveSessionStart(horaInicio);
  if (!startedAt) return false;

  return now - startedAt.getTime() >= STALE_ACTIVE_SESSION_THRESHOLD_MS;
}

export function formatActiveSessionStart(
  horaInicio: string,
  now: Date = new Date(),
): string {
  const startedAt = parseActiveSessionStart(horaInicio);
  if (!startedAt) return "Inicio no disponible";

  const calendarDays = getLocalCalendarDayDifference(startedAt, now);
  const time = new Intl.DateTimeFormat("es-AR", {
    hour: "2-digit",
    minute: "2-digit",
  }).format(startedAt);

  if (calendarDays <= 0) return `Iniciado hoy a las ${time}`;
  if (calendarDays === 1) return `Iniciado ayer a las ${time}`;

  return `Iniciado hace ${calendarDays} días`;
}

function getLocalCalendarDayDifference(startedAt: Date, now: Date): number {
  const startDay = Date.UTC(
    startedAt.getFullYear(),
    startedAt.getMonth(),
    startedAt.getDate(),
  );
  const currentDay = Date.UTC(now.getFullYear(), now.getMonth(), now.getDate());

  return Math.floor((currentDay - startDay) / (24 * 60 * 60 * 1000));
}
