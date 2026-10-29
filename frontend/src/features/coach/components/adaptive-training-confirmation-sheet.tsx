"use client";

import { X } from "lucide-react";
import { useRouter } from "next/navigation";
import { useRef, useState, type MouseEvent } from "react";
import { Button } from "@/components/ui/button";
import { useTrainingProfileGate } from "@/features/training-profile/gate/training-profile-gate";
import { ActiveSessionRecoveryDialog } from "@/features/training/components/active-session-recovery-dialog";
import type { TrainingSession } from "@/features/training/types/training.types";
import { confirmSessionAdaptationProposalAction } from "../actions/confirm-session-adaptation-proposal.action";
import type {
  SessionAdaptationProposalResponse,
  SessionAdaptationRequest,
} from "../types/adaptive-training.types";

type ConfirmationState =
  | { status: "confirming" | "error" | "idle" | "stale" }
  | { session: TrainingSession; status: "active-session" };

export function AdaptiveTrainingConfirmationSheet({
  onInvalidate,
  onRegenerate,
  proposal,
  request,
}: {
  onInvalidate: () => void;
  onRegenerate: () => Promise<void>;
  proposal: SessionAdaptationProposalResponse;
  request: SessionAdaptationRequest;
}) {
  const router = useRouter();
  const { invalidateSession } = useTrainingProfileGate();
  const dialogRef = useRef<HTMLDialogElement>(null);
  const requestInFlightRef = useRef(false);
  const [state, setState] = useState<ConfirmationState>({ status: "idle" });

  const isNotFeasible = proposal.feasibility === "NotFeasible";
  const isConfirming = state.status === "confirming";

  function handleUnauthenticated() {
    invalidateSession();
    router.replace("/login");
  }

  function openSheet() {
    if (isNotFeasible) return;

    const dialog = dialogRef.current;
    if (!dialog || dialog.open) return;

    if (state.status === "error") setState({ status: "idle" });
    dialog.showModal();
  }

  function closeSheet() {
    if (requestInFlightRef.current) return;
    dialogRef.current?.close();
  }

  function handleBackdropClick(event: MouseEvent<HTMLDialogElement>) {
    if (requestInFlightRef.current || event.target !== event.currentTarget) {
      return;
    }

    event.currentTarget.close();
  }

  function handleDialogClose() {
    if (state.status === "stale") onInvalidate();
  }

  async function confirmProposal() {
    if (
      requestInFlightRef.current ||
      isNotFeasible ||
      state.status === "stale"
    ) {
      return;
    }

    requestInFlightRef.current = true;
    setState({ status: "confirming" });

    try {
      const result = await confirmSessionAdaptationProposalAction(
        proposal.proposalId,
        proposal.proposalHash,
        request,
      );

      if (result.status === "unauthenticated") {
        handleUnauthenticated();
        return;
      }

      if (result.status === "stale") {
        setState({ status: "stale" });
        return;
      }

      if (result.status === "active-session") {
        dialogRef.current?.close();
        setState({ session: result.session, status: "active-session" });
        return;
      }

      if (result.status === "ready") {
        if (!result.confirmation.sessionId) {
          setState({ status: "error" });
          return;
        }

        router.push("/training");
        return;
      }

      setState({ status: "error" });
    } catch {
      setState({ status: "error" });
    } finally {
      requestInFlightRef.current = false;
    }
  }

  function regenerateProposal() {
    dialogRef.current?.close();
    void onRegenerate();
  }

  return (
    <>
      <div>
        <Button
          disabled={isNotFeasible}
          fullWidth
          onClick={openSheet}
          size="lg"
        >
          Comenzar entrenamiento adaptado
        </Button>
        {isNotFeasible ? (
          <p className="mt-2 text-[0.8125rem] leading-5 text-text-muted">
            Edit&aacute; las condiciones para generar una propuesta que pueda
            confirmarse.
          </p>
        ) : null}
      </div>

      <dialog
        aria-describedby="adaptive-confirmation-description"
        aria-labelledby="adaptive-confirmation-title"
        aria-modal="true"
        className="mt-auto w-full max-w-full rounded-t-container border border-border-strong bg-surface-elevated p-0 text-text-primary shadow-elevated backdrop:bg-black/75 sm:m-auto sm:w-[calc(100%-2rem)] sm:max-w-md sm:rounded-container"
        onCancel={(event) => {
          if (requestInFlightRef.current) event.preventDefault();
        }}
        onClick={handleBackdropClick}
        onClose={handleDialogClose}
        ref={dialogRef}
      >
        <div className="mx-auto mt-2 h-1 w-10 rounded-full bg-border-strong sm:hidden" />
        <div className="px-5 pb-[calc(1.25rem+env(safe-area-inset-bottom))] pt-5 sm:p-6">
          {state.status === "stale" ? (
            <StaleContent onRegenerate={regenerateProposal} />
          ) : (
            <>
              <div className="flex items-start gap-3">
                <div className="min-w-0 flex-1">
                  <h2
                    className="font-brand text-xl font-bold leading-7 text-text-primary"
                    id="adaptive-confirmation-title"
                  >
                    Comenzar entrenamiento adaptado
                  </h2>
                  <p
                    className="mt-3 text-sm leading-6 text-text-secondary"
                    id="adaptive-confirmation-description"
                  >
                    Se iniciar&aacute; una sesi&oacute;n con la adaptaci&oacute;n
                    mostrada. Tu rutina base no se modifica: estos cambios
                    aplican solo al entrenamiento de hoy.
                  </p>
                </div>
                <button
                  aria-label="Cerrar confirmaci&oacute;n"
                  className="flex size-11 shrink-0 items-center justify-center rounded-control-sm text-text-secondary transition-colors duration-150 hover:bg-surface-interactive hover:text-text-primary focus-visible:outline-2 focus-visible:outline-focus disabled:cursor-wait disabled:text-text-muted motion-reduce:transition-none"
                  disabled={isConfirming}
                  onClick={closeSheet}
                  type="button"
                >
                  <X aria-hidden="true" size={20} />
                </button>
              </div>

              {state.status === "error" ? (
                <p
                  className="mt-4 text-[0.8125rem] leading-5 text-error"
                  role="alert"
                >
                  No pudimos iniciar el entrenamiento. Pod&eacute;s reintentar
                  sin perder la propuesta.
                </p>
              ) : null}

              <div className="mt-6 flex flex-col-reverse gap-3 sm:flex-row sm:justify-end">
                <Button
                  disabled={isConfirming}
                  onClick={closeSheet}
                  variant="secondary"
                >
                  Cancelar
                </Button>
                <Button
                  isLoading={isConfirming}
                  onClick={() => void confirmProposal()}
                >
                  {isConfirming
                    ? "Iniciando"
                    : state.status === "error"
                      ? "Reintentar"
                      : "Comenzar entrenamiento"}
                </Button>
              </div>
            </>
          )}
        </div>
      </dialog>

      {state.status === "active-session" ? (
        <ActiveSessionRecoveryDialog
          onClose={() => setState({ status: "idle" })}
          onResolved={() => setState({ status: "idle" })}
          onUnauthenticated={handleUnauthenticated}
          session={state.session}
        />
      ) : null}
    </>
  );
}

function StaleContent({ onRegenerate }: { onRegenerate: () => void }) {
  return (
    <div role="alert">
      <h2
        className="font-brand text-xl font-bold leading-7 text-text-primary"
        id="adaptive-confirmation-title"
      >
        La propuesta cambi&oacute;
      </h2>
      <p
        className="mt-3 text-sm leading-6 text-text-secondary"
        id="adaptive-confirmation-description"
      >
        Las condiciones usadas para generarla ya no son v&aacute;lidas.
        Gener&aacute; una nueva propuesta antes de iniciar el entrenamiento.
      </p>
      <Button className="mt-6" fullWidth onClick={onRegenerate} size="lg">
        Generar nueva propuesta
      </Button>
    </div>
  );
}
