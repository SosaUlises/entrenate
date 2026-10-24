import type { Metadata } from "next";
import { redirect } from "next/navigation";
import { ProgressCoachExerciseContent } from "@/features/coach/components/progress-coach-exercise-content";
import { getExercisesAction } from "@/features/exercises/actions/get-exercises.action";

export const metadata: Metadata = {
  title: "An\u00e1lisis de progreso | Entrenate",
};

export default async function ProgressCoachExercisePage({
  params,
}: {
  params: Promise<{ exerciseId: string }>;
}) {
  const { exerciseId } = await params;
  const result = await getExercisesAction();

  if (result.status === "unauthenticated") {
    redirect("/login");
  }

  const exercise =
    result.status === "ready"
      ? result.exercises.find((item) => item.id === exerciseId)
      : undefined;

  return (
    <ProgressCoachExerciseContent
      exercise={exercise}
      exerciseId={exerciseId}
      loadFailed={result.status === "error"}
    />
  );
}
