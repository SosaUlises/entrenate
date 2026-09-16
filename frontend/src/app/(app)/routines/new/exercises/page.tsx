import type { Metadata } from "next";
import { RoutineExercisePicker } from "@/features/routines/components/routine-exercise-picker";

export const metadata: Metadata = {
  title: "Agregar ejercicios | Entrenate",
};

export default async function RoutineExercisesPage({
  searchParams,
}: {
  searchParams: Promise<{ day?: string }>;
}) {
  const { day } = await searchParams;
  return <RoutineExercisePicker dayId={day} />;
}
