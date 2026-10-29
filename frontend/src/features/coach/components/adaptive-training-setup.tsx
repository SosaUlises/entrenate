"use client";

import { ArrowLeft, ArrowRight, Check, RotateCcw, SlidersHorizontal } from "lucide-react";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { useCallback, useEffect, useMemo, useRef, useState } from "react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { useTrainingProfileGate } from "@/features/training-profile/gate/training-profile-gate";
import {
  equipmentCategoryDefinitions,
} from "@/features/training-profile/shared/training-profile-options";
import { cn } from "@/lib/class-names";
import {
  generateSessionAdaptationProposalAction,
} from "../actions/generate-session-adaptation-proposal.action";
import {
  getAdaptiveTrainingSetupAction,
  type AdaptiveTrainingSetupData,
  type GetAdaptiveTrainingSetupActionResult,
} from "../actions/get-adaptive-training-setup.action";
import type {
  SessionAdaptationRequest,
  SessionAdaptationProposalResponse,
  SessionEffortPreference,
} from "../types/adaptive-training.types";
import { AdaptiveTrainingConversationSheet } from "./adaptive-training-conversation-sheet";

type SetupState =
  | { status: "error" }
  | { status: "loading" }
  | { status: "not-found" }
  | { data: AdaptiveTrainingSetupData; status: "ready" };

type ProposalState =
  | { status: "idle" }
  | { request: SessionAdaptationRequest; status: "loading" }
  | { proposal: SessionAdaptationProposalResponse; request: SessionAdaptationRequest; status: "ready" }
  | { request: SessionAdaptationRequest; status: "error" | "validation-error" };

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
  const router = useRouter();
  const { invalidateSession } = useTrainingProfileGate();
  const requestInFlightRef = useRef(false);
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
  const [proposalState, setProposalState] = useState<ProposalState>({ status: "idle" });

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
  const hasAdaptationCondition = request.availableMinutes !== null ||
    request.availableEquipmentIds !== null ||
    request.excludedExerciseIds.length > 0 ||
    request.effortPreference === "Lighter";

  const generateProposal = async (sourceRequest: SessionAdaptationRequest) => {
    if (requestInFlightRef.current) return;

    requestInFlightRef.current = true;
    setProposalState({ request: sourceRequest, status: "loading" });

    try {
      const result = await generateSessionAdaptationProposalAction(sourceRequest);

      if (result.status === "unauthenticated") {
        invalidateSession();
        router.replace("/login");
        return;
      }

      if (result.status === "ready") {
        setProposalState({
          proposal: result.proposal,
          request: sourceRequest,
          status: "ready",
        });
        return;
      }

      setProposalState({ request: sourceRequest, status: result.status });
    } finally {
      requestInFlightRef.current = false;
    }
  };

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

  if (proposalState.status === "loading") {
    return <AdaptiveTrainingProposalSkeleton />;
  }

  if (proposalState.status === "ready") {
    return (
      <AdaptiveTrainingProposalPreview
        onEdit={() => setProposalState({ status: "idle" })}
        onRegenerate={() => generateProposal(proposalState.request)}
        proposal={proposalState.proposal}
        request={proposalState.request}
      />
    );
  }

  if (proposalState.status === "error" || proposalState.status === "validation-error") {
    return (
      <ProposalError
        onEdit={() => setProposalState({ status: "idle" })}
        onRetry={() => void generateProposal(proposalState.request)}
        validation={proposalState.status === "validation-error"}
      />
    );
  }

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

      <div className="mt-6">
        <Button
          disabled={hasInvalidMinutes || !hasAdaptationCondition}
          fullWidth
          onClick={() => void generateProposal(request)}
          size="lg"
        >
          Generar adaptaci&oacute;n
          <ArrowRight aria-hidden="true" />
        </Button>
        {!hasAdaptationCondition ? (
          <p className="mt-2 text-center text-[0.8125rem] leading-5 text-text-muted">
            Indic&aacute; al menos una condici&oacute;n distinta para adaptar la sesi&oacute;n.
          </p>
        ) : null}
      </div>
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

function AdaptiveTrainingProposalPreview({
  onEdit,
  onRegenerate,
  proposal,
  request,
}: {
  onEdit: () => void;
  onRegenerate: () => Promise<void>;
  proposal: SessionAdaptationProposalResponse;
  request: SessionAdaptationRequest;
}) {
  const feasibility = getFeasibilityPresentation(proposal.feasibility);
  const observations = getDiagnosticMessages(proposal.diagnostics);
  const exercises = [...proposal.adaptedExercises].sort((first, second) => first.order - second.order);
  const structuredChanges = getStructuredChangePresentations(proposal);
  const isNotFeasible = proposal.feasibility === "NotFeasible";

  return (
    <div className="mt-8">
      <header className="border-y border-border/70 py-5">
        <h2 className="font-brand text-2xl font-bold leading-tight text-text-primary">
          Tu sesi&oacute;n de hoy
        </h2>
        <div className="mt-2 flex items-center gap-2 text-sm font-semibold text-text-secondary">
          <span>Original</span>
          <ArrowRight aria-hidden="true" className="text-steel-muted" size={15} />
          <span className="text-text-primary">Adaptada</span>
        </div>
      </header>

      <section aria-labelledby="adaptation-summary-title" className="mt-7">
        <div className="flex items-start gap-3">
          <span aria-hidden="true" className="mt-1 h-7 w-px shrink-0 bg-thermal-accent" />
          <div>
            <p className="text-[0.6875rem] font-bold uppercase tracking-[0.08em] text-text-muted">
              Factibilidad
            </p>
            <h3 className="mt-1 font-brand text-[1.375rem] font-extrabold leading-7 text-text-primary" id="adaptation-summary-title">
              {feasibility.label}
            </h3>
            <p className="mt-1 text-sm leading-6 text-text-secondary">
              {feasibility.description}
            </p>
          </div>
        </div>

        <dl className="mt-8 grid grid-cols-2 border-y border-border/70">
          <Metric label="Duraci&oacute;n original" value={formatDuration(proposal.originalDurationSeconds)} />
          <Metric label="Duraci&oacute;n adaptada" value={formatDuration(proposal.adaptedDurationSeconds)} />
          {request.availableMinutes !== null ? (
            <Metric label="L&iacute;mite disponible" value={`${request.availableMinutes} min`} />
          ) : null}
          <Metric
            label="Cambios aplicados"
            value={`${proposal.changes.length}`}
          />
        </dl>
      </section>

      {observations.length > 0 || isNotFeasible ? (
        <section aria-labelledby="adaptation-observations-title" className="mt-8 border-t border-border/70 pt-6">
          <h3 className="font-brand text-lg font-bold leading-6 text-text-primary" id="adaptation-observations-title">
            {isNotFeasible ? "Antes de continuar" : "Cambios y observaciones"}
          </h3>
          <ul className="mt-4 divide-y divide-border/50 border-y border-border/60">
            {(observations.length > 0
              ? observations
              : ["La propuesta no puede confirmarse con las condiciones actuales."]
            ).map((observation) => (
              <li className="py-4 text-sm leading-6 text-text-secondary" key={observation}>
                {observation}
              </li>
            ))}
          </ul>
        </section>
      ) : null}

      {structuredChanges.length > 0 ? (
        <section aria-labelledby="structured-changes-title" className="mt-8 border-t border-border/70 pt-6">
          <h3 className="font-brand text-lg font-bold leading-6 text-text-primary" id="structured-changes-title">
            Antes y despu&eacute;s
          </h3>
          <div className="mt-4 divide-y divide-border/55 border-y border-border/70">
            {structuredChanges.map((change) => (
              <div className="py-5" key={change.key}>
                <p className="text-[0.6875rem] font-bold uppercase tracking-[0.07em] text-text-muted">
                  {change.label}
                </p>
                {change.exerciseName ? (
                  <p className="mt-1.5 font-brand text-[0.9375rem] font-bold leading-6 text-text-primary">
                    {change.exerciseName}
                  </p>
                ) : null}
                <div className="mt-3 grid grid-cols-[minmax(0,1fr)_1.25rem_minmax(0,1fr)] items-center gap-2">
                  <ChangeValue label="Original" value={change.originalValue} />
                  <ArrowRight aria-hidden="true" className="text-thermal-accent" size={16} strokeWidth={1.8} />
                  <ChangeValue emphasized label="Adaptado" value={change.adaptedValue} />
                </div>
                {change.reason ? (
                  <p className="mt-3 text-[0.8125rem] leading-5 text-text-secondary">
                    {change.reason}
                  </p>
                ) : null}
              </div>
            ))}
          </div>
        </section>
      ) : null}

      <section aria-labelledby="adapted-session-title" className="mt-8 border-t border-border/70 pt-6">
        <div className="flex items-end justify-between gap-4">
          <h3 className="font-brand text-lg font-bold leading-6 text-text-primary" id="adapted-session-title">
            Sesi&oacute;n adaptada
          </h3>
          <span className="text-[0.75rem] font-semibold text-text-muted">
            {exercises.length} {exercises.length === 1 ? "ejercicio" : "ejercicios"}
          </span>
        </div>

        {exercises.length > 0 ? (
          <ol className="mt-3 divide-y divide-border/55 border-y border-border/70">
            {exercises.map((exercise) => (
              <li className="py-5" key={`${exercise.order}-${exercise.exerciseId}`}>
                <div className="flex items-baseline gap-3">
                  <span aria-hidden="true" className="w-5 shrink-0 text-[0.6875rem] font-bold tabular-nums text-text-muted">
                    {String(exercise.order).padStart(2, "0")}
                  </span>
                  <div className="min-w-0">
                    <p className="font-brand text-[0.9375rem] font-bold leading-6 text-text-primary">
                      {exercise.name}
                    </p>
                    <p className="mt-1.5 text-[0.8125rem] font-semibold leading-5 text-text-secondary">
                      {exercise.sets} {exercise.sets === 1 ? "serie" : "series"} &middot; {formatRange(exercise.minReps, exercise.maxReps)} reps
                    </p>
                    <p className="mt-1 text-[0.8125rem] leading-5 text-text-muted">
                      RIR {formatRange(exercise.minRir, exercise.maxRir)} &middot; {formatRest(exercise.restSeconds)}
                    </p>
                  </div>
                </div>
              </li>
            ))}
          </ol>
        ) : (
          <p className="mt-3 border-y border-border/70 py-4 text-sm leading-6 text-text-secondary">
            La propuesta no incluye ejercicios finales para estas condiciones.
          </p>
        )}
      </section>

      <div className="mt-7 space-y-3">
        <AdaptiveTrainingConversationSheet
          onInvalidate={onEdit}
          onRegenerate={onRegenerate}
          proposal={proposal}
          request={request}
        />
        <Button fullWidth onClick={onEdit} variant="outline">
          Editar condiciones
        </Button>
      </div>
    </div>
  );
}

function ChangeValue({
  emphasized = false,
  label,
  value,
}: {
  emphasized?: boolean;
  label: string;
  value: string;
}) {
  return (
    <div className="min-w-0">
      <span className="block text-[0.6875rem] font-semibold uppercase tracking-[0.06em] text-text-muted">
        {label}
      </span>
      <span className={cn(
        "mt-1 block text-sm font-semibold leading-5",
        emphasized ? "text-text-primary" : "text-text-secondary",
      )}>
        {value}
      </span>
    </div>
  );
}

type StructuredChangePresentation = {
  adaptedValue: string;
  exerciseName: string | null;
  key: string;
  label: string;
  originalValue: string;
  reason: string | null;
};

function getStructuredChangePresentations(
  proposal: SessionAdaptationProposalResponse,
): StructuredChangePresentation[] {
  const originalNames = new Map(
    proposal.originalExercises.map((exercise) => [exercise.exerciseId, exercise.name]),
  );
  const adaptedNames = new Map(
    proposal.adaptedExercises.map((exercise) => [exercise.exerciseId, exercise.name]),
  );

  return proposal.changes.flatMap((change): StructuredChangePresentation[] => {
    const originalName = originalNames.get(change.originalExerciseId);
    const key = `${change.sourceRoutineExerciseId}-${change.changeType}`;

    switch (change.changeType) {
      case "ExerciseSubstituted": {
        const adaptedName = change.adaptedExerciseId
          ? adaptedNames.get(change.adaptedExerciseId)
          : undefined;

        if (!originalName || !adaptedName) return [];

        return [{
          adaptedValue: adaptedName,
          exerciseName: null,
          key,
          label: "Sustitución de ejercicio",
          originalValue: originalName,
          reason: getSubstitutionReason(change.reason),
        }];
      }

      case "ExerciseSetsReducedForTime":
      case "OptionalAccessorySetsReducedForLighter":
        if (!originalName || change.originalSets === null || change.adaptedSets === null) return [];

        return [{
          adaptedValue: formatSeries(change.adaptedSets),
          exerciseName: originalName,
          key,
          label: "Cambio de series",
          originalValue: formatSeries(change.originalSets),
          reason: change.changeType === "ExerciseSetsReducedForTime"
            ? "Para respetar el tiempo disponible."
            : "Para preparar una sesión más liviana.",
        }];

      case "RirIncreased":
        if (
          !originalName ||
          change.originalRirMin === null ||
          change.originalRirMax === null ||
          change.adaptedRirMin === null ||
          change.adaptedRirMax === null
        ) return [];

        return [{
          adaptedValue: `RIR ${formatRange(change.adaptedRirMin, change.adaptedRirMax)}`,
          exerciseName: originalName,
          key,
          label: "Cambio de RIR",
          originalValue: `RIR ${formatRange(change.originalRirMin, change.originalRirMax)}`,
          reason: "Para preparar una sesión más liviana.",
        }];

      case "OptionalAccessoryRemovedNoSubstitute":
      case "OptionalAccessoryRemovedForTime":
        if (!originalName) return [];

        return [{
          adaptedValue: "Fuera de la sesión",
          exerciseName: null,
          key,
          label: "Ejercicio eliminado",
          originalValue: originalName,
          reason: change.changeType === "OptionalAccessoryRemovedForTime"
            ? "Para respetar el tiempo disponible."
            : getRemovalReason(change.reason),
        }];

      case "RequiredExerciseUnavailable":
        if (!originalName) return [];

        return [{
          adaptedValue: "Sin reemplazo compatible",
          exerciseName: null,
          key,
          label: "Ejercicio no disponible",
          originalValue: originalName,
          reason: getRequiredUnavailableReason(change.reason),
        }];
    }
  });
}

function getSubstitutionReason(reason: string | null): string | null {
  const reasons = getKnownSubstitutionReasons(reason);

  if (reasons.has("EquipmentUnavailable") && reasons.has("UserExcludedExercise")) {
    return "El equipamiento necesario no está disponible y elegiste no realizar el ejercicio original hoy.";
  }
  if (reasons.has("EquipmentUnavailable")) {
    return "El equipamiento necesario no está disponible hoy.";
  }
  if (reasons.has("UserExcludedExercise")) {
    return "Elegiste no realizar el ejercicio original hoy.";
  }

  return null;
}

function getRemovalReason(reason: string | null): string {
  const naturalReason = getSubstitutionReason(reason);
  return naturalReason
    ? `${naturalReason} No había un reemplazo compatible.`
    : "No había un reemplazo compatible con las condiciones de hoy.";
}

function getRequiredUnavailableReason(reason: string | null): string {
  const naturalReason = getSubstitutionReason(reason);
  return naturalReason
    ? `${naturalReason} No hay un reemplazo compatible.`
    : "No hay un reemplazo compatible con las condiciones de hoy.";
}

function getKnownSubstitutionReasons(reason: string | null): Set<string> {
  return new Set(
    (reason ?? "")
      .split(",")
      .map((item) => item.trim())
      .filter((item) => item === "EquipmentUnavailable" || item === "UserExcludedExercise"),
  );
}

function formatSeries(value: number): string {
  return `${value} ${value === 1 ? "serie" : "series"}`;
}

function Metric({ label, value }: { label: string; value: string }) {
  return (
    <div className="min-w-0 border-b border-r border-border/50 px-1 py-4 even:border-r-0 [&:nth-last-child(-n+2)]:border-b-0">
      <dt className="text-[0.6875rem] font-bold uppercase leading-4 tracking-[0.06em] text-text-muted">
        {label}
      </dt>
      <dd className="mt-1.5 font-brand text-lg font-bold leading-6 tabular-nums text-text-primary">
        {value}
      </dd>
    </div>
  );
}

function ProposalError({
  onEdit,
  onRetry,
  validation,
}: {
  onEdit: () => void;
  onRetry: () => void;
  validation: boolean;
}) {
  return (
    <div className="mt-8 border-y border-border/70 py-6" role="alert">
      <h2 className="font-brand text-lg font-bold text-text-primary">
        {validation ? "Revisá las condiciones" : "No pudimos generar la adaptación"}
      </h2>
      <p className="mt-2 text-sm leading-6 text-text-secondary">
        {validation
          ? "Alguna condición no es válida para preparar la sesión. Podés volver y corregirla."
          : "Conservamos las condiciones que elegiste. Podés reintentar sin configurarlas de nuevo."}
      </p>
      {validation ? (
        <Button className="mt-5" fullWidth onClick={onEdit}>
          Editar condiciones
        </Button>
      ) : (
        <div className="mt-5 grid grid-cols-2 gap-3">
          <Button onClick={onEdit} variant="secondary">
            Editar
          </Button>
          <Button onClick={onRetry}>
            Reintentar
          </Button>
        </div>
      )}
    </div>
  );
}

function AdaptiveTrainingProposalSkeleton() {
  return (
    <div aria-label="Generando adaptaci&oacute;n" className="mt-8 animate-pulse motion-reduce:animate-none" role="status">
      <div className="border-y border-border/50 py-5">
        <div className="h-3 w-24 rounded bg-surface-elevated" />
        <div className="mt-3 h-7 w-48 rounded bg-surface-elevated" />
        <div className="mt-3 h-4 w-32 rounded bg-surface-elevated" />
      </div>
      <div className="mt-7 h-20 rounded-control-sm bg-surface-elevated" />
      <div className="mt-6 grid grid-cols-2 gap-px bg-border/50">
        {[0, 1, 2, 3].map((item) => (
          <div className="h-20 bg-background p-4" key={item}>
            <div className="h-3 w-20 rounded bg-surface-elevated" />
            <div className="mt-3 h-5 w-12 rounded bg-surface-elevated" />
          </div>
        ))}
      </div>
      <span className="sr-only">Preparando tu sesi&oacute;n adaptada.</span>
    </div>
  );
}

function getFeasibilityPresentation(feasibility: SessionAdaptationProposalResponse["feasibility"]): {
  description: string;
  label: string;
} {
  switch (feasibility) {
    case "Feasible":
      return {
        description: "La sesión puede realizarse con las condiciones que elegiste.",
        label: "Factible",
      };
    case "FeasibleWithChanges":
      return {
        description: "La sesión puede realizarse con los ajustes indicados.",
        label: "Factible con ajustes",
      };
    case "NotFeasible":
      return {
        description: "La propuesta puede revisarse, pero todavía no puede confirmarse.",
        label: "No confirmable",
      };
  }
}

function getDiagnosticMessages(diagnostics: SessionAdaptationProposalResponse["diagnostics"]): string[] {
  const messages: Array<string | null> = diagnostics.map((diagnostic) => {
    switch (diagnostic.code) {
      case "ExerciseSubstituted":
        return "Se reemplazó al menos un ejercicio para respetar las condiciones de hoy.";
      case "OptionalExerciseRemovedNoSubstitute":
        return "Se retiró un accesorio opcional que no tenía un reemplazo compatible.";
      case "RequiredExerciseHasNoSubstitute":
        return "Un ejercicio requerido no tiene un reemplazo válido con las condiciones de hoy.";
      case "OptionalAccessoryRemovedForTime":
        return "Se retiró trabajo accesorio para respetar el tiempo disponible.";
      case "ExerciseSetsReducedForTime":
        return "Se ajustaron series para respetar el tiempo disponible.";
      case "MinimumSessionExceedsAvailableTime":
        return "Incluso la sesión mínima permitida supera el tiempo disponible.";
      case "RirIncreased":
        return "Se aumentó el RIR objetivo para preparar una sesión más liviana.";
      case "OptionalAccessorySetsReducedForLighter":
        return "Se redujo trabajo accesorio para preparar una sesión más liviana.";
      case "NeutralLighterTransformation":
        return "La preferencia más liviana no requirió cambios adicionales en la prescripción.";
      default:
        return null;
    }
  });

  return [...new Set(messages.filter((message): message is string => message !== null))];
}

function formatDuration(totalSeconds: number): string {
  const safeSeconds = Math.max(0, Math.round(totalSeconds));
  const minutes = Math.floor(safeSeconds / 60);
  const seconds = safeSeconds % 60;

  if (seconds === 0) return `${minutes} min`;
  return `${minutes} min ${seconds} s`;
}

function formatRange(minimum: number, maximum: number): string {
  return minimum === maximum ? `${minimum}` : `${minimum}–${maximum}`;
}

function formatRest(totalSeconds: number): string {
  if (totalSeconds < 60) return `${totalSeconds} s`;

  const minutes = Math.floor(totalSeconds / 60);
  const seconds = totalSeconds % 60;
  return seconds === 0 ? `${minutes} min` : `${minutes} min ${seconds} s`;
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
