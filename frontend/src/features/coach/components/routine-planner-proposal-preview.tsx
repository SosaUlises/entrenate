"use client";

import { ArrowLeft, ChevronDown } from "lucide-react";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { useCallback, useEffect, useRef, useState } from "react";
import { Button } from "@/components/ui/button";
import { useTrainingProfileGate } from "@/features/training-profile/gate/training-profile-gate";
import {
  generateRoutinePlannerProposalAction,
  type GenerateRoutinePlannerProposalActionResult,
} from "../actions/generate-routine-planner-proposal.action";
import type {
  RoutinePlannerDayOrigin,
  RoutinePlannerProposalDay,
  RoutinePlannerProposalResponse,
  RoutinePlannerPrescriptionDiagnosticCode,
  RoutinePlannerScheduledDay,
  RoutinePlannerSelectionDiagnosticCode,
  RoutinePlannerSessionFocus,
  RoutinePlannerSessionVariant,
  RoutineProposalFeasibility,
} from "../types/routine-planner.types";

type ProposalState =
  | { status: "error" }
  | { status: "loading" }
  | { proposal: RoutinePlannerProposalResponse; status: "ready" };

const feasibilityLabels: Record<RoutineProposalFeasibility, string> = {
  Feasible: "Factible",
  FeasibleWithWarnings: "Factible con observaciones",
  NotFeasible: "No confirmable",
};

const focusLabels: Record<RoutinePlannerSessionFocus, string> = {
  CoreCalves: "Core y gemelos",
  FullBody: "Full body",
  Legs: "Piernas",
  Lower: "Tren inferior",
  Pull: "Tracción",
  Push: "Empuje",
  Upper: "Tren superior",
};

const variantLabels: Record<RoutinePlannerSessionVariant, string> = {
  A: "Variante A",
  B: "Variante B",
  C: "Variante C",
  None: "",
};

const originLabels: Record<RoutinePlannerDayOrigin, string> = {
  Derived: "Sugerido",
  Preferred: "Preferido",
};

const weekDays: ReadonlyArray<{
  label: string;
  shortLabel: string;
  value: RoutinePlannerScheduledDay;
}> = [
  { label: "Lunes", shortLabel: "Lun", value: "Lunes" },
  { label: "Martes", shortLabel: "Mar", value: "Martes" },
  { label: "Miércoles", shortLabel: "Mié", value: "Miercoles" },
  { label: "Jueves", shortLabel: "Jue", value: "Jueves" },
  { label: "Viernes", shortLabel: "Vie", value: "Viernes" },
  { label: "Sábado", shortLabel: "Sáb", value: "Sabado" },
  { label: "Domingo", shortLabel: "Dom", value: "Domingo" },
];

export function RoutinePlannerProposalPreview() {
  const router = useRouter();
  const { invalidateSession } = useTrainingProfileGate();
  const [state, setState] = useState<ProposalState>({ status: "loading" });
  const mountedRef = useRef(false);
  const requestInFlightRef = useRef(false);

  const handleResult = useCallback(
    (result: GenerateRoutinePlannerProposalActionResult) => {
      if (!mountedRef.current) return;

      if (result.status === "unauthenticated") {
        invalidateSession();
        router.replace("/login");
        return;
      }

      setState(result.status === "ready" ? result : { status: "error" });
    },
    [invalidateSession, router],
  );

  const generateProposal = useCallback(async () => {
    if (requestInFlightRef.current) return;

    requestInFlightRef.current = true;
    setState({ status: "loading" });

    try {
      handleResult(await generateRoutinePlannerProposalAction());
    } catch {
      if (mountedRef.current) setState({ status: "error" });
    } finally {
      requestInFlightRef.current = false;
    }
  }, [handleResult]);

  useEffect(() => {
    mountedRef.current = true;

    if (!requestInFlightRef.current) {
      requestInFlightRef.current = true;
      void generateRoutinePlannerProposalAction()
        .then(handleResult, () => {
          if (mountedRef.current) setState({ status: "error" });
        })
        .finally(() => {
          requestInFlightRef.current = false;
        });
    }

    return () => {
      mountedRef.current = false;
    };
  }, [handleResult]);

  return (
    <section
      aria-busy={state.status === "loading"}
      aria-labelledby="routine-proposal-title"
      className="mx-auto w-full min-w-0 max-w-xl"
    >
      <header className="max-w-lg">
        <div className="flex min-w-0 items-center gap-1">
          <Link
            aria-label="Volver al planificador de rutinas"
            className="flex size-11 shrink-0 items-center justify-center rounded-control-sm text-text-secondary transition-colors duration-150 hover:bg-surface-interactive hover:text-text-primary focus-visible:outline-2 focus-visible:outline-focus motion-reduce:transition-none"
            href="/coach/routines"
          >
            <ArrowLeft aria-hidden="true" size={19} />
          </Link>
          <h1
            className="min-w-0 font-brand text-2xl font-bold leading-tight text-text-primary"
            id="routine-proposal-title"
          >
            Propuesta de rutina
          </h1>
        </div>
        <p className="mt-1.5 pl-12 text-sm leading-6 text-text-secondary">
          Estructura generada a partir de tu perfil actual.
        </p>
      </header>

      {state.status === "loading" ? (
        <ProposalPreviewSkeleton />
      ) : state.status === "error" ? (
        <div className="mt-8 border-t border-border/70 pt-6" role="alert">
          <p className="text-sm leading-6 text-text-primary">
            No pudimos generar la propuesta.
          </p>
          <Button
            className="mt-4"
            onClick={() => void generateProposal()}
            variant="secondary"
          >
            Reintentar
          </Button>
        </div>
      ) : (
        <ProposalContent proposal={state.proposal} />
      )}
    </section>
  );
}

function ProposalContent({
  proposal,
}: {
  proposal: RoutinePlannerProposalResponse;
}) {
  const estimatedDuration = formatDurationRange(
    proposal.days.map((day) => day.estimatedDurationSeconds),
  );
  const maximumAvailability = proposal.days.length
    ? `${Math.max(...proposal.days.map((day) => day.targetDurationMinutes))} min`
    : "—";
  const structure = proposal.days.length
    ? proposal.days.map(formatSessionIdentity).join(" · ")
    : "Sin estructura disponible";

  return (
    <div className="mt-8">
      <section
        aria-labelledby="proposal-feasibility-title"
        className="border-y border-border/70 py-6"
      >
        <div className="flex items-center gap-3">
          <span
            aria-hidden="true"
            className="h-0.5 w-7 shrink-0 rounded-full bg-thermal-accent"
          />
          <h2
            className="font-brand text-2xl font-bold leading-tight text-text-primary"
            id="proposal-feasibility-title"
          >
            {feasibilityLabels[proposal.feasibility]}
          </h2>
        </div>

        <div className="mt-6 border-t border-border/60 pt-5">
          <p className="text-[0.8125rem] font-semibold leading-5 text-text-secondary">
            Estructura
          </p>
          <p className="mt-1.5 break-words font-brand text-lg font-bold leading-7 text-text-primary">
            {structure}
          </p>
        </div>

        <dl className="mt-6 grid grid-cols-[0.7fr_1.3fr] border-t border-border/60 pt-5 sm:grid-cols-[0.65fr_1.25fr_1.1fr]">
          <ProposalMetric
            className="pr-4"
            label="Días"
            value={String(proposal.requestedDaysPerWeek)}
          />
          <ProposalMetric
            className="border-l border-border/60 px-4"
            label="Duración estimada"
            value={estimatedDuration}
            valueClassName="whitespace-nowrap text-[0.9375rem] sm:text-[1.0625rem]"
          />
          <ProposalMetric
            className="col-span-2 mt-5 border-t border-border/60 pt-5 sm:col-span-1 sm:mt-0 sm:border-t-0 sm:border-l sm:px-4 sm:pt-0"
            label="Disponibilidad máxima"
            value={maximumAvailability}
          />
        </dl>
        <p className="mt-5 text-[0.8125rem] leading-6 text-text-muted">
          La disponibilidad es un límite por sesión, no una duración que
          debas completar.
        </p>
      </section>

      <ProposalObservations proposal={proposal} />
      <WeeklyCalendar days={proposal.days} />
      <SessionList days={proposal.days} />
    </div>
  );
}

function ProposalObservations({
  proposal,
}: {
  proposal: RoutinePlannerProposalResponse;
}) {
  const observations = buildProposalObservations(proposal);

  if (observations.length === 0) return null;

  const isNotFeasible = proposal.feasibility === "NotFeasible";

  return (
    <section
      aria-labelledby="proposal-observations-title"
      className="mt-10 border-y border-border/70 py-5"
    >
      <div className="flex items-center gap-3">
        <span
          aria-hidden="true"
          className="size-1.5 shrink-0 rounded-full bg-thermal-accent"
        />
        <h2
          className="font-brand text-lg font-bold leading-6 text-text-primary"
          id="proposal-observations-title"
        >
          {isNotFeasible ? "Qué limita la propuesta" : "Observaciones"}
        </h2>
      </div>

      <ul className="mt-3 divide-y divide-border/50">
        {observations.map((observation, index) => (
          <li
            className="py-3 text-sm leading-6 text-text-secondary first:pt-1 last:pb-0"
            key={`${observation}-${index}`}
          >
            {observation}
          </li>
        ))}
      </ul>
    </section>
  );
}

function ProposalMetric({
  className,
  label,
  value,
  valueClassName,
}: {
  className?: string;
  label: string;
  value: string;
  valueClassName?: string;
}) {
  return (
    <div className={`min-w-0 ${className ?? ""}`}>
      <dt className="text-[0.8125rem] font-medium leading-5 text-text-secondary">
        {label}
      </dt>
      <dd
        className={`mt-2 break-words font-brand text-[1.0625rem] font-bold leading-6 text-text-primary tabular-nums ${valueClassName ?? ""}`}
      >
        {value}
      </dd>
    </div>
  );
}

function WeeklyCalendar({ days }: { days: RoutinePlannerProposalDay[] }) {
  return (
    <section aria-labelledby="proposal-calendar-title" className="mt-11">
      <h2
        className="font-brand text-xl font-bold leading-7 text-text-primary"
        id="proposal-calendar-title"
      >
        Semana propuesta
      </h2>

      <ol className="mt-4 grid grid-cols-7 gap-1.5">
        {weekDays.map((weekDay) => {
          const scheduledDay = days.find(
            (day) => day.scheduledDay === weekDay.value,
          );

          return (
            <li
              aria-label={
                scheduledDay
                  ? `${weekDay.label}, ${originLabels[scheduledDay.dayOrigin]}`
                  : `${weekDay.label}, sin sesión propuesta`
              }
              className={`flex min-h-[4.25rem] min-w-0 flex-col items-center justify-center rounded-control-sm border px-1 py-2.5 ${scheduledDay ? "border-border-strong bg-surface-interactive text-text-primary" : "border-border/60 text-text-muted"}`}
              key={weekDay.value}
            >
              <span className="text-[0.8125rem] font-semibold leading-4">
                {weekDay.shortLabel}
              </span>
              {scheduledDay ? (
                <span
                  aria-hidden="true"
                  className={`mt-2 bg-thermal-accent ${scheduledDay.dayOrigin === "Preferred" ? "size-1.5 rounded-full" : "h-0.5 w-3 rounded-full"}`}
                />
              ) : (
                <span aria-hidden="true" className="mt-2 h-1.5" />
              )}
            </li>
          );
        })}
      </ol>

      <div className="mt-3 flex flex-wrap gap-x-5 gap-y-2 text-[0.8125rem] leading-5 text-text-secondary">
        <span className="inline-flex items-center gap-2">
          <span
            aria-hidden="true"
            className="size-1.5 rounded-full bg-thermal-accent"
          />
          Preferido
        </span>
        <span className="inline-flex items-center gap-2">
          <span
            aria-hidden="true"
            className="h-0.5 w-3 rounded-full bg-thermal-accent"
          />
          Sugerido
        </span>
      </div>
    </section>
  );
}

function SessionList({ days }: { days: RoutinePlannerProposalDay[] }) {
  return (
    <section aria-labelledby="proposal-sessions-title" className="mt-12">
      <h2
        className="font-brand text-xl font-bold leading-7 text-text-primary"
        id="proposal-sessions-title"
      >
        Sesiones
      </h2>

      {days.length === 0 ? (
        <p className="mt-4 border-y border-border/70 py-5 text-sm leading-6 text-text-secondary">
          La propuesta no incluye sesiones.
        </p>
      ) : (
        <div className="mt-4 border-y border-border/70">
          {days.map((day, index) => (
            <SessionDisclosure day={day} defaultOpen={index === 0} key={day.order} />
          ))}
        </div>
      )}
    </section>
  );
}

function SessionDisclosure({
  day,
  defaultOpen,
}: {
  day: RoutinePlannerProposalDay;
  defaultOpen: boolean;
}) {
  return (
    <details
      className="group border-b border-border/60 last:border-b-0"
      open={defaultOpen || undefined}
    >
      <summary className="flex min-h-24 cursor-pointer list-none items-center gap-3 rounded-control-sm px-1 py-5 transition-colors hover:bg-surface-interactive/45 focus-visible:outline-2 focus-visible:outline-focus motion-reduce:transition-none [&::-webkit-details-marker]:hidden">
        <span className="min-w-0 flex-1">
          <span className="block font-brand text-lg font-bold leading-6 text-text-primary">
            {formatDayName(day.scheduledDay)}
          </span>
          <span className="mt-1 block text-[0.8125rem] font-semibold leading-5 text-info">
            {formatSessionIdentity(day)}
          </span>
        </span>
        <span className="shrink-0 text-right">
          <span className="block text-[0.8125rem] font-semibold leading-5 text-text-primary tabular-nums">
            {formatDuration(day.estimatedDurationSeconds)}
          </span>
          <span className="mt-0.5 block text-[0.8125rem] leading-5 text-text-secondary">
            {day.exercises.length}{" "}
            {day.exercises.length === 1 ? "ejercicio" : "ejercicios"}
          </span>
        </span>
        <ChevronDown
          aria-hidden="true"
          className="shrink-0 text-text-muted transition-transform duration-150 group-open:rotate-180 motion-reduce:transition-none"
          size={18}
        />
      </summary>

      <div className="border-t border-border/50 px-1 pb-6 pt-4">
        {day.exercises.length === 0 ? (
          <p className="text-sm leading-6 text-text-secondary">
            No hay ejercicios asignados a esta sesión.
          </p>
        ) : (
          <ol className="divide-y divide-border/50">
            {day.exercises.map((exercise) => (
              <li
                className="flex min-h-20 items-start gap-3 py-4"
                key={exercise.exerciseId}
              >
                <span className="mt-0.5 w-6 shrink-0 text-[0.8125rem] font-semibold leading-5 text-text-muted tabular-nums">
                  {String(exercise.order).padStart(2, "0")}
                </span>
                <span className="min-w-0 flex-1">
                  <span className="block break-words text-sm font-semibold leading-5 text-text-primary">
                    {exercise.name}
                  </span>
                  <span className="mt-1.5 block text-[0.8125rem] leading-5 text-text-secondary tabular-nums">
                    {formatSets(exercise.sets)} · {formatRange(exercise.minReps, exercise.maxReps)} reps
                  </span>
                  <span className="mt-0.5 block text-[0.8125rem] leading-5 text-text-muted tabular-nums">
                    RIR {formatRange(exercise.minRir, exercise.maxRir)} · {exercise.restSeconds} s
                  </span>
                </span>
              </li>
            ))}
          </ol>
        )}
      </div>
    </details>
  );
}

function ProposalPreviewSkeleton() {
  return (
    <div
      aria-label="Generando propuesta de rutina"
      className="mt-8 animate-pulse motion-reduce:animate-none"
      role="status"
    >
      <div className="border-y border-border/50 py-6">
        <div className="h-7 w-2/3 rounded bg-surface-elevated" />
        <div className="mt-6 h-5 w-full rounded bg-surface-elevated" />
        <div className="mt-6 grid grid-cols-2 gap-4 border-t border-border/50 pt-5 sm:grid-cols-3">
          {[0, 1, 2].map((item) => (
            <div key={item}>
              <div className="h-3 w-16 rounded bg-surface-elevated" />
              <div className="mt-2 h-5 w-20 rounded bg-surface-elevated" />
            </div>
          ))}
        </div>
      </div>
      <div className="mt-11 h-6 w-40 rounded bg-surface-elevated" />
      <div className="mt-4 grid grid-cols-7 gap-1.5">
        {weekDays.map((day) => (
          <div
            className="h-[4.25rem] rounded-control-sm bg-surface-elevated"
            key={day.value}
          />
        ))}
      </div>
      <div className="mt-12 h-6 w-24 rounded bg-surface-elevated" />
      <div className="mt-4 border-y border-border/50">
        {[0, 1, 2].map((item) => (
          <div
            className="h-24 border-b border-border/50 py-5 last:border-b-0"
            key={item}
          >
            <div className="h-5 w-32 rounded bg-surface-elevated" />
            <div className="mt-2 h-3 w-24 rounded bg-surface-elevated" />
          </div>
        ))}
      </div>
    </div>
  );
}

function formatSessionIdentity(day: RoutinePlannerProposalDay): string {
  const variant = variantLabels[day.variant];
  return variant ? `${focusLabels[day.focus]} · ${variant}` : focusLabels[day.focus];
}

function formatDayName(day: RoutinePlannerScheduledDay): string {
  return weekDays.find((item) => item.value === day)?.label ?? day;
}

function formatDurationRange(durations: number[]): string {
  if (durations.length === 0) return "—";

  const minimum = Math.min(...durations);
  const maximum = Math.max(...durations);

  return minimum === maximum
    ? formatDuration(minimum)
    : `${formatDuration(minimum)}–${formatDuration(maximum)}`;
}

function formatDuration(totalSeconds: number): string {
  const minutes = Math.floor(totalSeconds / 60);
  const seconds = totalSeconds % 60;

  if (minutes === 0) return `${seconds} s`;
  if (seconds === 0) return `${minutes} min`;
  return `${minutes} min ${seconds} s`;
}

function formatSets(sets: number): string {
  return `${sets} ${sets === 1 ? "serie" : "series"}`;
}

function formatRange(minimum: number, maximum: number): string {
  return minimum === maximum ? String(minimum) : `${minimum}–${maximum}`;
}

function buildProposalObservations(
  proposal: RoutinePlannerProposalResponse,
): string[] {
  const observations = [
    ...proposal.selectionDiagnostics.map((diagnostic) =>
      describeSelectionDiagnostic(diagnostic.code),
    ),
    ...proposal.prescriptionDiagnostics.map((diagnostic) =>
      describePrescriptionDiagnostic(diagnostic.code),
    ),
  ];
  const uniqueObservations = [...new Set(observations)];

  if (uniqueObservations.length > 0) return uniqueObservations;

  if (proposal.feasibility === "FeasibleWithWarnings") {
    return [
      "La propuesta es factible, pero incluye observaciones para revisar antes de confirmarla.",
    ];
  }

  if (proposal.feasibility === "NotFeasible") {
    return [
      "No se pudo construir una rutina utilizable con las restricciones actuales.",
    ];
  }

  return [];
}

function describeSelectionDiagnostic(
  code: RoutinePlannerSelectionDiagnosticCode,
): string {
  switch (code) {
    case "MissingRequiredExercise":
      return "No se encontró una opción compatible para cubrir todos los movimientos necesarios.";
    case "MissingOptionalExercise":
      return "No se encontró una opción compatible para uno de los complementos opcionales.";
    case "ExerciseReusedAcrossVariants":
      return "Se reutilizó un ejercicio entre sesiones porque no había una alternativa compatible equivalente.";
    default:
      return "La propuesta incluye una observación adicional del planificador.";
  }
}

function describePrescriptionDiagnostic(
  code: RoutinePlannerPrescriptionDiagnosticCode,
): string {
  switch (code) {
    case "SelectionNotFeasible":
      return "La selección no cubre todos los ejercicios necesarios para completar la rutina.";
    case "WeeklyVolumeTargetUnreachable":
      return "No se pudo alcanzar todo el volumen semanal previsto con las restricciones actuales.";
    case "OptionalExerciseRemovedForDuration":
      return "Se retiraron complementos opcionales para respetar el tiempo disponible.";
    case "ExerciseSetsReducedForDuration":
      return "Se redujeron algunas series para ajustar las sesiones al tiempo disponible.";
    case "SessionDurationExceeded":
      return "Al menos una sesión supera la duración objetivo incluso después de los ajustes.";
    default:
      return "La propuesta incluye una observación adicional del planificador.";
  }
}
