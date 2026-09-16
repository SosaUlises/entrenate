import { apiRequest } from "@/services/api-client";
import type { RoutineDetail, RoutineListItem, RoutineSummary } from "../types/routine.types";

export async function getRoutines(authToken: string): Promise<RoutineListItem[]> {
  const routines = await apiRequest<RoutineSummary[]>("/api/routines", {
    authToken,
    cache: "no-store",
    method: "GET",
  });

  return Promise.all(
    routines.map(async (routine) => {
      const detail = await apiRequest<RoutineDetail>(`/api/routines/${routine.id}`, {
        authToken,
        cache: "no-store",
        method: "GET",
      });

      return {
        ...routine,
        cantidadEjercicios: detail.dias.reduce(
          (total, day) => total + day.ejercicios.length,
          0,
        ),
      };
    }),
  );
}
