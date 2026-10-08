import type { Metadata } from "next";
import { ExerciseProgressContent } from "@/features/progress/components/exercise-progress-content";

export const metadata: Metadata = {
  title: "Progreso | Entrenate",
};

export default async function ExerciseProgressPage({ params }: { params: Promise<{ id: string }> }) {
  const { id } = await params;
  return <ExerciseProgressContent exerciseId={id} />;
}
