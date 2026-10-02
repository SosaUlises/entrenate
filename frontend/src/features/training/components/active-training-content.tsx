"use client";

import { ArrowLeft, ArrowLeftRight, Check, ChevronRight, ClipboardList, Ellipsis, Play, X } from "lucide-react";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { useEffect, useRef, useState } from "react";
import { Button } from "@/components/ui/button";
import { Spinner } from "@/components/ui/spinner";
import { getExercisesAction } from "@/features/exercises/actions/get-exercises.action";
import { useTrainingProfileGate } from "@/features/training-profile/gate/training-profile-gate";
import {
  cancelTrainingSessionAction, completeTrainingSessionAction,
  getActiveTrainingSessionAction,
} from "../actions/training.actions";
import { areTargetSetsComplete } from "../training-position";
import type { TrainingSession, TrainingSet } from "../types/training.types";
import { FreeTrainingView } from "./free-training-view";
import { GuidedTrainingView } from "./guided-training-view";

type ViewState = { status: "loading" | "empty" | "error" } | { status: "ready"; session: TrainingSession };
type FinishMode = "complete" | "cancel";
type TrainingMode = "guided" | "free" | null;
type PendingNavigation = "home" | "mode";

export function ActiveTrainingContent() {
  const router = useRouter();
  const { invalidateSession } = useTrainingProfileGate();
  const [view, setView] = useState<ViewState>({ status: "loading" });
  const [requestKey, setRequestKey] = useState(0);
  const [mode, setMode] = useState<TrainingMode>(null);
  const [finishMode, setFinishMode] = useState<FinishMode | null>(null);
  const [finishPending, setFinishPending] = useState(false);
  const [finishError, setFinishError] = useState(false);
  const [isDirty, setIsDirty] = useState(false);
  const [pendingNavigation, setPendingNavigation] = useState<PendingNavigation | null>(null);
  const [sessionNotice, setSessionNotice] = useState<string | null>(null);
  const [workoutGuideIds, setWorkoutGuideIds] = useState<Record<string, string>>({});
  const catalogRequestedRef = useRef(false);
  const dialogRef = useRef<HTMLDialogElement>(null);
  const navigationDialogRef = useRef<HTMLDialogElement>(null);
  const finishPendingRef = useRef(false);
  const actionsRef = useRef<HTMLDetailsElement>(null);
  const actionsTriggerRef = useRef<HTMLElement>(null);

  useEffect(() => {
    function closeOnOutside(event: PointerEvent) {
      if (actionsRef.current?.open && event.target instanceof Node && !actionsRef.current.contains(event.target)) {
        actionsRef.current.open = false;
      }
    }

    function closeOnEscape(event: KeyboardEvent) {
      if (event.key === "Escape" && actionsRef.current?.open) {
        actionsRef.current.open = false;
        actionsTriggerRef.current?.focus();
      }
    }

    document.addEventListener("pointerdown", closeOnOutside);
    document.addEventListener("keydown", closeOnEscape);
    return () => {
      document.removeEventListener("pointerdown", closeOnOutside);
      document.removeEventListener("keydown", closeOnEscape);
    };
  }, []);

  useEffect(() => {
    if (pendingNavigation && !navigationDialogRef.current?.open) {
      navigationDialogRef.current?.showModal();
    }
  }, [pendingNavigation]);

  useEffect(() => {
    let active = true;
    void getActiveTrainingSessionAction().then((result) => {
      if (!active) return;
      if (result.status === "unauthenticated") {
        invalidateSession();
        router.replace("/login");
      } else if (result.status === "success") {
        setView({ status: "ready", session: result.data });
      } else {
        setView({ status: result.status === "not-found" ? "empty" : "error" });
      }
    }, () => { if (active) setView({ status: "error" }); });
    return () => { active = false; };
  }, [invalidateSession, requestKey, router]);

  useEffect(() => {
    if (mode !== "guided" || view.status !== "ready" || catalogRequestedRef.current) return;
    catalogRequestedRef.current = true;

    void getExercisesAction().then((result) => {
      if (result.status === "unauthenticated") {
        invalidateSession();
        router.replace("/login");
      } else if (result.status === "ready") {
        const ids: Record<string, string> = {};
        for (const exercise of result.exercises) {
          if (exercise.workoutGuideId) ids[exercise.id] = exercise.workoutGuideId;
        }
        setWorkoutGuideIds(ids);
      }
    }, () => {
      // Technique is optional; a catalog failure must not interrupt the session.
    });
  }, [invalidateSession, mode, router, view.status]);

  function reload() {
    setIsDirty(false);
    setView({ status: "loading" });
    setRequestKey((key) => key + 1);
  }

  function openFinish(mode: FinishMode) {
    if (finishPendingRef.current) return;
    setFinishError(false);
    setFinishMode(mode);
    dialogRef.current?.showModal();
  }

  function completeNavigation(action: PendingNavigation) {
    navigationDialogRef.current?.close();
    setIsDirty(false);

    if (action === "home") {
      router.push("/home");
    } else {
      setMode(null);
    }
  }

  function requestNavigation(action: PendingNavigation) {
    actionsRef.current?.removeAttribute("open");

    if (isDirty) {
      setPendingNavigation(action);
      return;
    }

    completeNavigation(action);
  }

  async function finish() {
    if (view.status !== "ready" || !finishMode || finishPendingRef.current) return;
    finishPendingRef.current = true;
    setFinishPending(true);
    setFinishError(false);
    try {
      const result = finishMode === "complete"
        ? await completeTrainingSessionAction(view.session.id)
        : await cancelTrainingSessionAction(view.session.id);
      if (result.status === "success") {
        dialogRef.current?.close();
        setView({ status: "empty" });
        setSessionNotice(null);
        router.replace("/home");
      } else if (result.status === "unauthenticated") {
        invalidateSession();
        router.replace("/login");
      } else if (result.status === "not-found") {
        dialogRef.current?.close();
        setSessionNotice("La sesión ya no está disponible.");
        setView({ status: "empty" });
      } else if (result.status === "conflict") {
        dialogRef.current?.close();
        setSessionNotice("La sesión ya no está en curso. Actualizamos tu entrenamiento activo.");
        reload();
      } else {
        setFinishError(true);
      }
    } catch {
      setFinishError(true);
    } finally {
      finishPendingRef.current = false;
      setFinishPending(false);
    }
  }

  function recordSet(exerciseId: string, set: TrainingSet) {
    setView((current) => current.status === "ready" ? {
      status: "ready",
      session: {
        ...current.session,
        ejercicios: current.session.ejercicios.map((exercise) => exercise.id === exerciseId ? {
          ...exercise,
          series: [...exercise.series.filter((item) => item.numeroSerie !== set.numeroSerie), set],
        } : exercise),
      },
    } : current);
  }

  const session = view.status === "ready" ? view.session : null;
  const hasIncompleteTargets = session ? !areTargetSetsComplete(session) : false;
  const targetsComplete = Boolean(session && !hasIncompleteTargets);
  const shouldShowRecoveredFinalState = targetsComplete && mode === null;
  const showTrainingActions = Boolean(session && (mode === "free" || (mode === "guided" && !targetsComplete)));

  return (
    <section className={`mx-auto min-w-0 w-full max-w-xl ${mode === "guided" ? "pb-10" : ""}`} aria-labelledby="training-title">
      <div className="flex min-w-0 max-w-full items-start gap-1">
        <button
          aria-label="Salir y continuar después"
          className="flex size-11 shrink-0 items-center justify-center rounded-control-sm text-primary hover:bg-surface focus-visible:outline-primary"
          onClick={() => requestNavigation("home")}
          type="button"
        >
          <ArrowLeft aria-hidden="true" size={20} />
        </button>
        <div className={`min-w-0 flex-1 ${mode === "guided" || mode === "free" ? "pt-3" : "pt-2"}`}>
          <h1 className={mode === "guided" || mode === "free" ? "text-[0.8125rem] font-semibold uppercase leading-5 tracking-[0.1em] text-text-secondary" : "font-brand text-xl font-bold text-text-primary"} id="training-title">Entrenamiento en curso</h1>
        </div>
        {showTrainingActions ? (
          <details className="group relative shrink-0" ref={actionsRef}>
            <summary aria-label="Acciones del entrenamiento" className="flex size-11 cursor-pointer list-none items-center justify-center rounded-control-sm text-text-secondary transition-colors hover:bg-surface/50 hover:text-text-primary focus-visible:outline-2 focus-visible:outline-primary group-open:bg-primary/10 group-open:text-primary [&::-webkit-details-marker]:hidden" ref={actionsTriggerRef}>
              <Ellipsis aria-hidden="true" size={20} strokeWidth={1.75} />
            </summary>
            <div className="absolute right-0 z-20 mt-1.5 w-56 max-w-[calc(100vw-2rem)] rounded-control border border-border/60 bg-surface-elevated p-2 shadow-elevated">
              <button className="flex min-h-11 w-full items-center gap-3 rounded-control-sm px-3 text-left text-sm font-medium text-text-primary transition-colors hover:bg-surface focus-visible:outline-2 focus-visible:outline-primary" onClick={() => requestNavigation("mode")} type="button">
                <ArrowLeftRight aria-hidden="true" className="shrink-0 text-text-secondary" size={17} strokeWidth={1.75} />
                Cambiar modo
              </button>
              <button className="flex min-h-11 w-full items-center gap-3 rounded-control-sm px-3 text-left text-sm font-medium text-text-primary transition-colors hover:bg-primary/8 focus-visible:outline-2 focus-visible:outline-primary" onClick={() => { actionsRef.current?.removeAttribute("open"); openFinish("complete"); }} type="button">
                <Check aria-hidden="true" className="shrink-0 text-primary" size={17} strokeWidth={2} />
                Finalizar entrenamiento
              </button>
              <div className="my-1 border-t border-border/60" />
              <button className="flex min-h-11 w-full items-center gap-3 rounded-control-sm px-3 text-left text-sm font-medium text-error/85 transition-colors hover:bg-error/8 hover:text-error focus-visible:outline-2 focus-visible:outline-primary" onClick={() => { actionsRef.current?.removeAttribute("open"); openFinish("cancel"); }} type="button">
                <X aria-hidden="true" className="shrink-0" size={17} strokeWidth={1.9} />
                Cancelar entrenamiento
              </button>
            </div>
          </details>
        ) : session ? <button className="min-h-11 shrink-0 px-2 text-xs text-text-secondary hover:text-text-primary focus-visible:outline-primary" onClick={() => openFinish("cancel")} type="button">Cancelar</button> : null}
      </div>

      {sessionNotice ? <p className="mt-5 text-sm text-text-secondary" role="status">{sessionNotice}</p> : null}
      {view.status === "loading" ? <div className="grid min-h-48 place-items-center"><Spinner className="size-6 text-primary" label="Cargando entrenamiento" /></div> : null}
      {view.status === "empty" ? <div className="mt-6 rounded-card border border-border bg-surface p-5"><p className="text-sm text-text-primary">No tenés un entrenamiento activo.</p><Link className="mt-3 inline-flex min-h-11 items-center text-sm font-semibold text-primary" href="/routines">Ver mis rutinas</Link></div> : null}
      {view.status === "error" ? <div className="mt-6 rounded-card border border-border bg-surface p-5"><p className="text-sm text-text-primary">No pudimos cargar tu entrenamiento.</p><Button className="mt-4" onClick={reload} variant="secondary">Reintentar</Button></div> : null}

      {session ? (
        <>
          {shouldShowRecoveredFinalState ? (
            <GuidedTrainingView
              onDirtyChange={setIsDirty}
              onFinish={() => openFinish("complete")}
              onMissing={reload}
              onSaved={recordSet}
              onUnauthenticated={() => { invalidateSession(); router.replace("/login"); }}
              session={session}
              workoutGuideIds={workoutGuideIds}
            />
          ) : mode === null ? (
            <div className="mt-8 min-w-0 max-w-full">
              <h2 className="font-brand text-xl font-bold text-text-primary">¿Cómo querés entrenar hoy?</h2>
              <p className="mt-2 text-sm text-text-secondary">Elegí cómo querés registrar tus series.</p>
              <div className="mt-6 space-y-3">
                <button className="flex min-h-24 min-w-0 w-full max-w-full items-center gap-4 rounded-card border border-primary/25 bg-primary/5 p-4 text-left transition-colors hover:bg-primary/10 focus-visible:outline-2 focus-visible:outline-primary" onClick={() => setMode("guided")} type="button">
                  <Play aria-hidden="true" className="size-5 shrink-0 text-primary" strokeWidth={1.75} />
                  <span className="min-w-0 flex-1">
                    <span className="block font-brand text-base font-bold text-text-primary">Modo guiado</span>
                    <span className="mt-1 block text-sm leading-5 text-text-secondary">Registrá cada serie mientras entrenás y seguí tus descansos paso a paso.</span>
                  </span>
                  <ChevronRight aria-hidden="true" className="size-4 shrink-0 text-text-secondary" strokeWidth={1.75} />
                </button>
                <button className="flex min-h-24 min-w-0 w-full max-w-full items-center gap-4 rounded-card border border-border/60 bg-surface/50 p-4 text-left transition-colors hover:bg-surface focus-visible:outline-2 focus-visible:outline-primary" onClick={() => setMode("free")} type="button">
                  <ClipboardList aria-hidden="true" className="size-5 shrink-0 text-text-secondary" strokeWidth={1.75} />
                  <span className="min-w-0 flex-1">
                    <span className="block font-brand text-base font-bold text-text-primary">Carga libre</span>
                    <span className="mt-1 block text-sm leading-5 text-text-secondary">Entrená a tu ritmo y completá los datos de tus series cuando quieras.</span>
                  </span>
                  <ChevronRight aria-hidden="true" className="size-4 shrink-0 text-text-secondary" strokeWidth={1.75} />
                </button>
              </div>
            </div>
          ) : null}
          {mode === "guided" ? (
            <GuidedTrainingView
              onDirtyChange={setIsDirty}
              onFinish={() => openFinish("complete")}
              onMissing={reload}
              onSaved={recordSet}
              onUnauthenticated={() => { invalidateSession(); router.replace("/login"); }}
              session={session}
              workoutGuideIds={workoutGuideIds}
            />
          ) : null}
          {mode === "free" ? (
            <FreeTrainingView
              onDirtyChange={setIsDirty}
              onFinish={() => openFinish("complete")}
              onMissing={reload}
              onSaved={recordSet}
              onUnauthenticated={() => { invalidateSession(); router.replace("/login"); }}
              session={session}
            />
          ) : null}
        </>
      ) : null}

      <dialog aria-labelledby="finish-title" aria-modal="true" className="m-auto w-[calc(100%-2rem)] max-w-[calc(100vw-2rem)] rounded-card border border-border bg-surface-elevated p-5 text-text-primary shadow-elevated backdrop:bg-black/75 sm:max-w-sm" onCancel={(event) => { if (finishPendingRef.current) event.preventDefault(); }} onClick={(event) => { if (event.target === event.currentTarget && !finishPendingRef.current) event.currentTarget.close(); }} onClose={() => setFinishMode(null)} ref={dialogRef}>
        <h2 className="font-brand text-lg font-bold" id="finish-title">{finishMode === "cancel" ? "¿Cancelar entrenamiento?" : "¿Finalizar entrenamiento?"}</h2>
        <p className="mt-2 text-sm leading-6 text-text-secondary">{finishMode === "cancel" ? "La sesión se guardará como cancelada." : "Las series registradas quedarán guardadas en tu historial."}</p>
        {finishMode === "complete" && hasIncompleteTargets ? <p className="mt-2 text-sm text-warning">Todavía hay series sin completar.</p> : null}
        {finishError ? <p className="mt-3 text-sm text-error" role="alert">No pudimos actualizar el entrenamiento. Intentá nuevamente.</p> : null}
        <div className="mt-5 flex flex-wrap justify-end gap-2">
          <Button disabled={finishPending} onClick={() => dialogRef.current?.close()} variant="secondary">Seguir entrenando</Button>
          <Button isLoading={finishPending} onClick={() => void finish()}>{finishMode === "cancel" ? "Cancelar sesión" : "Finalizar"}</Button>
        </div>
      </dialog>

      <dialog
        aria-labelledby="discard-input-title"
        aria-modal="true"
        className="m-auto w-[calc(100%-2rem)] max-w-[calc(100vw-2rem)] rounded-card border border-border bg-surface-elevated p-5 text-text-primary shadow-elevated backdrop:bg-black/75 sm:max-w-sm"
        onClick={(event) => {
          if (event.target === event.currentTarget) event.currentTarget.close();
        }}
        onClose={() => setPendingNavigation(null)}
        ref={navigationDialogRef}
      >
        <h2 className="font-brand text-lg font-bold" id="discard-input-title">¿Descartar cambios sin guardar?</h2>
        <p className="mt-2 text-sm leading-6 text-text-secondary">
          Los datos que escribiste en esta serie todavía no se guardaron.
        </p>
        <div className="mt-5 flex flex-wrap justify-end gap-2">
          <Button onClick={() => navigationDialogRef.current?.close()} variant="secondary">Seguir entrenando</Button>
          <Button onClick={() => pendingNavigation && completeNavigation(pendingNavigation)}>
            {pendingNavigation === "mode" ? "Descartar y cambiar" : "Descartar y salir"}
          </Button>
        </div>
      </dialog>
    </section>
  );
}
