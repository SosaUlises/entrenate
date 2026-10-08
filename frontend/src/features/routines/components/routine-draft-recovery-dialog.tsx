"use client";

import { useEffect, useRef } from "react";
import { Button } from "@/components/ui/button";
import { formatRoutineDraftUpdatedAt, type StoredRoutineDraft } from "../routine-draft-storage";

export function RoutineDraftRecoveryDialog({
  onContinue,
  onDiscard,
  storedDraft,
}: {
  onContinue: () => void;
  onDiscard: () => void;
  storedDraft: StoredRoutineDraft;
}) {
  const dialogRef = useRef<HTMLDialogElement>(null);

  useEffect(() => {
    const dialog = dialogRef.current;
    if (dialog && !dialog.open) dialog.showModal();
  }, []);

  function continueDraft() {
    dialogRef.current?.close();
    onContinue();
  }

  function discardDraft() {
    dialogRef.current?.close();
    onDiscard();
  }

  return (
    <dialog
      aria-describedby="routine-draft-recovery-description"
      aria-labelledby="routine-draft-recovery-title"
      aria-modal="true"
      className="mt-auto max-h-[calc(100dvh-1rem)] w-full max-w-full overflow-y-auto rounded-t-container border border-border bg-surface-elevated p-0 text-text-primary shadow-elevated backdrop:bg-black/75 sm:m-auto sm:w-[calc(100%-2rem)] sm:max-w-md sm:rounded-container"
      onCancel={(event) => {
        event.preventDefault();
        continueDraft();
      }}
      onClick={(event) => {
        if (event.target === event.currentTarget) continueDraft();
      }}
      ref={dialogRef}
    >
      <div className="mx-auto mt-2 h-1 w-10 rounded-full bg-border-strong sm:hidden" />
      <div className="px-5 pb-[max(1.25rem,env(safe-area-inset-bottom))] pt-5 sm:p-6">
        <p className="text-xs font-semibold uppercase tracking-[0.12em] text-info">BORRADOR GUARDADO</p>
        <h2 className="mt-3 font-brand text-2xl font-bold leading-tight" id="routine-draft-recovery-title">
          Tenés una rutina sin terminar
        </h2>
        <p className="mt-3 text-sm leading-6 text-text-secondary" id="routine-draft-recovery-description">
          Última modificación · {formatRoutineDraftUpdatedAt(storedDraft.updatedAt)}
        </p>
        <div className="mt-6 flex flex-col gap-2 sm:flex-row-reverse sm:justify-start">
          <Button autoFocus className="min-h-12 sm:min-w-32" onClick={continueDraft}>
            Continuar
          </Button>
          <Button className="min-h-12 sm:min-w-32" onClick={discardDraft} variant="secondary">
            Descartar
          </Button>
        </div>
      </div>
    </dialog>
  );
}
