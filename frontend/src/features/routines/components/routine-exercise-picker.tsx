"use client";

import { useRouter } from "next/navigation";
import Link from "next/link";
import { ExerciseCatalog } from "@/features/exercises/components/exercise-catalog";
import { useRoutineDraft } from "../context/routine-draft-context";

export function RoutineExercisePicker({ dayId, returnHref = "/routines/new" }: { dayId: string | undefined; returnHref?: string }) {
  const router = useRouter();
  const { dias, setDayExercises } = useRoutineDraft();
  const day = dias.find((item) => item.id === dayId);
  const backLabel = returnHref === "/routines/new" ? "Volver a nueva rutina" : "Volver a editar rutina";

  if (!day) {
    return (
      <div className="mx-auto w-full max-w-xl rounded-card border border-border bg-surface p-5">
        <p className="text-sm text-text-primary">No encontramos el día de entrenamiento.</p>
        <Link className="mt-4 inline-flex min-h-11 items-center text-sm font-semibold text-primary focus-visible:outline-primary" href={returnHref}>
          {backLabel}
        </Link>
      </div>
    );
  }

  return (
    <ExerciseCatalog
      key={day.id}
      backHref={returnHref}
      backLabel={backLabel}
      description={`Elegí los ejercicios que querés sumar a ${day.nombre}.`}
      initialSelectedIds={day.exercises.map((exercise) => exercise.exerciseId)}
      onConfirmSelection={(selected) => {
        setDayExercises(day.id, selected);
        router.push(returnHref);
      }}
      title="Agregar ejercicios"
    />
  );
}
