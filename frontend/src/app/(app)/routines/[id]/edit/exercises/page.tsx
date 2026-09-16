import type { Metadata } from "next";
import { RoutineExercisePicker } from "@/features/routines/components/routine-exercise-picker";

export const metadata: Metadata = {
  title: "Agregar ejercicios | Entrenate",
};

export default async function EditRoutineExercisesPage({
  params,
  searchParams,
}: {
  params: Promise<{ id: string }>;
  searchParams: Promise<{ day?: string }>;
}) {
  const [{ id }, { day }] = await Promise.all([params, searchParams]);
  return <RoutineExercisePicker dayId={day} returnHref={`/routines/${id}/edit`} />;
}
