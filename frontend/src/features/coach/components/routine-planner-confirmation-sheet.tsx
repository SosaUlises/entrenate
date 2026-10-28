"use client";

import { Check, X } from "lucide-react";
import { useRouter } from "next/navigation";
import { useRef, useState, type MouseEvent } from "react";
import { Button } from "@/components/ui/button";
import { useTrainingProfileGate } from "@/features/training-profile/gate/training-profile-gate";
import { confirmRoutinePlannerProposalAction } from "../actions/confirm-routine-planner-proposal.action";
import type {
  RoutinePlannerConfirmationResponse,
  RoutinePlannerProposalResponse,
} from "../types/routine-planner.types";

type ConfirmationState =
  | { status: "confirming" | "error" | "idle" | "stale" }
  | {
      confirmation: RoutinePlannerConfirmationResponse;
      status: "success";
    };

export function RoutinePlannerConfirmationSheet({
  onRegenerate,
  proposal,
}: {
  onRegenerate: () => Promise<void>;
  proposal: RoutinePlannerProposalResponse;
}) {
  const router = useRouter();
  const { invalidateSession } = useTrainingProfileGate();
  const dialogRef = useRef<HTMLDialogElement>(null);
  const requestInFlightRef = useRef(false);
  const [state, setState] = useState<ConfirmationState>({ status: "idle" });

  if (proposal.feasibility === "NotFeasible") {
    return (
      <div className="border-y border-border/70 py-5">
        <p className="text-sm font-semibold leading-6 text-text-primary">
          Esta propuesta no se puede crear todavía.
        </p>
        <p className="mt-1.5 text-[0.8125rem] leading-5 text-text-secondary">
          Podés revisarla y preguntarle al Coach, pero necesitás generar una
          propuesta factible antes de confirmarla.
        </p>
      </div>
    );
  }

  function openSheet() {
    if (state.status === "success") {
      router.push(`/routines/${encodeURIComponent(state.confirmation.routineId)}`);
      return;
    }

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

  async function confirmProposal() {
    if (requestInFlightRef.current || state.status === "stale") return;

    requestInFlightRef.current = true;
    setState({ status: "confirming" });

    try {
      const result = await confirmRoutinePlannerProposalAction(
        proposal.proposalId,
        proposal.proposalHash,
      );

      if (result.status === "unauthenticated") {
        invalidateSession();
        router.replace("/login");
        return;
      }

      if (result.status === "stale") {
        setState({ status: "stale" });
        return;
      }

      if (result.status === "ready") {
        setState({
          confirmation: result.confirmation,
          status: "success",
        });
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

  const isConfirming = state.status === "confirming";

  return (
    <>
      <Button fullWidth onClick={openSheet} size="lg">
        {state.status === "success" ? "Ver rutina" : "Crear esta rutina"}
      </Button>

      <dialog
        aria-describedby="routine-confirmation-description"
        aria-labelledby="routine-confirmation-title"
        aria-modal="true"
        className="mt-auto w-full max-w-full rounded-t-container border border-border-strong bg-surface-elevated p-0 text-text-primary shadow-elevated backdrop:bg-black/75 sm:m-auto sm:w-[calc(100%-2rem)] sm:max-w-md sm:rounded-container"
        onCancel={(event) => {
          if (requestInFlightRef.current) event.preventDefault();
        }}
        onClick={handleBackdropClick}
        ref={dialogRef}
      >
        <div className="mx-auto mt-2 h-1 w-10 rounded-full bg-border-strong sm:hidden" />
        <div className="px-5 pb-[calc(1.25rem+env(safe-area-inset-bottom))] pt-5 sm:p-6">
          {state.status === "success" ? (
            <SuccessContent
              confirmation={state.confirmation}
              onViewRoutine={() =>
                router.push(
                  `/routines/${encodeURIComponent(state.confirmation.routineId)}`,
                )
              }
            />
          ) : state.status === "stale" ? (
            <StaleContent onRegenerate={regenerateProposal} />
          ) : (
            <>
              <div className="flex items-start gap-3">
                <div className="min-w-0 flex-1">
                  <h2
                    className="font-brand text-xl font-bold leading-7 text-text-primary"
                    id="routine-confirmation-title"
                  >
                    Crear rutina
                  </h2>
                  <p
                    className="mt-3 text-sm leading-6 text-text-secondary"
                    id="routine-confirmation-description"
                  >
                    La propuesta se guardará como rutina y quedará activa. Tu
                    rutina activa anterior se conservará, pero dejará de estar
                    activa.
                  </p>
                </div>
                <button
                  aria-label="Cerrar confirmación"
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
                  No pudimos crear la rutina. Podés reintentar sin perder la
                  propuesta.
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
                    ? "Confirmando"
                    : state.status === "error"
                      ? "Reintentar"
                      : "Confirmar rutina"}
                </Button>
              </div>
            </>
          )}
        </div>
      </dialog>
    </>
  );
}

function SuccessContent({
  confirmation,
  onViewRoutine,
}: {
  confirmation: RoutinePlannerConfirmationResponse;
  onViewRoutine: () => void;
}) {
  return (
    <div aria-live="polite">
      <span
        aria-hidden="true"
        className="flex size-9 items-center justify-center rounded-control-sm border border-border-strong bg-surface-active text-thermal-accent"
      >
        <Check size={19} strokeWidth={2.25} />
      </span>
      <h2
        className="mt-4 font-brand text-2xl font-bold leading-8 text-text-primary"
        id="routine-confirmation-title"
      >
        Rutina creada
      </h2>
      <p
        className="mt-2 text-sm leading-6 text-text-secondary"
        id="routine-confirmation-description"
      >
        {confirmation.isActive
          ? "Tu nueva rutina ya está activa y lista para entrenar."
          : "Tu rutina se guardó correctamente."}
      </p>
      <Button className="mt-6" fullWidth onClick={onViewRoutine} size="lg">
        Ver rutina
      </Button>
    </div>
  );
}

function StaleContent({ onRegenerate }: { onRegenerate: () => void }) {
  return (
    <div role="alert">
      <h2
        className="font-brand text-xl font-bold leading-7 text-text-primary"
        id="routine-confirmation-title"
      >
        La propuesta cambió
      </h2>
      <p
        className="mt-3 text-sm leading-6 text-text-secondary"
        id="routine-confirmation-description"
      >
        Tu perfil, equipamiento o los datos usados para generarla cambiaron.
        Generá una nueva propuesta antes de crear la rutina.
      </p>
      <Button className="mt-6" fullWidth onClick={onRegenerate} size="lg">
        Generar nueva propuesta
      </Button>
    </div>
  );
}
