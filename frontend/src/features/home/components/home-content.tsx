"use client";

import { ArrowRight, CalendarDays, ChevronRight, Search, SlidersHorizontal, X } from "lucide-react";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { useEffect, useRef, useState } from "react";
import { Button, buttonClassName } from "@/components/ui/button";
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
      <section aria-labelledby="home-error-title" className="w-full max-w-lg py-1">
        <h2 className="max-w-md font-brand text-[2rem] font-bold leading-[1.08] text-text-primary" id="home-error-title">
          No pudimos actualizar tu estado
        </h2>
        <p className="mt-3 max-w-sm text-[0.9375rem] leading-6 text-text-secondary">
          No podemos confirmar si tenés un entrenamiento en curso. Intentá nuevamente.
        </p>
        <Button
          className="mt-7 max-w-sm"
          fullWidth
          onClick={() => {
            setState({ status: "loading" });
            setRequestKey((key) => key + 1);
          }}
          size="lg"
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
      <section aria-labelledby="active-training-title" className="w-full max-w-lg py-1">
        <StatusLabel label="Entrenamiento pendiente" />
        <h2 className="mt-4 max-w-md break-words font-brand text-[2rem] font-bold leading-[1.06] text-text-primary sm:text-4xl" id="active-training-title">
          {state.exerciseName ?? "Tu entrenamiento"}
        </h2>
        <div className="mt-6 max-w-sm border-y border-border py-4">
          <p className="text-[0.875rem] font-medium leading-5 text-text-primary">
            {formatActiveSessionStart(state.horaInicio)}
          </p>
          <p className="mt-1.5 text-[0.8125rem] font-semibold leading-5 text-text-muted">
            {state.registeredSetCount} {state.registeredSetCount === 1 ? "serie registrada" : "series registradas"}
          </p>
        </div>
        <HomeActionLink className="mt-6" href="/training" variant="secondary">
          Continuar entrenamiento
        </HomeActionLink>
        <Button
          className="mt-2 px-1 text-[0.8125rem]"
          onClick={() => setConfirmingCancellation(true)}
          size="sm"
          variant="ghost"
        >
          Cancelar sesión
        </Button>
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
      <section aria-labelledby="active-training-title" className="w-full max-w-lg py-1">
        <StatusLabel label="Entrenamiento listo" tone="success" />
        <h2 className="mt-4 max-w-md font-brand text-[2rem] font-bold leading-[1.06] text-text-primary sm:text-4xl" id="active-training-title">
          Objetivos completados.
        </h2>
        <p className="mt-3 max-w-sm text-[0.9375rem] leading-6 text-text-secondary">
          Tu sesión está lista para finalizar.
        </p>
        <dl className="mt-6 grid max-w-sm grid-cols-2 border-y border-border py-4">
          <div className="min-w-0 border-r border-border pr-4">
            <dt className="text-[0.8125rem] font-semibold leading-5 text-text-muted">Ejercicios</dt>
            <dd className="mt-1 font-brand text-2xl font-bold tabular-nums text-text-primary">{state.exerciseCount}</dd>
          </div>
          <div className="min-w-0 pl-5">
            <dt className="text-[0.8125rem] font-semibold leading-5 text-text-muted">Series registradas</dt>
            <dd className="mt-1 font-brand text-2xl font-bold tabular-nums text-text-primary">{state.registeredSetCount}</dd>
          </div>
        </dl>
        <HomeActionLink className="mt-6" href="/training" variant="secondary">
          Continuar entrenamiento
        </HomeActionLink>
        <HistoryLink />
      </section>
    );
  }

  return (
    <section aria-labelledby="active-training-title" className="w-full max-w-lg py-1">
      <StatusLabel label="Entrenamiento en curso" />
      <h2 className="mt-4 max-w-md break-words font-brand text-[2rem] font-bold leading-[1.06] text-text-primary sm:text-4xl" id="active-training-title">
        {state.exerciseName}
      </h2>
      <p className="mt-3 text-[0.8125rem] font-semibold leading-5 text-text-muted">
        {formatActiveSessionStart(state.horaInicio)}
      </p>

      <dl className="mt-6 grid max-w-sm grid-cols-2 border-y border-border py-4">
        <div className="min-w-0 border-r border-border pr-4">
          <dt className="text-[0.8125rem] font-semibold uppercase leading-5 tracking-[0.08em] text-text-muted">Ejercicio</dt>
          <dd className="mt-2 font-brand text-3xl font-bold leading-none tabular-nums text-steel">
            {formatMetric(state.exercisePosition)} <span className="text-xl text-text-secondary">/ {formatMetric(state.exerciseCount)}</span>
          </dd>
        </div>
        <div className="min-w-0 pl-5">
          <dt className="text-[0.8125rem] font-semibold uppercase leading-5 tracking-[0.08em] text-text-muted">Serie</dt>
          <dd className="mt-2 font-brand text-3xl font-bold leading-none tabular-nums text-steel">
            {formatMetric(state.setNumber)} <span className="text-xl text-text-secondary">/ {formatMetric(state.targetSetCount)}</span>
          </dd>
        </div>
      </dl>

      <HomeActionLink className="mt-6" href="/training" variant="secondary">
        Continuar entrenamiento
      </HomeActionLink>
      <HistoryLink />
    </section>
  );
}

function NoRoutinesState() {
  return (
    <section aria-labelledby="first-routine-title" className="w-full max-w-lg py-1">
      <h2 className="max-w-md font-brand text-[2rem] font-bold leading-[1.06] text-text-primary sm:text-4xl" id="first-routine-title">
        Creá tu primera rutina.
      </h2>
      <p className="mt-4 max-w-sm text-[0.9375rem] leading-6 text-text-secondary">
        Organizá tus ejercicios y empezá a registrar tus entrenamientos.
      </p>
      <HomeActionLink className="mt-8" href="/routines/new">
        Crear rutina
      </HomeActionLink>
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
      setSelectedRoutine({ ...result.routine, dias: days });
    } catch {
      setStartError({ routineId, kind: "detail" });
    } finally {
      setLoadingRoutineId(null);
    }
  }

  return (
    <section aria-labelledby="available-routines-title" className="w-full max-w-lg py-1">
      <h2 className="font-brand text-[2rem] font-bold leading-[1.06] text-text-primary" id="available-routines-title">
        Elegí una rutina.
      </h2>

      {showSearch ? (
        <div className="relative mt-7 max-w-md">
          <label className="sr-only" htmlFor="home-routine-search">Buscar rutina por nombre</label>
          <Search aria-hidden="true" className="pointer-events-none absolute left-4 top-1/2 -translate-y-1/2 text-text-secondary" size={18} strokeWidth={1.75} />
          <Input
            autoComplete="off"
            className="min-h-12! rounded-control-sm pl-11 pr-4 text-[0.9375rem]"
            id="home-routine-search"
            onChange={(event) => setSearchQuery(event.target.value)}
            placeholder="Buscar rutina"
            type="search"
            value={searchQuery}
          />
        </div>
      ) : null}

      {visibleRoutines.length > 0 ? (
        <ul className={`${showSearch ? "mt-4" : "mt-7"} border-t border-border`}>
          {visibleRoutines.map((routine) => {
          const isLoading = loadingRoutineId === routine.id;
          const error = startError?.routineId === routine.id ? getRoutineErrorMessage(startError.kind) : null;

          return (
            <li className="border-b border-border" key={routine.id}>
              <button
                aria-busy={isLoading}
                className="group flex min-h-[4.75rem] w-full min-w-0 items-center gap-4 rounded-control-sm px-1 py-2.5 text-left transition-[background-color,transform] duration-200 hover:bg-surface/45 active:scale-[0.995] active:bg-surface-active/70 focus-visible:bg-surface/55 focus-visible:outline-focus disabled:cursor-wait disabled:opacity-60 motion-reduce:transition-none motion-reduce:active:scale-100"
                disabled={Boolean(loadingRoutineId || startingDayId)}
                onClick={() => void chooseRoutine(routine.id)}
                type="button"
              >
                <span className="min-w-0 flex-1">
                  <span className="line-clamp-2 break-words font-brand text-lg font-bold leading-6 text-text-primary">
                    {routine.nombre}
                  </span>
                  <span className="mt-1 block text-[0.8125rem] font-semibold leading-5 text-text-secondary">
                    {routine.cantidadDias} {routine.cantidadDias === 1 ? "día" : "días"}
                  </span>
                </span>
                {isLoading ? (
                  <Spinner className="size-5 shrink-0 text-primary" label={`Cargando ${routine.nombre}`} />
                ) : (
                  <ChevronRight aria-hidden="true" className="shrink-0 text-text-muted transition-[color,transform] duration-200 group-hover:translate-x-0.5 group-hover:text-steel motion-reduce:transition-none" size={20} />
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
            className="flex size-11 shrink-0 items-center justify-center rounded-control-sm text-text-secondary transition-colors duration-200 hover:bg-surface-active hover:text-text-primary focus-visible:outline-focus motion-reduce:transition-none"
            disabled={Boolean(startingDayId)}
            onClick={() => dialogRef.current?.close()}
            type="button"
          >
            <X aria-hidden="true" size={20} />
          </button>
        </div>

        <ul className="border-y border-border">
          {routine.dias.map((day) => (
            <li className="border-b border-border px-5 py-2 last:border-b-0 sm:px-6" key={day.id}>
              <button
                className="group flex min-h-[4.25rem] w-full min-w-0 items-center gap-4 rounded-control-sm px-1 py-2 text-left transition-colors duration-200 hover:bg-surface-active/60 focus-visible:bg-surface-active/60 focus-visible:outline-focus disabled:cursor-wait disabled:opacity-60 motion-reduce:transition-none"
                disabled={Boolean(startingDayId)}
                onClick={() => onSelect(day.id)}
                type="button"
              >
                <span className="min-w-0 flex-1">
                  <span className="block break-words font-brand text-lg font-bold leading-6 text-text-primary">
                    {day.nombre}
                  </span>
                  <span className="mt-1 block text-[0.8125rem] font-semibold leading-5 text-text-secondary">
                    {day.ejercicios.length} {day.ejercicios.length === 1 ? "ejercicio" : "ejercicios"}
                  </span>
                </span>
                {startingDayId === day.id ? (
                  <Spinner className="size-5 shrink-0 text-primary" label={`Iniciando ${day.nombre}`} />
                ) : (
                  <ChevronRight aria-hidden="true" className="shrink-0 text-text-muted transition-[color,transform] duration-200 group-hover:translate-x-0.5 group-hover:text-steel motion-reduce:transition-none" size={20} />
                )}
              </button>
              {routine.activa ? (
                <Link
                  className="mb-1 flex min-h-11 w-full items-center gap-2 rounded-control-sm px-1 text-[0.8125rem] font-semibold text-text-secondary transition-colors hover:bg-surface-interactive hover:text-text-primary focus-visible:outline-2 focus-visible:outline-focus"
                  href={`/coach/adaptive-training/${encodeURIComponent(routine.id)}/${encodeURIComponent(day.id)}`}
                >
                  <SlidersHorizontal aria-hidden="true" className="text-steel" size={16} />
                  Adaptar hoy
                </Link>
              ) : null}
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

function StatusLabel({ label, tone = "active" }: { label: string; tone?: "active" | "success" }) {
  return (
    <p className="flex min-h-5 items-center gap-2 text-[0.8125rem] font-semibold uppercase leading-5 tracking-[0.1em] text-text-muted">
      <span
        aria-hidden="true"
        className={`h-0.5 w-4 shrink-0 rounded-full ${tone === "success" ? "bg-success" : "bg-thermal-accent"}`}
      />
      {label}
    </p>
  );
}

function HistoryLink() {
  return (
    <Link className="group mt-6 flex min-h-[3.25rem] w-full max-w-lg items-center justify-between gap-4 border-t border-border px-1 py-2 text-[0.9375rem] font-semibold text-text-secondary transition-[background-color,color] duration-200 hover:bg-surface-interactive/45 hover:text-text-primary active:bg-surface-active/60 focus-visible:outline-focus motion-reduce:transition-none" href="/history">
      <span className="flex min-w-0 items-center gap-3">
        <CalendarDays aria-hidden="true" className="shrink-0 text-steel" size={18} strokeWidth={1.75} />
        <span>Actividad e historial</span>
      </span>
      <ChevronRight aria-hidden="true" className="shrink-0 text-steel transition-transform duration-200 group-hover:translate-x-0.5 motion-reduce:transition-none" size={18} strokeWidth={1.75} />
    </Link>
  );
}

function HomeActionLink({ children, className, href, variant = "primary" }: { children: string; className?: string; href: string; variant?: "primary" | "secondary" }) {
  return (
    <Link
      className={buttonClassName({
        className: `max-w-sm justify-between gap-4 overflow-hidden ${variant === "secondary" ? "[&_svg]:text-thermal-accent" : ""} ${className ?? ""}`,
        fullWidth: true,
        size: "lg",
        variant,
      })}
      href={href}
    >
      <span className="min-w-0 break-words leading-5">{children}</span>
      <ArrowRight aria-hidden="true" className="shrink-0" size={18} strokeWidth={2} />
    </Link>
  );
}

function formatMetric(value: number): string {
  return String(value).padStart(2, "0");
}
