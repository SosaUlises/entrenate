"use client";

import { CircleHelp, Minus, Plus, X } from "lucide-react";
import { useEffect, useId, useRef, useState, type FormEvent } from "react";
import { Button } from "@/components/ui/button";
import {
  validateRoutineExerciseConfig,
  type RoutineDraftExercise,
  type RoutineExerciseConfig,
  type RoutineExerciseErrors,
} from "../types/routine-draft-exercise";

type NumericField = "repeticionesMinimas" | "repeticionesMaximas" | "rirObjetivoMinimo" | "rirObjetivoMaximo" | "descansoSegundos";

const helpText = {
  Series: "Cantidad de veces que vas a realizar un grupo de repeticiones del ejercicio.",
  Repeticiones: "Cantidad de veces que realizás el movimiento dentro de cada serie.",
  RIR: "Repeticiones en reserva: indica cuántas repeticiones más sentís que podrías hacer antes de llegar al fallo.",
  Descanso: "Tiempo de recuperación entre una serie y la siguiente.",
} as const;

export function RoutineExerciseEditor({
  exercise, onSave, onClose,
}: {
  exercise: RoutineDraftExercise;
  onSave: (config: RoutineExerciseConfig) => void;
  onClose: () => void;
}) {
  const dialogRef = useRef<HTMLDialogElement>(null);
  const id = useId();
  const [series, setSeries] = useState(exercise.cantidadSeries);
  const [values, setValues] = useState<Record<NumericField, string>>({
    repeticionesMinimas: String(exercise.repeticionesMinimas),
    repeticionesMaximas: String(exercise.repeticionesMaximas),
    rirObjetivoMinimo: String(exercise.rirObjetivoMinimo),
    rirObjetivoMaximo: String(exercise.rirObjetivoMaximo),
    descansoSegundos: String(exercise.descansoSegundos),
  });
  const [notas, setNotas] = useState(exercise.notas ?? "");
  const [errors, setErrors] = useState<RoutineExerciseErrors>({});

  useEffect(() => {
    const dialog = dialogRef.current;
    if (dialog && !dialog.open) dialog.showModal();
    return () => dialog?.close();
  }, []);

  const setNumeric = (field: NumericField, value: string) => {
    setValues((current) => ({ ...current, [field]: value }));
    setErrors((current) => ({ ...current, [field]: undefined }));
  };

  const save = (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    const parse = (value: string) => value.trim() === "" ? Number.NaN : Number(value);
    const config: RoutineExerciseConfig = {
      cantidadSeries: series,
      repeticionesMinimas: parse(values.repeticionesMinimas),
      repeticionesMaximas: parse(values.repeticionesMaximas),
      rirObjetivoMinimo: parse(values.rirObjetivoMinimo),
      rirObjetivoMaximo: parse(values.rirObjetivoMaximo),
      descansoSegundos: parse(values.descansoSegundos),
      notas: notas.trim() || null,
    };
    const nextErrors = validateRoutineExerciseConfig(config);
    setErrors(nextErrors);
    if (Object.keys(nextErrors).length > 0) return;
    onSave(config);
  };

  return (
    <dialog
      aria-labelledby={`${id}-title`}
      aria-modal="true"
      className="fixed inset-x-0 top-auto bottom-0 m-0 max-h-[calc(100dvh-1rem)] w-full max-w-none overflow-y-auto rounded-t-card border border-border bg-surface-elevated p-0 text-text-primary shadow-elevated backdrop:bg-black/75 sm:inset-0 sm:m-auto sm:max-h-[calc(100dvh-3rem)] sm:max-w-lg sm:rounded-card"
      onClick={(event) => {
        const bounds = event.currentTarget.getBoundingClientRect();
        if (event.clientX < bounds.left || event.clientX > bounds.right || event.clientY < bounds.top || event.clientY > bounds.bottom) {
          event.currentTarget.close();
        }
      }}
      onClose={() => {
        // React may close and reopen the dialog during its development effect check.
        // Ignore the queued close event if this dialog is open again.
        if (dialogRef.current && !dialogRef.current.open) onClose();
      }}
      ref={dialogRef}
    >
      <form className="space-y-5 p-5 pb-[calc(1.25rem+env(safe-area-inset-bottom))] sm:p-6" noValidate onSubmit={save}>
        <div className="flex items-start justify-between gap-4">
          <div>
            <h2 className="font-brand text-xl font-bold text-text-primary" id={`${id}-title`}>{exercise.nombre}</h2>
            {exercise.grupoMuscularPrincipal ? <p className="mt-1 text-sm text-primary">{exercise.grupoMuscularPrincipal}</p> : null}
          </div>
          <button
            aria-label="Cerrar sin guardar"
            className="flex size-11 shrink-0 items-center justify-center rounded-control-sm text-text-secondary hover:bg-surface hover:text-text-primary focus-visible:outline-primary"
            onClick={() => dialogRef.current?.close()}
            type="button"
          >
            <X aria-hidden="true" size={20} />
          </button>
        </div>

        <div>
          <FieldHeading label="Series" text={helpText.Series} />
          <div className="mt-2 flex items-center gap-3">
            <button aria-label="Quitar una serie" className="flex size-11 items-center justify-center rounded-control-sm border border-border bg-surface text-text-primary disabled:opacity-40 focus-visible:outline-primary" disabled={series <= 1} onClick={() => setSeries((value) => Math.max(1, value - 1))} type="button"><Minus aria-hidden="true" size={18} /></button>
            <output aria-label="Cantidad de series" className="min-w-8 text-center text-base font-semibold">{series}</output>
            <button aria-label="Agregar una serie" className="flex size-11 items-center justify-center rounded-control-sm border border-border bg-surface text-text-primary disabled:opacity-40 focus-visible:outline-primary" disabled={series >= 20} onClick={() => setSeries((value) => Math.min(20, value + 1))} type="button"><Plus aria-hidden="true" size={18} /></button>
          </div>
        </div>

        <div>
          <FieldHeading label="Repeticiones" text={helpText.Repeticiones} />
          <div className="mt-2 flex items-center gap-3">
            <NumberInput error={errors.repeticionesMinimas} id={`${id}-reps-min`} label="Repeticiones mínimas" max={100} min={1} onChange={(value) => setNumeric("repeticionesMinimas", value)} value={values.repeticionesMinimas} />
            <span aria-hidden="true" className="text-text-secondary">—</span>
            <NumberInput error={errors.repeticionesMaximas} id={`${id}-reps-max`} label="Repeticiones máximas" max={100} min={1} onChange={(value) => setNumeric("repeticionesMaximas", value)} value={values.repeticionesMaximas} />
          </div>
        </div>

        <div>
          <FieldHeading label="RIR" text={helpText.RIR} />
          <div className="mt-2 flex items-center gap-3">
            <NumberInput error={errors.rirObjetivoMinimo} id={`${id}-rir-min`} label="RIR mínimo" max={5} min={0} onChange={(value) => setNumeric("rirObjetivoMinimo", value)} value={values.rirObjetivoMinimo} />
            <span aria-hidden="true" className="text-text-secondary">—</span>
            <NumberInput error={errors.rirObjetivoMaximo} id={`${id}-rir-max`} label="RIR máximo" max={5} min={0} onChange={(value) => setNumeric("rirObjetivoMaximo", value)} value={values.rirObjetivoMaximo} />
          </div>
        </div>

        <div>
          <FieldHeading label="Descanso" text={helpText.Descanso} />
          <div className="mt-2 flex items-center gap-3">
            <NumberInput error={errors.descansoSegundos} id={`${id}-rest`} label="Descanso en segundos" max={900} min={15} onChange={(value) => setNumeric("descansoSegundos", value)} value={values.descansoSegundos} />
            <span className="text-sm text-text-secondary">segundos</span>
          </div>
        </div>

        <div>
          <label className="text-sm font-semibold text-text-primary" htmlFor={`${id}-notes`}>Notas</label>
          <textarea
            aria-invalid={Boolean(errors.notas)}
            className="mt-2 min-h-20 w-full resize-y rounded-control-sm border border-border bg-surface p-3 text-sm text-text-primary outline-none placeholder:text-text-secondary/70 focus:border-primary focus-visible:outline-primary"
            id={`${id}-notes`}
            maxLength={500}
            onChange={(event) => setNotas(event.target.value)}
            placeholder="Ej. priorizar técnica, agarre neutro..."
            value={notas}
          />
          {errors.notas ? <p className="mt-1 text-xs text-error" role="alert">{errors.notas}</p> : null}
        </div>

        <Button className="w-full" type="submit">Guardar cambios</Button>
      </form>
    </dialog>
  );
}

function FieldHeading({ label, text }: { label: string; text: string }) {
  const [open, setOpen] = useState(false);
  const id = useId();
  return (
    <div className="relative flex items-center gap-1.5" onBlur={(event) => {
      if (!event.currentTarget.contains(event.relatedTarget)) setOpen(false);
    }} onMouseEnter={() => setOpen(true)} onMouseLeave={() => setOpen(false)}>
      <span className="text-sm font-semibold text-text-primary">{label}</span>
      <button
        aria-controls={open ? id : undefined}
        aria-describedby={open ? id : undefined}
        aria-expanded={open}
        aria-label={`Ayuda sobre ${label}`}
        className="flex size-8 items-center justify-center rounded-full text-text-secondary hover:text-primary focus-visible:outline-primary"
        onClick={() => setOpen(true)}
        onFocus={() => setOpen(true)}
        type="button"
      >
        <CircleHelp aria-hidden="true" size={16} />
      </button>
      {open ? (
        <div className="absolute top-full left-0 z-20 w-64 max-w-[calc(100vw-2.5rem)] rounded-control-sm border border-border bg-surface p-3 text-xs leading-5 text-text-primary shadow-elevated" id={id} role="tooltip">
          {text}
          <button aria-label={`Cerrar ayuda sobre ${label}`} className="ml-2 inline-flex min-h-6 items-center text-primary focus-visible:outline-primary" onClick={() => setOpen(false)} type="button">Cerrar</button>
        </div>
      ) : null}
    </div>
  );
}

function NumberInput({ id, label, value, min, max, error, onChange }: {
  id: string;
  label: string;
  value: string;
  min: number;
  max: number;
  error?: string;
  onChange: (value: string) => void;
}) {
  return (
    <div className="max-w-28 min-w-0 flex-1">
      <label className="sr-only" htmlFor={id}>{label}</label>
      <input
        aria-describedby={error ? `${id}-error` : undefined}
        aria-invalid={Boolean(error)}
        className="h-11 w-full rounded-control-sm border border-border bg-surface px-3 text-center text-sm text-text-primary outline-none focus:border-primary focus-visible:outline-primary"
        id={id}
        inputMode="numeric"
        max={max}
        min={min}
        onChange={(event) => onChange(event.target.value)}
        step={1}
        type="number"
        value={value}
      />
      {error ? <p className="mt-1 text-xs text-error" id={`${id}-error`} role="alert">{error}</p> : null}
    </div>
  );
}
