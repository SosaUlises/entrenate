"use client";

import Link from "next/link";
import { useEffect, useRef, useState } from "react";
import { Button } from "@/components/ui/button";
import { cancelTrainingSessionAction } from "../actions/training.actions";
import {
  formatActiveSessionStart,
  isStaleActiveSession,
} from "../active-session-time";
import type { TrainingSession } from "../types/training.types";

type RecoveryStep = "decision" | "cancel";

type ActiveSessionRecoveryDialogProps = {
  initialStep?: RecoveryStep;
  onClose: () => void;
  onResolved: () => void;
  onUnauthenticated: () => void;
  session: Pick<TrainingSession, "id" | "horaInicio">;
};

export function ActiveSessionRecoveryDialog({
  initialStep = "decision",
  onClose,
  onResolved,
  onUnauthenticated,
  session,
}: ActiveSessionRecoveryDialogProps) {
  const dialogRef = useRef<HTMLDialogElement>(null);
  const cancelPendingRef = useRef(false);
  const [step, setStep] = useState<RecoveryStep>(initialStep);
  const [cancelPending, setCancelPending] = useState(false);
  const [cancelError, setCancelError] = useState(false);

  useEffect(() => {
    const dialog = dialogRef.current;
    if (dialog && !dialog.open) dialog.showModal();
  }, []);

  async function cancelSession() {
    if (cancelPendingRef.current) return;

    cancelPendingRef.current = true;
    setCancelPending(true);
    setCancelError(false);

    try {
      const result = await cancelTrainingSessionAction(session.id);
      if (result.status === "success" || result.status === "not-found" || result.status === "conflict") {
        dialogRef.current?.close();
        onResolved();
        return;
      }

      if (result.status === "unauthenticated") {
        dialogRef.current?.close();
        onUnauthenticated();
        return;
      }

      setCancelError(true);
    } catch {
      setCancelError(true);
    } finally {
      cancelPendingRef.current = false;
      setCancelPending(false);
    }
  }

  const stale = isStaleActiveSession(session.horaInicio);

  return (
    <dialog
      aria-labelledby="active-session-recovery-title"
      aria-modal="true"
      className="mt-auto max-h-[calc(100dvh-1rem)] w-full max-w-full overflow-y-auto rounded-t-container border border-border bg-surface-elevated p-0 text-text-primary shadow-elevated backdrop:bg-black/75 sm:m-auto sm:w-[calc(100%-2rem)] sm:max-w-md sm:rounded-container"
      onCancel={(event) => {
        if (cancelPendingRef.current) event.preventDefault();
      }}
      onClick={(event) => {
        if (event.target === event.currentTarget && !cancelPendingRef.current) {
          event.currentTarget.close();
        }
      }}
      onClose={onClose}
      ref={dialogRef}
    >
      <div className="p-5 pb-[max(1.25rem,env(safe-area-inset-bottom))] sm:p-6">
        {step === "decision" ? (
          <>
            <p className="text-xs font-semibold uppercase tracking-[0.12em] text-info">
              {stale ? "ENTRENAMIENTO PENDIENTE" : "ENTRENAMIENTO EN CURSO"}
            </p>
            <h2 className="mt-3 font-brand text-2xl font-bold leading-tight" id="active-session-recovery-title">
              {stale ? "Tenés un entrenamiento pendiente." : "Ya tenés un entrenamiento en curso."}
            </h2>
            <p className="mt-3 text-sm leading-6 text-text-secondary">
              {formatActiveSessionStart(session.horaInicio)}. Podés continuarlo o cancelarlo antes de iniciar otro.
            </p>
            <Link
              className="mt-6 flex min-h-[3.25rem] w-full items-center justify-center rounded-control-sm bg-primary-strong px-5 text-sm font-bold text-text-primary transition-colors hover:bg-primary focus-visible:outline-primary"
              href="/training"
            >
              Continuar entrenamiento
            </Link>
            <button
              className="mt-2 min-h-11 w-full text-sm font-semibold text-text-secondary transition-colors hover:text-error focus-visible:outline-primary"
              onClick={() => {
                setCancelError(false);
                setStep("cancel");
              }}
              type="button"
            >
              Cancelar entrenamiento actual
            </button>
          </>
        ) : (
          <>
            <h2 className="font-brand text-xl font-bold leading-tight" id="active-session-recovery-title">
              ¿Cancelar este entrenamiento?
            </h2>
            <p className="mt-3 text-sm leading-6 text-text-secondary">
              Las series que ya registraste quedarán disponibles en Historial como una sesión cancelada.
            </p>
            {cancelError ? (
              <p className="mt-3 text-sm text-error" role="alert">
                No pudimos cancelar el entrenamiento.
              </p>
            ) : null}
            <div className="mt-6 flex flex-col-reverse gap-2 sm:flex-row sm:justify-end">
              <Button
                disabled={cancelPending}
                onClick={() => {
                  if (initialStep === "decision") {
                    setCancelError(false);
                    setStep("decision");
                  } else {
                    dialogRef.current?.close();
                  }
                }}
                variant="secondary"
              >
                Volver
              </Button>
              <Button isLoading={cancelPending} onClick={() => void cancelSession()}>
                Cancelar entrenamiento
              </Button>
            </div>
          </>
        )}
      </div>
    </dialog>
  );
}
