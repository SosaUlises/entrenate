import { apiRequest } from "@/services/api-client";
import type { Equipment } from "../types/equipment.types";

export async function getEquipment(authToken: string): Promise<Equipment[]> {
  return apiRequest<Equipment[]>("/api/equipment", {
    authToken,
    cache: "no-store",
    method: "GET",
  });
}
