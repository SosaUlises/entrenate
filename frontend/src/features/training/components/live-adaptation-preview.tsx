import { ChevronDown } from "lucide-react";
import type { LiveAdaptationPreview, LiveAdaptationTarget } from "@/features/coach/types/live-coach.types";

const statusLabels = {
  retained: "Se conserva",
  completed: "Completado",
  newlyOmitted: "Se omitiría",
  alreadyOmitted: "Ya omitido",
} satisfies Record<LiveAdaptationPreview["exercises"][number]["status"], string>;

function range(min: number, max: number) {
  return min === max ? String(min) : `${min}–${max}`;
}

function targetLabel(target: LiveAdaptationTarget) {
  return `${range(target.repMin, target.repMax)} reps · RIR ${range(target.rirMin, target.rirMax)}`;
}

function setsLabel(count: number) {
  return count === 1 ? "1 serie pendiente" : `${count} series pendientes`;
}

export function LiveAdaptationPreviewContent({ preview }: { preview: LiveAdaptationPreview }) {
  const omitted = preview.exercises.filter((exercise) => exercise.status === "newlyOmitted");
  const change = preview.prescriptionChange;

  return (
    <>
      <h4 className="font-brand text-lg font-bold leading-6 text-text-primary">{preview.title}</h4>
      <p className="mt-2 text-sm leading-6 text-text-secondary">{preview.explanation}</p>
      <p className="mt-3 text-sm font-semibold tabular-nums text-text-primary">
        {setsLabel(preview.pendingSets)}
        {preview.minutesBefore !== null && preview.minutesAfter !== null
          ? ` · ${preview.minutesBefore} → ${preview.minutesAfter} min estimados`
          : null}
      </p>
      {omitted.length > 0 ? (
        <p className="mt-2 text-sm leading-6 text-text-secondary">
          <span className="font-semibold text-text-primary">Se omitirían:</span>{" "}
          {omitted.map((exercise) => exercise.name).join(", ")}.
        </p>
      ) : null}
      {change ? (
        <div className="mt-3 space-y-1 text-sm leading-5 text-text-secondary">
          <p className="font-semibold text-text-primary">{change.exerciseName} · Desde la serie {change.fromSet}</p>
          <p>Antes · {targetLabel(change.before)}</p>
          <p>Nuevo objetivo · {targetLabel(change.after)}</p>
        </div>
      ) : null}
      <details className="group mt-3 border-y border-border/60">
        <summary className="flex min-h-11 cursor-pointer list-none items-center justify-between gap-3 rounded-control-sm text-sm font-semibold text-text-primary focus-visible:outline-focus [&::-webkit-details-marker]:hidden">
          Ver cómo quedaría el entrenamiento
          <ChevronDown aria-hidden="true" className="shrink-0 group-open:rotate-180" size={16} />
        </summary>
        <ul className="divide-y divide-border/60 pb-2">
          {preview.exercises.map((exercise) => (
            <li className="py-2.5 text-sm leading-5" key={exercise.id}>
              <p className="font-semibold text-text-primary">{exercise.name}</p>
              <p className="mt-1 text-text-secondary">
                {statusLabels[exercise.status]}
                {exercise.status === "retained"
                  ? ` · ${setsLabel(exercise.pendingSets)} · ${targetLabel(exercise.target)} · ${exercise.restSeconds} s de descanso`
                  : null}
              </p>
            </li>
          ))}
        </ul>
      </details>
    </>
  );
}
