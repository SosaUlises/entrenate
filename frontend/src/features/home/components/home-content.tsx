"use client";

import { ArrowRight, ChevronRight, Search, X } from "lucide-react";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { useEffect, useRef, useState } from "react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Spinner } from "@/components/ui/spinner";
import { getRoutineDetailAction } from "@/features/routines/actions/get-routine-detail.action";
import type { RoutineDetail, RoutineSummary } from "@/features/routines/types/routine.types";
import { useTrainingProfileGate } from "@/features/training-profile/gate/training-profile-gate";
import { formatActiveSessionStart, isStaleActiveSession } from "@/features/training/active-session-time";
import { getActiveTrainingSessionAction, startTrainingSessionAction } from "@/features/training/actions/training.actions";
import { ActiveSessionRecoveryDialog } from "@/features/training/components/active-session-recovery-dialog";
import type { TrainingSession } from "@/features/training/types/training.types";
import { getHomeStateAction, type HomeState } from "../actions/get-home-state.action";

type HomeViewState = { status: "loading" } | HomeState;

const primaryLinkClass = "flex min-h-[3.25rem] min-w-0 w-full max-w-sm items-center justify-between overflow-hidden rounded-control-sm bg-primary px-5 text-[0.9375rem] font-bold text-primary-foreground transition-[background-color,transform] duration-200 hover:bg-[color-mix(in_srgb,var(--primary)_88%,var(--primary-pressed))] active:scale-[0.99] active:bg-primary-pressed focus-visible:outline-focus";
const sectionLabelClass = "text-[0.8125rem] font-semibold uppercase leading-5 tracking-[0.12em] text-text-secondary";

export function HomeContent() {
  const router = useRouter();
  const { invalidateSession } = useTrainingProfileGate();
  const [state, setState] = useState<HomeViewState>({ status: "loading" });
  const [requestKey, setRequestKey] = useState(0);

  useEffect(() => {
    let active = true;

    void getHomeStateAction().then(
      (result) => {
        if (!active) return;

        if (result.status === "unauthenticated") {
          invalidateSession();
          router.replace("/login");
          return;
        }

        setState(result);
      },
      () => {
        if (active) setState({ status: "error" });
      },
    );

    return () => {
      active = false;
    };
  }, [invalidateSession, requestKey, router]);

  const handleUnauthenticated = () => {
    invalidateSession();
    router.replace("/login");
  };

  if (state.status === "loading" || state.status === "unauthenticated") {
    return (
      <div className="grid min-h-64 max-w-lg place-items-center" role="status">
        <Spinner className="size-6 text-primary" label="Cargando tu entrenamiento" />
      </div>
    );
  }

  if (state.status === "error") {
    return (
      <section aria-labelledby="home-error-title" className="max-w-lg py-2">
        <p className={sectionLabelClass}>TU ENTRENAMIENTO</p>
        <h2 className="mt-5 max-w-md font-brand text-3xl font-bold leading-tight text-text-primary" id="home-error-title">
          No pudimos actualizar tu estado
        </h2>
        <p className="mt-3 max-w-sm text-[0.9375rem] leading-6 text-text-secondary">
          No podemos confirmar si tenés un entrenamiento en curso. Intentá nuevamente.
        </p>
        <Button
          className="mt-7 min-h-[3.25rem] w-full max-w-sm rounded-control-sm text-[0.9375rem] font-bold"
          onClick={() => {
            setState({ status: "loading" });
            setRequestKey((key) => key + 1);
          }}
        >
          Reintentar
        </Button>
      </section>
    );
  }

  if (state.status === "active-session") {
    return (
      <ActiveSessionState
        onResolved={() => {
          setState({ status: "loading" });
          setRequestKey((key) => key + 1);
        }}
        onUnauthenticated={handleUnauthenticated}
        state={state}
      />
    );
  }

  if (state.status === "no-routines") return <NoRoutinesState />;

  return <RoutinesAvailableState onUnauthenticated={handleUnauthenticated} routines={state.routines} />;
}

function ActiveSessionState({
  onResolved,
  onUnauthenticated,
  state,
}: {
  onResolved: () => void;
  onUnauthenticated: () => void;
  state: Extract<HomeState, { status: "active-session" }>;
}) {
  const [confirmingCancellation, setConfirmingCancellation] = useState(false);

  if (isStaleActiveSession(state.horaInicio)) {
    return (
      <section aria-labelledby="active-training-title" className="max-w-lg py-2">
        <p className="text-[0.8125rem] font-semibold uppercase leading-5 tracking-[0.12em] text-info">
          ENTRENAMIENTO PENDIENTE
        </p>
        <h2 className="mt-5 break-words font-brand text-3xl font-bold leading-[1.08] text-text-primary sm:text-4xl" id="active-training-title">
          {state.exerciseName ?? "Tu entrenamiento"}
        </h2>
        <p className="mt-3 text-[0.9375rem] font-medium leading-6 text-text-primary">
          {formatActiveSessionStart(state.horaInicio)}
        </p>
        <p className="mt-1 text-sm leading-6 text-text-secondary">
          {state.registeredSetCount} {state.registeredSetCount === 1 ? "serie registrada" : "series registradas"}
        </p>
        <PrimaryLink className="mt-7" href="/training">
          Continuar entrenamiento
        </PrimaryLink>
        <button
          className="mt-2 inline-flex min-h-11 items-center text-sm font-semibold text-text-secondary transition-colors hover:text-error focus-visible:outline-primary"
          onClick={() => setConfirmingCancellation(true)}
          type="button"
        >
          Cancelar sesión
        </button>
        {confirmingCancellation ? (
          <ActiveSessionRecoveryDialog
            initialStep="cancel"
            onClose={() => setConfirmingCancellation(false)}
            onResolved={onResolved}
            onUnauthenticated={onUnauthenticated}
            session={{ id: state.sessionId, horaInicio: state.horaInicio }}
          />
        ) : null}
      </section>
    );
  }

  if (state.readyToFinish) {
    return (
      <section aria-labelledby="active-training-title" className="max-w-lg py-2">
        <p className="text-[0.8125rem] font-semibold uppercase leading-5 tracking-[0.12em] text-success">
          ENTRENAMIENTO LISTO
        </p>
        <h2 className="mt-5 font-brand text-4xl font-bold leading-[1.05] text-text-primary" id="active-training-title">
          Objetivos completados.
        </h2>
        <p className="mt-4 max-w-sm text-[0.9375rem] leading-6 text-text-secondary">
          Tu sesión está lista para finalizar.
        </p>
        <PrimaryLink className="mt-8" href="/training">
          Continuar entrenamiento
        </PrimaryLink>
        <HistoryLink />
      </section>
    );
  }

  return (
    <section aria-labelledby="active-training-title" className="max-w-lg py-2">
      <p className="text-[0.8125rem] font-semibold uppercase leading-5 tracking-[0.12em] text-info">
        ENTRENAMIENTO EN CURSO
      </p>
      <h2 className="mt-5 break-words font-brand text-3xl font-bold leading-[1.08] text-text-primary sm:text-4xl" id="active-training-title">
        {state.exerciseName}
      </h2>

      <dl className="mt-7 grid max-w-sm grid-cols-2 border-y border-border/60 py-4">
        <div className="min-w-0 border-r border-border/60 pr-4">
          <dt className="text-[0.8125rem] font-semibold uppercase leading-5 tracking-[0.1em] text-text-secondary">EJERCICIO</dt>
          <dd className="mt-2 font-brand text-3xl font-bold leading-none tabular-nums text-info">
            {formatMetric(state.exercisePosition)} <span className="text-xl text-text-secondary">/ {formatMetric(state.exerciseCount)}</span>
          </dd>
        </div>
        <div className="min-w-0 pl-5">
          <dt className="text-[0.8125rem] font-semibold uppercase leading-5 tracking-[0.1em] text-text-secondary">SERIE</dt>
          <dd className="mt-2 font-brand text-3xl font-bold leading-none tabular-nums text-info">
            {formatMetric(state.setNumber)} <span className="text-xl text-text-secondary">/ {formatMetric(state.targetSetCount)}</span>
          </dd>
        </div>
      </dl>

      <PrimaryLink className="mt-8" href="/training">
        Continuar entrenamiento
      </PrimaryLink>
      <HistoryLink />
    </section>
  );
}

function NoRoutinesState() {
  return (
    <section aria-labelledby="first-routine-title" className="w-full max-w-lg py-2">
      <h2 className="max-w-md font-brand text-4xl font-bold leading-[1.05] text-text-primary" id="first-routine-title">
        Creá tu primera rutina.
      </h2>
      <p className="mt-4 max-w-sm text-[0.9375rem] leading-6 text-text-secondary">
        Organizá tus ejercicios y empezá a registrar tus entrenamientos.
      </p>
      <PrimaryLink className="mt-7" href="/routines/new">
        Crear rutina
      </PrimaryLink>
      <HistoryLink />
    </section>
  );
}

type RoutineStartError = {
  routineId: string;
  kind: "detail" | "empty" | "verification" | "start";
};

function RoutinesAvailableState({
  onUnauthenticated,
  routines,
}: {
  onUnauthenticated: () => void;
  routines: RoutineSummary[];
}) {
  const router = useRouter();
  const [loadingRoutineId, setLoadingRoutineId] = useState<string | null>(null);
  const [startingDayId, setStartingDayId] = useState<string | null>(null);
  const [selectedRoutine, setSelectedRoutine] = useState<RoutineDetail | null>(null);
  const [startError, setStartError] = useState<RoutineStartError | null>(null);
  const [recoverySession, setRecoverySession] = useState<TrainingSession | null>(null);
  const [searchQuery, setSearchQuery] = useState("");

  const showSearch = routines.length >= 7;
  const normalizedQuery = searchQuery.trim().toLocaleLowerCase("es-AR");
  const visibleRoutines = normalizedQuery
    ? routines.filter((routine) => routine.nombre.toLocaleLowerCase("es-AR").includes(normalizedQuery))
    : routines;

  async function recoverActiveSession(routineId: string): Promise<"none" | "blocked" | "unauthenticated" | "error"> {
    const result = await getActiveTrainingSessionAction();

    if (result.status === "success") {
      setSelectedRoutine(null);
      setRecoverySession(result.data);
      return "blocked";
    }
    if (result.status === "unauthenticated") {
      onUnauthenticated();
      return "unauthenticated";
    }
    if (result.status === "not-found") return "none";

    setStartError({ routineId, kind: "verification" });
    return "error";
  }

  async function startDay(routineId: string, dayId: string) {
    if (startingDayId) return;

    setStartingDayId(dayId);
    setStartError(null);

    try {
      const activeState = await recoverActiveSession(routineId);
      if (activeState !== "none") return;

      const result = await startTrainingSessionAction(dayId);
      if (result.status === "success") {
        router.push("/training");
        return;
      }
      if (result.status === "unauthenticated") {
        onUnauthenticated();
        return;
      }
      if (result.status === "conflict") {
        const recoveryState = await recoverActiveSession(routineId);
        if (recoveryState === "none") setStartError({ routineId, kind: "start" });
        return;
      }

      setStartError({ routineId, kind: "start" });
    } catch {
      setStartError({ routineId, kind: "verification" });
    } finally {
      setStartingDayId(null);
    }
  }

  async function chooseRoutine(routineId: string) {
    if (loadingRoutineId || startingDayId) return;

    setLoadingRoutineId(routineId);
    setStartError(null);

    try {
      const result = await getRoutineDetailAction(routineId);
      if (result.status === "unauthenticated") {
        onUnauthenticated();
        return;
      }
      if (result.status !== "ready") {
        setStartError({ routineId, kind: "detail" });
        return;
      }

      const days = [...result.routine.dias].sort((first, second) => first.orden - second.orden);
      if (days.length === 0) {
        setStartError({ routineId, kind: "empty" });
        return;
      }
      if (days.length === 1) {
        await startDay(routineId, days[0].id);
        return;
      }

      setSelectedRoutine({ ...result.routine, dias: days });
    } catch {
      setStartError({ routineId, kind: "detail" });
    } finally {
      setLoadingRoutineId(null);
    }
  }

  return (
    <section aria-labelledby="available-routines-title" className="w-full max-w-lg py-2">
      <h2 className="font-brand text-3xl font-bold leading-[1.08] text-text-primary" id="available-routines-title">
        Elegí una rutina.
      </h2>

      {showSearch ? (
        <div className="relative mt-6">
          <label className="sr-only" htmlFor="home-routine-search">Buscar rutina por nombre</label>
          <Search aria-hidden="true" className="pointer-events-none absolute left-4 top-1/2 -translate-y-1/2 text-text-secondary" size={18} strokeWidth={1.75} />
          <Input
            autoComplete="off"
            className="min-h-12! rounded-control-sm bg-surface/70 pl-11 pr-4 text-[0.9375rem]"
            id="home-routine-search"
            onChange={(event) => setSearchQuery(event.target.value)}
            placeholder="Buscar rutina"
            type="search"
            value={searchQuery}
          />
        </div>
      ) : null}

      {visibleRoutines.length > 0 ? (
        <ul className={`${showSearch ? "mt-4" : "mt-6"} border-t border-border/60`}>
          {visibleRoutines.map((routine) => {
          const isLoading = loadingRoutineId === routine.id;
          const error = startError?.routineId === routine.id ? getRoutineErrorMessage(startError.kind) : null;

          return (
            <li className="border-b border-border/60" key={routine.id}>
              <button
                aria-busy={isLoading}
                className="group flex min-h-[4.5rem] w-full min-w-0 items-center gap-4 px-2 py-2 text-left transition-[background-color,transform] duration-200 hover:bg-surface/35 active:scale-[0.99] active:bg-primary/5 focus-visible:bg-primary/5 focus-visible:outline-primary disabled:cursor-wait motion-reduce:transition-none motion-reduce:active:scale-100"
                disabled={Boolean(loadingRoutineId || startingDayId)}
                onClick={() => void chooseRoutine(routine.id)}
                type="button"
              >
                <span className="min-w-0 flex-1">
                  <span className="line-clamp-2 break-words font-brand text-lg font-bold leading-6 text-text-primary transition-colors group-hover:text-primary">
                    {routine.nombre}
                  </span>
                  <span className="mt-1 block text-[0.8125rem] font-semibold leading-5 text-text-secondary">
                    {routine.cantidadDias} {routine.cantidadDias === 1 ? "día" : "días"}
                  </span>
                </span>
                {isLoading ? (
                  <Spinner className="size-5 shrink-0 text-primary" label={`Cargando ${routine.nombre}`} />
                ) : (
                  <ChevronRight aria-hidden="true" className="shrink-0 text-text-secondary transition-[color,transform] duration-200 group-hover:translate-x-0.5 group-hover:text-primary motion-reduce:transition-none" size={20} />
                )}
              </button>
              {error ? <p className="pb-3 text-[0.8125rem] leading-5 text-error" role="alert">{error}</p> : null}
            </li>
          );
          })}
        </ul>
      ) : (
        <p className="mt-4 border-y border-border/60 py-6 text-[0.9375rem] leading-6 text-text-secondary" role="status">
          No encontramos rutinas con ese nombre.
        </p>
      )}

      <HistoryLink />

      {selectedRoutine ? (
        <DayPickerDialog
          onClose={() => {
            if (!startingDayId) setSelectedRoutine(null);
          }}
          onSelect={(dayId) => void startDay(selectedRoutine.id, dayId)}
          routine={selectedRoutine}
          startError={startError?.routineId === selectedRoutine.id ? getRoutineErrorMessage(startError.kind) : null}
          startingDayId={startingDayId}
        />
      ) : null}

      {recoverySession ? (
        <ActiveSessionRecoveryDialog
          onClose={() => setRecoverySession(null)}
          onResolved={() => setRecoverySession(null)}
          onUnauthenticated={onUnauthenticated}
          session={recoverySession}
        />
      ) : null}
    </section>
  );
}

function DayPickerDialog({
  onClose,
  onSelect,
  routine,
  startError,
  startingDayId,
}: {
  onClose: () => void;
  onSelect: (dayId: string) => void;
  routine: RoutineDetail;
  startError: string | null;
  startingDayId: string | null;
}) {
  const dialogRef = useRef<HTMLDialogElement>(null);

  useEffect(() => {
    const dialog = dialogRef.current;
    if (dialog && !dialog.open) dialog.showModal();
  }, []);

  return (
    <dialog
      aria-labelledby="home-day-picker-title"
      aria-modal="true"
      className="mt-auto max-h-[calc(100dvh-1rem)] w-full max-w-full overflow-y-auto rounded-t-container border border-border bg-surface-elevated p-0 text-text-primary shadow-elevated backdrop:bg-black/75 sm:m-auto sm:w-[calc(100%-2rem)] sm:max-w-md sm:rounded-container"
      onCancel={(event) => {
        if (startingDayId) event.preventDefault();
      }}
      onClick={(event) => {
        if (event.target === event.currentTarget && !startingDayId) event.currentTarget.close();
      }}
      onClose={onClose}
      ref={dialogRef}
    >
      <div className="pb-[max(1.25rem,env(safe-area-inset-bottom))]">
        <div className="flex min-w-0 items-start gap-4 px-5 pb-4 pt-5 sm:px-6 sm:pt-6">
          <div className="min-w-0 flex-1">
            <h2 className="break-words font-brand text-2xl font-bold leading-tight" id="home-day-picker-title">
              {routine.nombre}
            </h2>
            <p className="mt-2 text-[0.9375rem] leading-6 text-text-secondary">Elegí un día para empezar.</p>
          </div>
          <button
            aria-label="Cerrar selector de días"
            className="flex size-11 shrink-0 items-center justify-center rounded-full text-text-secondary transition-colors hover:bg-surface hover:text-text-primary focus-visible:outline-primary"
            disabled={Boolean(startingDayId)}
            onClick={() => dialogRef.current?.close()}
            type="button"
          >
            <X aria-hidden="true" size={20} />
          </button>
        </div>

        <ul className="border-y border-border/60">
          {routine.dias.map((day) => (
            <li className="border-b border-border/60 last:border-b-0" key={day.id}>
              <button
                className="group flex min-h-[4.5rem] w-full min-w-0 items-center gap-4 px-5 py-3 text-left focus-visible:outline-primary disabled:cursor-wait sm:px-6"
                disabled={Boolean(startingDayId)}
                onClick={() => onSelect(day.id)}
                type="button"
              >
                <span className="min-w-0 flex-1">
                  <span className="block break-words font-brand text-lg font-bold leading-6 text-text-primary group-hover:text-primary">
                    {day.nombre}
                  </span>
                  <span className="mt-1 block text-[0.8125rem] font-semibold leading-5 text-text-secondary">
                    {day.ejercicios.length} {day.ejercicios.length === 1 ? "ejercicio" : "ejercicios"}
                  </span>
                </span>
                {startingDayId === day.id ? (
                  <Spinner className="size-5 shrink-0 text-primary" label={`Iniciando ${day.nombre}`} />
                ) : (
                  <ChevronRight aria-hidden="true" className="shrink-0 text-text-secondary transition-[color,transform] duration-200 group-hover:translate-x-0.5 group-hover:text-primary motion-reduce:transition-none" size={20} />
                )}
              </button>
            </li>
          ))}
        </ul>
        {startError ? <p className="px-5 pt-4 text-[0.8125rem] leading-5 text-error sm:px-6" role="alert">{startError}</p> : null}
      </div>
    </dialog>
  );
}

function getRoutineErrorMessage(kind: RoutineStartError["kind"]): string {
  if (kind === "empty") return "Esta rutina todavía no tiene días disponibles.";
  if (kind === "verification") return "No pudimos verificar si ya tenés un entrenamiento en curso. Intentá nuevamente.";
  if (kind === "start") return "No pudimos iniciar el entrenamiento. Intentá nuevamente.";
  return "No pudimos cargar esta rutina. Intentá nuevamente.";
}

function HistoryLink() {
  return (
    <Link className="group mt-3 flex min-h-12 w-full max-w-lg items-center justify-between gap-4 text-sm font-semibold text-text-secondary transition-colors hover:text-text-primary focus-visible:outline-primary" href="/history">
      <span>Actividad e historial</span>
      <ArrowRight aria-hidden="true" className="shrink-0 transition-transform duration-200 group-hover:translate-x-0.5 motion-reduce:transition-none" size={18} strokeWidth={1.75} />
    </Link>
  );
}

function PrimaryLink({ children, className, href }: { children: string; className?: string; href: string }) {
  return (
    <Link className={`${primaryLinkClass} gap-4 ${className ?? ""}`} href={href}>
      <span className="min-w-0 break-words leading-5">{children}</span>
      <ArrowRight aria-hidden="true" className="shrink-0" size={18} strokeWidth={2} />
    </Link>
  );
}

function formatMetric(value: number): string {
  return String(value).padStart(2, "0");
}
