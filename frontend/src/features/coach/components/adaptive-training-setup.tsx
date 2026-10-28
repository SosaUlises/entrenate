"use client";

import { ArrowLeft, Check, RotateCcw, SlidersHorizontal } from "lucide-react";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { useCallback, useEffect, useMemo, useState } from "react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { useTrainingProfileGate } from "@/features/training-profile/gate/training-profile-gate";
import {
  equipmentCategoryDefinitions,
} from "@/features/training-profile/shared/training-profile-options";
import { cn } from "@/lib/class-names";
import {
  getAdaptiveTrainingSetupAction,
  type AdaptiveTrainingSetupData,
  type GetAdaptiveTrainingSetupActionResult,
} from "../actions/get-adaptive-training-setup.action";
import type {
  SessionAdaptationRequest,
  SessionEffortPreference,
} from "../types/adaptive-training.types";

type SetupState =
  | { status: "error" }
  | { status: "loading" }
  | { status: "not-found" }
  | { data: AdaptiveTrainingSetupData; status: "ready" };

export function AdaptiveTrainingSetup({
  routineDayId,
  routineId,
}: {
  routineDayId: string;
  routineId: string;
}) {
  const router = useRouter();
  const { invalidateSession, recordPresence } = useTrainingProfileGate();
  const [state, setState] = useState<SetupState>({ status: "loading" });
  const [requestKey, setRequestKey] = useState(0);

  const handleResult = useCallback((result: GetAdaptiveTrainingSetupActionResult) => {
    if (result.status === "unauthenticated") {
      invalidateSession();
      router.replace("/login");
      return;
    }

    if (result.status === "missing-profile") {
      recordPresence("missing");
      router.replace("/onboarding");
      return;
    }

    setState(result);
  }, [invalidateSession, recordPresence, router]);

  useEffect(() => {
    let active = true;

    void getAdaptiveTrainingSetupAction(routineId, routineDayId).then(
      (result) => {
        if (active) handleResult(result);
      },
      () => {
        if (active) setState({ status: "error" });
      },
    );

    return () => {
      active = false;
    };
  }, [handleResult, requestKey, routineDayId, routineId]);

  return (
    <section
      aria-busy={state.status === "loading"}
      aria-labelledby="adaptive-training-title"
      className="mx-auto w-full min-w-0 max-w-xl"
    >
      <header>
        <div className="flex min-w-0 items-center gap-1">
          <Link
            aria-label="Volver al detalle de la rutina"
            className="flex size-11 shrink-0 items-center justify-center rounded-control-sm text-text-secondary transition-colors duration-150 hover:bg-surface-interactive hover:text-text-primary focus-visible:outline-2 focus-visible:outline-focus motion-reduce:transition-none"
            href={`/routines/${encodeURIComponent(routineId)}`}
          >
            <ArrowLeft aria-hidden="true" size={19} />
          </Link>
          <h1
            className="min-w-0 font-brand text-2xl font-bold leading-tight text-text-primary"
            id="adaptive-training-title"
          >
            Adaptar entrenamiento
          </h1>
        </div>
        <p className="mt-1.5 max-w-lg pl-12 text-sm leading-6 text-text-secondary">
          Configur&aacute; las condiciones de hoy sin cambiar tu rutina base.
        </p>
      </header>

      {state.status === "loading" ? (
        <AdaptiveTrainingSetupSkeleton />
      ) : state.status === "error" ? (
        <SetupError
          onRetry={() => {
            setState({ status: "loading" });
            setRequestKey((key) => key + 1);
          }}
        />
      ) : state.status === "not-found" ? (
        <div className="mt-8 border-y border-border/70 py-6">
          <p className="text-sm leading-6 text-text-primary">
            No encontramos el d&iacute;a de rutina que quer&eacute;s adaptar.
          </p>
          <Link
            className="mt-4 inline-flex min-h-11 items-center text-sm font-semibold text-text-primary focus-visible:outline-2 focus-visible:outline-focus"
            href={`/routines/${encodeURIComponent(routineId)}`}
          >
            Volver a la rutina
          </Link>
        </div>
      ) : !state.data.routine.activa ? (
        <div className="mt-8 border-y border-border/70 py-6">
          <p className="text-sm leading-6 text-text-primary">
            Solo pod&eacute;s adaptar un d&iacute;a de tu rutina activa.
          </p>
          <Link
            className="mt-4 inline-flex min-h-11 items-center text-sm font-semibold text-text-primary focus-visible:outline-2 focus-visible:outline-focus"
            href={`/routines/${encodeURIComponent(routineId)}`}
          >
            Volver a la rutina
          </Link>
        </div>
      ) : (
        <AdaptiveTrainingForm data={state.data} />
      )}
    </section>
  );
}

function AdaptiveTrainingForm({ data }: { data: AdaptiveTrainingSetupData }) {
  const habitualEquipmentIds = useMemo(
    () => data.profile.equipamientos.map((item) => item.id),
    [data.profile.equipamientos],
  );
  const activeEquipmentIds = useMemo(
    () => new Set(data.equipment.map((item) => item.id)),
    [data.equipment],
  );
  const availableHabitualIds = useMemo(
    () => habitualEquipmentIds.filter((id) => activeEquipmentIds.has(id)),
    [activeEquipmentIds, habitualEquipmentIds],
  );
  const [availableMinutes, setAvailableMinutes] = useState<number | null>(null);
  const [availableEquipmentIds, setAvailableEquipmentIds] = useState<string[] | null>(null);
  const [excludedExerciseIds, setExcludedExerciseIds] = useState<string[]>([]);
  const [effortPreference, setEffortPreference] = useState<SessionEffortPreference>("Normal");

  const exercises = useMemo(() => {
    const byId = new Map<string, (typeof data.day.ejercicios)[number]>();

    [...data.day.ejercicios]
      .sort((first, second) => first.orden - second.orden)
      .forEach((exercise) => {
        if (!byId.has(exercise.ejercicioId)) byId.set(exercise.ejercicioId, exercise);
      });

    return [...byId.values()];
  }, [data]);

  const request: SessionAdaptationRequest = useMemo(() => ({
    availableEquipmentIds,
    availableMinutes,
    effortPreference,
    excludedExerciseIds,
    routineDayId: data.day.id,
  }), [availableEquipmentIds, availableMinutes, data.day.id, effortPreference, excludedExerciseIds]);
  const hasInvalidMinutes = request.availableMinutes !== null && (
    !Number.isInteger(request.availableMinutes) ||
    request.availableMinutes < 15 ||
    request.availableMinutes > 240
  );

  const toggleAvailableEquipment = (equipmentId: string) => {
    setAvailableEquipmentIds((current) => {
      const selected = current ?? availableHabitualIds;

      return selected.includes(equipmentId)
        ? selected.filter((id) => id !== equipmentId)
        : [...selected, equipmentId];
    });
  };

  const toggleExcludedExercise = (exerciseId: string) => {
    setExcludedExerciseIds((current) => current.includes(exerciseId)
      ? current.filter((id) => id !== exerciseId)
      : [...current, exerciseId]);
  };

  const equipmentSummary = request.availableEquipmentIds === null
    ? "Equipamiento habitual"
    : request.availableEquipmentIds.length === 0
      ? "Sin equipamiento hoy"
      : `${request.availableEquipmentIds.length} ${request.availableEquipmentIds.length === 1 ? "elemento disponible" : "elementos disponibles"}`;

  return (
    <div className="mt-8">
      <div className="border-y border-border/70 py-4">
        <p className="font-brand text-lg font-bold leading-6 text-text-primary">
          {data.day.nombre}
        </p>
        <p className="mt-1 text-[0.8125rem] font-semibold leading-5 text-text-secondary">
          {data.routine.nombre} &middot; {data.day.ejercicios.length} {data.day.ejercicios.length === 1 ? "ejercicio" : "ejercicios"}
        </p>
      </div>

      <div className="mt-8 space-y-8">
        <fieldset>
          <legend className="font-brand text-lg font-bold leading-6 text-text-primary">
            Tiempo disponible
          </legend>
          <p className="mt-1 text-sm leading-6 text-text-secondary">
            Indic&aacute; el m&aacute;ximo que ten&eacute;s hoy, no una duraci&oacute;n obligatoria.
          </p>
          <div className="mt-3">
            <label className="relative block">
              <span className="sr-only">Minutos disponibles hoy</span>
              <Input
                aria-describedby="available-minutes-help"
                aria-invalid={hasInvalidMinutes}
                className="pr-14 text-[0.9375rem] font-semibold tabular-nums"
                hasError={hasInvalidMinutes}
                inputMode="numeric"
                max={240}
                min={15}
                onChange={(event) => setAvailableMinutes(event.target.value === "" ? null : Number(event.target.value))}
                placeholder="Sin límite especial"
                step={1}
                type="number"
                value={availableMinutes ?? ""}
              />
              {availableMinutes !== null ? (
                <span className="pointer-events-none absolute right-4 top-1/2 -translate-y-1/2 text-[0.8125rem] font-semibold text-text-muted">
                  min
                </span>
              ) : null}
            </label>
            <div className="mt-2 flex min-h-8 flex-wrap items-start justify-between gap-x-4 gap-y-1">
              <p
                className={cn("text-[0.8125rem] leading-5", hasInvalidMinutes ? "text-error" : "text-text-muted")}
                id="available-minutes-help"
              >
                {hasInvalidMinutes ? "Ingresá un valor entero entre 15 y 240 minutos." : "Entre 15 y 240 minutos, o sin límite especial."}
              </p>
              {availableMinutes !== null ? (
                <button
                  className="min-h-8 text-[0.8125rem] font-semibold text-text-secondary hover:text-text-primary focus-visible:outline-2 focus-visible:outline-focus"
                  onClick={() => setAvailableMinutes(null)}
                  type="button"
                >
                  Sin l&iacute;mite especial
                </button>
              ) : null}
            </div>
          </div>
        </fieldset>

        <fieldset className="border-t border-border/70 pt-8">
          <legend className="font-brand text-lg font-bold leading-6 text-text-primary">
            Equipamiento disponible hoy
          </legend>
          <p className="mt-1 text-sm leading-6 text-text-secondary">
            Por defecto usamos el equipamiento habitual de tu perfil.
          </p>

          <div className="mt-3 flex min-h-14 items-center justify-between gap-4 border-y border-border/70 px-1 py-2">
            <span className="min-w-0 text-sm font-semibold text-text-primary">
              {equipmentSummary}
            </span>
            {availableEquipmentIds === null ? (
              <Button
                className="shrink-0 px-3"
                onClick={() => setAvailableEquipmentIds(availableHabitualIds)}
                size="sm"
                variant="outline"
              >
                <SlidersHorizontal aria-hidden="true" />
                Modificar
              </Button>
            ) : (
              <Button
                className="shrink-0 px-3"
                onClick={() => setAvailableEquipmentIds(null)}
                size="sm"
                variant="ghost"
              >
                <RotateCcw aria-hidden="true" />
                Usar habitual
              </Button>
            )}
          </div>

          {availableEquipmentIds !== null ? (
            <div className="mt-3 space-y-4">
              {equipmentCategoryDefinitions.map((category) => {
                const categoryEquipment = data.equipment.filter((item) => item.categoria === category.value);

                if (categoryEquipment.length === 0) return null;

                return (
                  <div key={category.value}>
                    <p className="mb-1 text-[0.75rem] font-bold uppercase tracking-[0.08em] text-text-muted">
                      {category.label}
                    </p>
                    <div className="border-y border-border/60">
                      {categoryEquipment.map((equipment) => (
                        <SelectionRow
                          checked={availableEquipmentIds.includes(equipment.id)}
                          compact
                          key={equipment.id}
                          label={equipment.nombre}
                          onChange={() => toggleAvailableEquipment(equipment.id)}
                          type="checkbox"
                        />
                      ))}
                    </div>
                  </div>
                );
              })}
              <button
                aria-pressed={availableEquipmentIds.length === 0}
                className={cn(
                  "flex min-h-12 w-full items-center justify-between gap-3 border-y px-1 py-2 text-left text-sm font-semibold transition-colors focus-visible:outline-2 focus-visible:outline-focus",
                  availableEquipmentIds.length === 0
                    ? "border-thermal-accent/45 bg-surface-active/45 text-text-primary"
                    : "border-border/70 text-text-secondary hover:bg-surface-interactive/45 hover:text-text-primary",
                )}
                onClick={() => setAvailableEquipmentIds([])}
                type="button"
              >
                Hoy no tengo equipamiento
                <SelectionIndicator checked={availableEquipmentIds.length === 0} />
              </button>
            </div>
          ) : null}
        </fieldset>

        <fieldset className="border-t border-border/70 pt-8">
          <legend className="font-brand text-lg font-bold leading-6 text-text-primary">
            Ejercicios que prefer&iacute;s no hacer
          </legend>
          <p className="mt-1 text-sm leading-6 text-text-secondary">
            La exclusi&oacute;n aplica solo a esta sesi&oacute;n de hoy.
          </p>
          <div className="mt-3 border-y border-border/70">
            {exercises.map((exercise) => (
              <SelectionRow
                checked={excludedExerciseIds.includes(exercise.ejercicioId)}
                compact
                key={exercise.ejercicioId}
                label={exercise.nombre}
                onChange={() => toggleExcludedExercise(exercise.ejercicioId)}
                type="checkbox"
              />
            ))}
          </div>
          {excludedExerciseIds.length === 0 ? (
            <p className="mt-2 text-[0.8125rem] leading-5 text-text-muted">
              No excluiste ejercicios.
            </p>
          ) : null}
        </fieldset>

        <fieldset className="border-t border-border/70 pt-8">
          <legend className="font-brand text-lg font-bold leading-6 text-text-primary">
            Intensidad
          </legend>
          <p className="mt-1 text-sm leading-6 text-text-secondary">
            Coach usar&aacute; esta preferencia al preparar la sesi&oacute;n.
          </p>
          <div className="mt-3 border-y border-border/70">
            <SelectionRow
              checked={effortPreference === "Normal"}
              description="Mantener la intensidad planificada."
              label="Normal"
              name="adaptive-training-effort"
              onChange={() => setEffortPreference("Normal")}
              type="radio"
            />
            <SelectionRow
              checked={effortPreference === "Lighter"}
              description="Preparar una sesi&oacute;n m&aacute;s liviana."
              label="M&aacute;s liviana"
              name="adaptive-training-effort"
              onChange={() => setEffortPreference("Lighter")}
              type="radio"
            />
          </div>
        </fieldset>
      </div>

      <section aria-label="Contexto preparado" className="mt-7 border-t border-border/60 pt-4">
        <p className="text-[0.6875rem] font-bold uppercase tracking-[0.08em] text-text-muted">
          Condiciones de hoy
        </p>
        <p className="mt-1.5 text-[0.8125rem] leading-5 text-text-muted">
          {request.availableMinutes === null ? "Sin límite especial" : `${request.availableMinutes} min máximo`}
          {" · "}{equipmentSummary}
          {" · "}{request.excludedExerciseIds.length === 0 ? "Sin exclusiones" : `${request.excludedExerciseIds.length} ${request.excludedExerciseIds.length === 1 ? "exclusión" : "exclusiones"}`}
          {" · "}{request.effortPreference === "Normal" ? "Intensidad normal" : "Más liviana"}
        </p>
      </section>
    </div>
  );
}

function SelectionRow({
  checked,
  compact = false,
  description,
  label,
  name,
  onChange,
  type,
}: {
  checked: boolean;
  compact?: boolean;
  description?: string;
  label: string;
  name?: string;
  onChange: () => void;
  type: "checkbox" | "radio";
}) {
  return (
    <label className="block cursor-pointer border-b border-border/50 last:border-b-0">
      <input
        checked={checked}
        className="peer sr-only"
        name={name}
        onChange={onChange}
        type={type}
      />
      <span
        className={cn(
          "flex items-center gap-3 px-1 transition-colors peer-focus-visible:outline-2 peer-focus-visible:outline-focus",
          compact ? "min-h-12 py-2" : "min-h-14 py-2.5",
          checked ? "bg-surface-active/45" : "hover:bg-surface-interactive/40",
        )}
      >
        <span className="min-w-0 flex-1">
          <span className="block text-sm font-semibold text-text-primary">{label}</span>
          {description ? (
            <span className="mt-0.5 block text-[0.8125rem] leading-5 text-text-secondary">
              {description}
            </span>
          ) : null}
        </span>
        <SelectionIndicator checked={checked} />
      </span>
    </label>
  );
}

function SelectionIndicator({ checked }: { checked: boolean }) {
  return (
    <span
      aria-hidden="true"
      className={cn(
        "flex size-6 shrink-0 items-center justify-center rounded-full border transition-[background-color,border-color,color,transform] duration-150 motion-reduce:transition-none",
        checked
          ? "scale-100 border-thermal-accent/60 bg-thermal-accent/15 text-thermal-accent"
          : "scale-95 border-border-strong bg-surface text-transparent",
      )}
    >
      <Check size={13} strokeWidth={2.4} />
    </span>
  );
}

function SetupError({ onRetry }: { onRetry: () => void }) {
  return (
    <div className="mt-8 border-t border-border/70 pt-6" role="alert">
      <p className="text-sm leading-6 text-text-primary">
        No pudimos cargar las condiciones del entrenamiento.
      </p>
      <Button className="mt-4" onClick={onRetry} variant="secondary">
        Reintentar
      </Button>
    </div>
  );
}

function AdaptiveTrainingSetupSkeleton() {
  return (
    <div aria-label="Cargando configuraci&oacute;n" className="mt-8 animate-pulse motion-reduce:animate-none" role="status">
      <div className="h-16 border-y border-border/50 py-4">
        <div className="h-5 w-36 rounded bg-surface-elevated" />
      </div>
      {[0, 1, 2, 3].map((item) => (
        <div className="mt-8" key={item}>
          <div className="h-5 w-44 rounded bg-surface-elevated" />
          <div className="mt-3 h-14 rounded-control-sm bg-surface-elevated" />
        </div>
      ))}
    </div>
  );
}
