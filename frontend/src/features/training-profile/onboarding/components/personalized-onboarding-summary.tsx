import { getCurrentUser } from "@/features/auth/services/auth.service";
import { getAuthToken } from "@/features/auth/services/session.service";
import { getEquipment } from "../services/equipment.service";
import type { Equipment } from "../types/equipment.types";
import { OnboardingSummary } from "./onboarding-summary";

export async function PersonalizedOnboardingSummary() {
  const authToken = await getAuthToken();

  if (!authToken) {
    return <OnboardingSummary equipment={[]} />;
  }

  const [currentUserResult, equipmentResult] = await Promise.allSettled([
    getCurrentUser(authToken),
    getEquipment(authToken),
  ]);
  const nombre =
    currentUserResult.status === "fulfilled"
      ? currentUserResult.value.nombre
      : undefined;
  const equipment: Equipment[] =
    equipmentResult.status === "fulfilled" ? equipmentResult.value : [];

  return <OnboardingSummary equipment={equipment} nombre={nombre} />;
}
