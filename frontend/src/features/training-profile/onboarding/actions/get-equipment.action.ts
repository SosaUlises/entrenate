"use server";

import { getAuthToken } from "@/features/auth/services/session.service";
import type { Equipment } from "../types/equipment.types";
import { getEquipment } from "../services/equipment.service";

export type GetEquipmentActionResult =
  | { equipment: Equipment[]; ok: true }
  | { ok: false };

export async function getEquipmentAction(): Promise<GetEquipmentActionResult> {
  const authToken = await getAuthToken();

  if (!authToken) {
    return { ok: false };
  }

  try {
    const equipment = await getEquipment(authToken);

    return { equipment, ok: true };
  } catch {
    return { ok: false };
  }
}
