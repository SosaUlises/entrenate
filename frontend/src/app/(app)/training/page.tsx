import type { Metadata } from "next";
import { ActiveTrainingContent } from "@/features/training/components/active-training-content";

export const metadata: Metadata = { title: "Entrenamiento en curso | Entrenate" };

export default function TrainingPage() {
  return <ActiveTrainingContent />;
}
