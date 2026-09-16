"use client";

import { useRouter } from "next/navigation";
import Link from "next/link";
import { ExerciseCatalog } from "@/features/exercises/components/exercise-catalog";
import { useRoutineDraft } from "../context/routine-draft-context";

export function RoutineExercisePicker({ dayId }: { dayId: string | undefined }) {
  const router = useRouter();
  const { dias, setDayExercises } = useRoutineDraft();
  const day = dias.find((item) => item.id === dayId);

  if (!day) {
    return (
      <div className="mx-auto w-full max-w-xl rounded-card border border-border bg-surface p-5">
        <p className="text-sm text-text-primary">No encontramos el día de entrenamiento.</p>
        <Link className="mt-4 inline-flex min-h-11 items-center text-sm font-semibold text-primary focus-visible:outline-primary" href="/routines/new">
          Volver a nueva rutina
        </Link>
      </div>
    );
  }

  return (
    <ExerciseCatalog
      key={day.id}
      backHref="/routines/new"
      backLabel="Volver a nueva rutina"
      description={`Elegí los ejercicios que querés sumar a ${day.nombre}.`}
      initialSelectedIds={day.exercises.map((exercise) => exercise.exerciseId)}
      onConfirmSelection={(selected) => {
        setDayExercises(day.id, selected);
        router.push("/routines/new");
      }}
      title="Agregar ejercicios"
    />
  );
}
