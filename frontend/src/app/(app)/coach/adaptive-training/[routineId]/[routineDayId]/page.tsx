import type { Metadata } from "next";
import { AdaptiveTrainingSetup } from "@/features/coach/components/adaptive-training-setup";

export const metadata: Metadata = {
  title: "Adaptar entrenamiento | Entrenate",
};

export default async function AdaptiveTrainingSetupPage({
  params,
}: {
  params: Promise<{ routineDayId: string; routineId: string }>;
}) {
  const { routineDayId, routineId } = await params;

  return (
    <AdaptiveTrainingSetup
      routineDayId={routineDayId}
      routineId={routineId}
    />
  );
}

