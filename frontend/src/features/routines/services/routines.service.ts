import { apiRequest } from "@/services/api-client";
import type {
  CreateRoutineRequest,
  CreateRoutineResponse,
  RoutineDetail,
  RoutineListItem,
  RoutineSummary,
  UpdateRoutineRequest,
} from "../types/routine.types";

export async function createRoutine(
  request: CreateRoutineRequest,
  authToken: string,
): Promise<CreateRoutineResponse> {
  return apiRequest<CreateRoutineResponse, CreateRoutineRequest>("/api/routines", {
    authToken,
    body: request,
    cache: "no-store",
    method: "POST",
  });
}

export async function getRoutineById(id: string, authToken: string): Promise<RoutineDetail> {
  return apiRequest<RoutineDetail>(`/api/routines/${encodeURIComponent(id)}`, {
    authToken,
    cache: "no-store",
    method: "GET",
  });
}

export async function updateRoutine(id: string, request: UpdateRoutineRequest, authToken: string): Promise<void> {
  await apiRequest<void, UpdateRoutineRequest>(`/api/routines/${encodeURIComponent(id)}`, {
    authToken,
    body: request,
    cache: "no-store",
    method: "PUT",
  });
}

export async function getRoutines(authToken: string): Promise<RoutineListItem[]> {
  const routines = await apiRequest<RoutineSummary[]>("/api/routines", {
    authToken,
    cache: "no-store",
    method: "GET",
  });

  return Promise.all(
    routines.map(async (routine) => {
      const detail = await getRoutineById(routine.id, authToken);

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
