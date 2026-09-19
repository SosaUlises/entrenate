"use client";

import { ChevronRight, ClipboardList, Ellipsis, Play } from "lucide-react";
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

export function ActiveTrainingContent() {
  const router = useRouter();
  const { invalidateSession } = useTrainingProfileGate();
  const [view, setView] = useState<ViewState>({ status: "loading" });
  const [requestKey, setRequestKey] = useState(0);
  const [mode, setMode] = useState<TrainingMode>(null);
  const [finishMode, setFinishMode] = useState<FinishMode | null>(null);
  const [finishPending, setFinishPending] = useState(false);
  const [finishError, setFinishError] = useState(false);
  const [sessionNotice, setSessionNotice] = useState<string | null>(null);
  const [workoutGuideIds, setWorkoutGuideIds] = useState<Record<string, string>>({});
  const catalogRequestedRef = useRef(false);
  const dialogRef = useRef<HTMLDialogElement>(null);
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
    setView({ status: "loading" });
    setRequestKey((key) => key + 1);
  }

  function openFinish(mode: FinishMode) {
    if (finishPendingRef.current) return;
    setFinishError(false);
    setFinishMode(mode);
    dialogRef.current?.showModal();
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

  return (
    <section className={`mx-auto w-full max-w-xl ${mode === "guided" ? "pb-10" : ""}`} aria-labelledby="training-title">
      <div className="flex items-start justify-between gap-3">
        <div className="min-w-0">
          <h1 className="font-brand text-xl font-bold text-text-primary" id="training-title">Entrenamiento en curso</h1>
        </div>
        {session && mode === "guided" ? (
          <details className="relative shrink-0" ref={actionsRef}>
            <summary aria-label="Acciones del entrenamiento" className="flex size-11 cursor-pointer list-none items-center justify-center rounded-control-sm text-text-secondary hover:bg-surface/50 hover:text-text-primary focus-visible:outline-2 focus-visible:outline-primary [&::-webkit-details-marker]:hidden" ref={actionsTriggerRef}>
              <Ellipsis aria-hidden="true" size={20} strokeWidth={1.75} />
            </summary>
            <div className="absolute right-0 z-20 mt-1 w-48 rounded-control border border-border bg-surface-elevated p-1 shadow-elevated">
              <button className="flex min-h-11 w-full items-center rounded-control-sm px-3 text-left text-sm text-text-primary hover:bg-surface focus-visible:outline-2 focus-visible:outline-primary" onClick={() => { actionsRef.current?.removeAttribute("open"); setMode(null); }} type="button">Cambiar modo</button>
              <div className="my-1 border-t border-border/60" />
              <button className="flex min-h-11 w-full items-center rounded-control-sm px-3 text-left text-sm text-text-secondary hover:bg-surface hover:text-text-primary focus-visible:outline-2 focus-visible:outline-primary" onClick={() => { actionsRef.current?.removeAttribute("open"); openFinish("cancel"); }} type="button">Cancelar entrenamiento</button>
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
          {mode === null ? (
            <div className="mt-8">
              <h2 className="font-brand text-xl font-bold text-text-primary">¿Cómo querés entrenar hoy?</h2>
              <p className="mt-2 text-sm text-text-secondary">Elegí cómo querés registrar tus series.</p>
              <div className="mt-6 space-y-3">
                <button className="flex min-h-24 w-full items-center gap-4 rounded-card border border-primary/25 bg-primary/5 p-4 text-left transition-colors hover:bg-primary/10 focus-visible:outline-2 focus-visible:outline-primary" onClick={() => setMode("guided")} type="button">
                  <Play aria-hidden="true" className="size-5 shrink-0 text-primary" strokeWidth={1.75} />
                  <span className="min-w-0 flex-1">
                    <span className="block font-brand text-base font-bold text-text-primary">Modo guiado</span>
                    <span className="mt-1 block text-sm leading-5 text-text-secondary">Registrá cada serie mientras entrenás y seguí tus descansos paso a paso.</span>
                  </span>
                  <ChevronRight aria-hidden="true" className="size-4 shrink-0 text-text-secondary" strokeWidth={1.75} />
                </button>
                <button className="flex min-h-24 w-full items-center gap-4 rounded-card border border-border/60 bg-surface/50 p-4 text-left transition-colors hover:bg-surface focus-visible:outline-2 focus-visible:outline-primary" onClick={() => setMode("free")} type="button">
                  <ClipboardList aria-hidden="true" className="size-5 shrink-0 text-text-secondary" strokeWidth={1.75} />
                  <span className="min-w-0 flex-1">
                    <span className="block font-brand text-base font-bold text-text-primary">Carga libre</span>
                    <span className="mt-1 block text-sm leading-5 text-text-secondary">Entrená a tu ritmo y completá los datos de tus series cuando quieras.</span>
                  </span>
                  <ChevronRight aria-hidden="true" className="size-4 shrink-0 text-text-secondary" strokeWidth={1.75} />
                </button>
              </div>
            </div>
          ) : mode === "free" ? (
            <div className="mt-4 text-right">
              <button className="min-h-11 px-1 text-sm text-text-secondary hover:text-text-primary focus-visible:outline-2 focus-visible:outline-primary" onClick={() => setMode(null)} type="button">Cambiar modo</button>
            </div>
          ) : null}
          {mode === "guided" ? (
            <GuidedTrainingView
              onFinish={() => openFinish("complete")}
              onMissing={reload}
              onSaved={recordSet}
              onUnauthenticated={() => { invalidateSession(); router.replace("/login"); }}
              session={session}
              workoutGuideIds={workoutGuideIds}
            />
          ) : null}
          {mode === "guided" && hasIncompleteTargets ? <Button className="mt-2 min-h-11 text-xs font-medium text-text-secondary hover:text-text-primary" fullWidth onClick={() => openFinish("complete")} variant="text">Finalizar entrenamiento</Button> : null}
          {mode === "free" ? (
            <FreeTrainingView
              onMissing={reload}
              onSaved={recordSet}
              onUnauthenticated={() => { invalidateSession(); router.replace("/login"); }}
              session={session}
            />
          ) : null}
          {mode === "free" ? <Button className="mt-7" fullWidth onClick={() => openFinish("complete")}>Finalizar entrenamiento</Button> : null}
        </>
      ) : null}

      <dialog aria-labelledby="finish-title" aria-modal="true" className="m-auto w-[calc(100%-2rem)] max-w-sm rounded-card border border-border bg-surface-elevated p-5 text-text-primary shadow-elevated backdrop:bg-black/75" onCancel={(event) => { if (finishPendingRef.current) event.preventDefault(); }} onClick={(event) => { if (event.target === event.currentTarget && !finishPendingRef.current) event.currentTarget.close(); }} onClose={() => setFinishMode(null)} ref={dialogRef}>
        <h2 className="font-brand text-lg font-bold" id="finish-title">{finishMode === "cancel" ? "¿Cancelar entrenamiento?" : "¿Finalizar entrenamiento?"}</h2>
        <p className="mt-2 text-sm leading-6 text-text-secondary">{finishMode === "cancel" ? "La sesión se guardará como cancelada." : "Las series registradas quedarán guardadas en tu historial."}</p>
        {finishMode === "complete" && hasIncompleteTargets ? <p className="mt-2 text-sm text-warning">Todavía hay series sin completar.</p> : null}
        {finishError ? <p className="mt-3 text-sm text-error" role="alert">No pudimos actualizar el entrenamiento. Intentá nuevamente.</p> : null}
        <div className="mt-5 flex flex-wrap justify-end gap-2">
          <Button disabled={finishPending} onClick={() => dialogRef.current?.close()} variant="secondary">Seguir entrenando</Button>
          <Button isLoading={finishPending} onClick={() => void finish()}>{finishMode === "cancel" ? "Cancelar sesión" : "Finalizar"}</Button>
        </div>
      </dialog>
    </section>
  );
}
