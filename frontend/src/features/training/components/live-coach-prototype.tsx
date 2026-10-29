"use client";

import { X } from "lucide-react";
import { useRef, useState, type MouseEvent } from "react";
import { cn } from "@/lib/class-names";

type LiveCoachState =
  | "idle"
  | "listening"
  | "thinking"
  | "speaking"
  | "success"
  | "error";

type StatePresentation = {
  description: string;
  indicatorClassName: string;
  label: string;
};

const stateOrder: LiveCoachState[] = [
  "idle",
  "listening",
  "thinking",
  "speaking",
  "success",
  "error",
];

const statePresentations: Record<LiveCoachState, StatePresentation> = {
  idle: {
    description: "Disponible sin intervenir en tu entrenamiento.",
    indicatorClassName: "bg-text-muted",
    label: "En espera",
  },
  listening: {
    description: "Entrada activa. En V0 este estado es solo una simulaci\u00f3n.",
    indicatorClassName: "bg-thermal-accent motion-safe:animate-pulse",
    label: "Escuchando",
  },
  thinking: {
    description: "Procesando contexto. En V0 no se analizan datos reales.",
    indicatorClassName: "bg-steel motion-safe:animate-pulse",
    label: "Procesando",
  },
  speaking: {
    description: "Respuesta en curso. En V0 no se reproduce audio.",
    indicatorClassName: "bg-thermal-accent motion-safe:animate-pulse",
    label: "Respondiendo",
  },
  success: {
    description: "Interacci\u00f3n completada sin modificar la sesi\u00f3n.",
    indicatorClassName: "bg-success",
    label: "Completado",
  },
  error: {
    description: "La interacci\u00f3n no pudo continuar. Tu sesi\u00f3n sigue intacta.",
    indicatorClassName: "bg-error",
    label: "No disponible",
  },
};

export function LiveCoachPrototype() {
  const dialogRef = useRef<HTMLDialogElement>(null);
  const [state, setState] = useState<LiveCoachState>("idle");
  const presentation = statePresentations[state];

  function openSheet() {
    const dialog = dialogRef.current;
    if (!dialog || dialog.open) return;
    dialog.showModal();
  }

  function handleBackdropClick(event: MouseEvent<HTMLDialogElement>) {
    const bounds = event.currentTarget.getBoundingClientRect();
    const outside =
      event.clientX < bounds.left ||
      event.clientX > bounds.right ||
      event.clientY < bounds.top ||
      event.clientY > bounds.bottom;

    if (outside) event.currentTarget.close();
  }

  return (
    <>
      <button
        aria-label={`Abrir prototipo de Live Coach. Estado: ${presentation.label}`}
        className="flex min-h-12 shrink-0 items-center gap-2 rounded-control-sm px-2 text-text-secondary transition-colors duration-150 hover:bg-surface-active hover:text-text-primary focus-visible:outline-2 focus-visible:outline-focus motion-reduce:transition-none"
        onClick={openSheet}
        type="button"
      >
        <LiveCoachFace compact state={state} />
        <span className="hidden text-[0.8125rem] font-semibold min-[380px]:inline">
          Live Coach
        </span>
      </button>

      <dialog
        aria-describedby="live-coach-prototype-description"
        aria-labelledby="live-coach-prototype-title"
        aria-modal="true"
        className="fixed inset-x-0 bottom-0 top-auto m-0 max-h-[calc(100dvh-0.5rem)] w-full max-w-none overflow-y-auto rounded-t-container border border-border-strong bg-surface-elevated p-0 text-text-primary shadow-elevated backdrop:bg-black/75 sm:inset-0 sm:m-auto sm:w-[calc(100%-2rem)] sm:max-w-md sm:rounded-container"
        onClick={handleBackdropClick}
        onClose={() => setState("idle")}
        ref={dialogRef}
      >
        <div className="mx-auto mt-2 h-1 w-10 rounded-full bg-border-strong sm:hidden" />
        <div className="px-5 pb-[calc(1.25rem+env(safe-area-inset-bottom))] pt-4 sm:p-6">
          <header className="flex items-start gap-3">
            <div className="min-w-0 flex-1">
              <h2
                className="font-brand text-2xl font-bold leading-8 text-text-primary"
                id="live-coach-prototype-title"
              >
                Live Coach
              </h2>
              <p
                className="mt-2 max-w-sm text-sm leading-6 text-text-secondary"
                id="live-coach-prototype-description"
              >
                Presencia visual para asistencia durante el entrenamiento.
                Este prototipo no escucha ni env&iacute;a datos.
              </p>
            </div>
            <button
              aria-label="Cerrar Live Coach"
              className="flex size-11 shrink-0 items-center justify-center rounded-control-sm text-text-secondary transition-colors duration-150 hover:bg-surface-interactive hover:text-text-primary focus-visible:outline-2 focus-visible:outline-focus motion-reduce:transition-none"
              onClick={() => dialogRef.current?.close()}
              type="button"
            >
              <X aria-hidden="true" size={20} />
            </button>
          </header>

          <section
            aria-labelledby="live-coach-current-state"
            aria-live="polite"
            className="mt-6 border-y border-border/70 py-6"
          >
            <div className="flex items-center gap-5">
              <LiveCoachFace state={state} />
              <div className="min-w-0 flex-1">
                <div className="flex items-center gap-2">
                  <span
                    aria-hidden="true"
                    className={cn(
                      "size-2 shrink-0 rounded-full motion-reduce:animate-none",
                      presentation.indicatorClassName,
                    )}
                  />
                  <h3
                    className="font-brand text-lg font-bold leading-6 text-text-primary"
                    id="live-coach-current-state"
                  >
                    {presentation.label}
                  </h3>
                </div>
                <p className="mt-1.5 text-[0.8125rem] leading-5 text-text-secondary">
                  {presentation.description}
                </p>
              </div>
            </div>
          </section>

          <fieldset className="mt-6">
            <legend className="text-sm font-semibold leading-5 text-text-primary">
              Simular estado visual
            </legend>
            <p className="mt-1 text-[0.8125rem] leading-5 text-text-muted">
              Solo cambia la presentaci&oacute;n local de este panel.
            </p>
            <div className="mt-3 grid grid-cols-2 gap-2">
              {stateOrder.map((candidate) => {
                const candidatePresentation = statePresentations[candidate];
                const selected = state === candidate;

                return (
                  <button
                    aria-pressed={selected}
                    className={cn(
                      "flex min-h-12 items-center gap-2 rounded-control-sm border px-3 text-left text-[0.8125rem] font-semibold transition-colors duration-150 focus-visible:outline-2 focus-visible:outline-focus motion-reduce:transition-none",
                      selected
                        ? "border-thermal-accent/50 bg-surface-active text-text-primary"
                        : "border-border bg-surface-interactive text-text-secondary hover:border-border-strong hover:text-text-primary",
                    )}
                    key={candidate}
                    onClick={() => setState(candidate)}
                    type="button"
                  >
                    <span
                      aria-hidden="true"
                      className={cn(
                        "size-1.5 shrink-0 rounded-full",
                        candidatePresentation.indicatorClassName.replace(
                          " motion-safe:animate-pulse",
                          "",
                        ),
                      )}
                    />
                    {candidatePresentation.label}
                  </button>
                );
              })}
            </div>
          </fieldset>

          <p className="mt-6 border-t border-border/60 pt-4 text-[0.8125rem] leading-5 text-text-muted">
            Sin micr&oacute;fono, voz, an&aacute;lisis, memoria ni cambios sobre la
            sesi&oacute;n.
          </p>
        </div>
      </dialog>
    </>
  );
}

function LiveCoachFace({
  compact = false,
  state,
}: {
  compact?: boolean;
  state: LiveCoachState;
}) {
  const active = state === "listening" || state === "thinking";
  const speaking = state === "speaking";

  return (
    <span
      aria-hidden="true"
      className={cn(
        "relative flex shrink-0 items-center justify-center overflow-hidden rounded-control-sm border border-border-strong bg-surface-interactive",
        compact ? "h-7 w-10" : "h-16 w-24",
      )}
    >
      <span
        className={cn(
          "absolute rounded-full bg-technical-white transition-opacity duration-200 motion-reduce:transition-none",
          compact ? "left-2.5 top-2 h-1.5 w-1.5" : "left-6 top-[1.35rem] h-2.5 w-2.5",
          active && "motion-safe:animate-pulse",
        )}
      />
      <span
        className={cn(
          "absolute rounded-full bg-technical-white transition-opacity duration-200 motion-reduce:transition-none",
          compact ? "right-2.5 top-2 h-1.5 w-1.5" : "right-6 top-[1.35rem] h-2.5 w-2.5",
          active && "motion-safe:animate-pulse",
        )}
      />
      <span
        className={cn(
          "absolute left-1/2 -translate-x-1/2 rounded-full bg-thermal-accent transition-[height,width] duration-200 motion-reduce:transition-none",
          compact
            ? speaking
              ? "bottom-1.5 h-1.5 w-2"
              : "bottom-1.5 h-0.5 w-2"
            : speaking
              ? "bottom-3.5 h-2 w-4 motion-safe:animate-pulse"
              : "bottom-4 h-1 w-4",
        )}
      />
    </span>
  );
}
