"use client";

import { AlertCircle, Mic, RotateCcw, Square, X } from "lucide-react";
import { useEffect, useRef, useState, type MouseEvent } from "react";
import { Spinner } from "@/components/ui/spinner";
import { transcribeLiveAudioAction } from "@/features/coach/actions/transcribe-live-audio.action";
import { cn } from "@/lib/class-names";

type LiveCoachState =
  | "idle"
  | "requestingPermission"
  | "listening"
  | "transcribing"
  | "success"
  | "error";

type FaceState = "idle" | "listening" | "thinking" | "speaking" | "success" | "error";

type StatePresentation = {
  description: string;
  indicatorClassName: string;
  label: string;
};

const statePresentations: Record<LiveCoachState, StatePresentation> = {
  idle: {
    description: "Tocá el micrófono para hacer una consulta corta sobre tu entrenamiento.",
    indicatorClassName: "bg-text-muted",
    label: "En espera",
  },
  requestingPermission: {
    description: "Solicitando acceso al micrófono...",
    indicatorClassName: "bg-thermal-accent motion-safe:animate-pulse",
    label: "Permiso",
  },
  listening: {
    description: "Grabando audio... Tocá detener para transcribir (máximo 15 s).",
    indicatorClassName: "bg-thermal-accent motion-safe:animate-pulse",
    label: "Escuchando",
  },
  transcribing: {
    description: "Transcribiendo habla con Groq Whisper...",
    indicatorClassName: "bg-steel motion-safe:animate-pulse",
    label: "Transcribiendo",
  },
  success: {
    description: "Transcripción reconocida exitosamente.",
    indicatorClassName: "bg-success",
    label: "Completado",
  },
  error: {
    description: "No se pudo completar la grabación o la transcripción.",
    indicatorClassName: "bg-error",
    label: "No disponible",
  },
};

function mapToFaceState(state: LiveCoachState): FaceState {
  switch (state) {
    case "idle":
      return "idle";
    case "requestingPermission":
    case "listening":
      return "listening";
    case "transcribing":
      return "thinking";
    case "success":
      return "success";
    case "error":
      return "error";
  }
}

function getBestSupportedMimeType(): string {
  if (typeof window === "undefined" || typeof MediaRecorder === "undefined") {
    return "";
  }
  const candidates = [
    "audio/webm;codecs=opus",
    "audio/mp4",
    "audio/aac",
    "audio/ogg;codecs=opus",
    "audio/webm",
  ];
  for (const candidate of candidates) {
    if (MediaRecorder.isTypeSupported(candidate)) {
      return candidate;
    }
  }
  return "";
}

function getAudioFileExtension(mimeType: string): string {
  const clean = mimeType.toLowerCase();
  if (clean.includes("mp4") || clean.includes("aac")) return "audio.mp4";
  if (clean.includes("ogg")) return "audio.ogg";
  if (clean.includes("wav")) return "audio.wav";
  return "audio.webm";
}

export function LiveCoachPrototype() {
  const dialogRef = useRef<HTMLDialogElement>(null);
  const [state, setState] = useState<LiveCoachState>("idle");
  const [transcript, setTranscript] = useState<string | null>(null);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);

  const streamRef = useRef<MediaStream | null>(null);
  const recorderRef = useRef<MediaRecorder | null>(null);
  const chunksRef = useRef<Blob[]>([]);
  const timerRef = useRef<NodeJS.Timeout | null>(null);
  const startTimeRef = useRef<number>(0);

  function cleanupMedia() {
    if (timerRef.current) {
      clearTimeout(timerRef.current);
      timerRef.current = null;
    }
    if (recorderRef.current && recorderRef.current.state !== "inactive") {
      try {
        recorderRef.current.stop();
      } catch {
        // ignore if already stopped
      }
    }
    recorderRef.current = null;
    if (streamRef.current) {
      streamRef.current.getTracks().forEach((track) => track.stop());
      streamRef.current = null;
    }
    chunksRef.current = [];
  }

  useEffect(() => {
    return () => {
      cleanupMedia();
    };
  }, []);

  async function startRecording() {
    cleanupMedia();
    setErrorMessage(null);
    setTranscript(null);

    if (
      typeof window === "undefined" ||
      !navigator?.mediaDevices?.getUserMedia ||
      typeof MediaRecorder === "undefined"
    ) {
      setErrorMessage("Tu navegador no soporta grabación de audio o requiere conexión segura (HTTPS).");
      setState("error");
      return;
    }

    setState("requestingPermission");

    let stream: MediaStream;
    try {
      stream = await navigator.mediaDevices.getUserMedia({
        audio: {
          echoCancellation: true,
          noiseSuppression: true,
          autoGainControl: true,
          channelCount: 1,
        },
      });
    } catch (err: unknown) {
      const isDenied =
        err instanceof DOMException &&
        (err.name === "NotAllowedError" || err.name === "PermissionDeniedError");
      setErrorMessage(
        isDenied
          ? "Permiso de micrófono denegado. Habilitá el acceso al micrófono en la configuración de tu navegador."
          : "No se pudo acceder al micrófono del dispositivo.",
      );
      setState("error");
      return;
    }

    streamRef.current = stream;
    const mimeType = getBestSupportedMimeType();

    let recorder: MediaRecorder;
    try {
      recorder = mimeType ? new MediaRecorder(stream, { mimeType }) : new MediaRecorder(stream);
    } catch {
      recorder = new MediaRecorder(stream);
    }

    recorderRef.current = recorder;
    chunksRef.current = [];

    recorder.ondataavailable = (event) => {
      if (event.data && event.data.size > 0) {
        chunksRef.current.push(event.data);
      }
    };

    recorder.onstop = async () => {
      const elapsed = Date.now() - startTimeRef.current;
      const finalChunks = [...chunksRef.current];
      cleanupMedia();

      if (finalChunks.length === 0) {
        setErrorMessage("No se grabaron datos de audio.");
        setState("error");
        return;
      }

      const recordedBlob = new Blob(finalChunks, {
        type: recorder.mimeType || mimeType || "audio/webm",
      });

      if (recordedBlob.size < 1024) {
        setErrorMessage("La grabación fue demasiado corta o vacía. Hablá más cerca del micrófono.");
        setState("error");
        return;
      }

      setState("transcribing");

      const formData = new FormData();
      const filename = getAudioFileExtension(recorder.mimeType || mimeType);
      formData.append("audio", recordedBlob, filename);
      formData.append("interactionId", crypto.randomUUID());
      formData.append("clientTurnId", crypto.randomUUID());
      formData.append("mimeType", recorder.mimeType || mimeType || "audio/webm");
      formData.append("durationMilliseconds", Math.round(elapsed).toString());

      const result = await transcribeLiveAudioAction(formData);

      if (result.status === "success") {
        setTranscript(result.data.transcript);
        setState("success");
      } else {
        setErrorMessage(result.message || "Ocurrió un error al procesar el audio.");
        setState("error");
      }
    };

    startTimeRef.current = Date.now();
    recorder.start();
    setState("listening");

    // Auto-stop at 15 seconds
    timerRef.current = setTimeout(() => {
      if (recorderRef.current && recorderRef.current.state === "recording") {
        recorderRef.current.stop();
      }
    }, 15000);
  }

  function stopRecording() {
    if (timerRef.current) {
      clearTimeout(timerRef.current);
      timerRef.current = null;
    }
    if (recorderRef.current && recorderRef.current.state === "recording") {
      recorderRef.current.stop();
    }
  }

  function handleCancelOrClose() {
    cleanupMedia();
    setState("idle");
    setTranscript(null);
    setErrorMessage(null);
    dialogRef.current?.close();
  }

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

    if (outside) handleCancelOrClose();
  }

  const presentation = statePresentations[state];
  const faceState = mapToFaceState(state);

  return (
    <>
      <button
        aria-label={`Abrir Live Coach. Estado: ${presentation.label}`}
        className="flex min-h-12 shrink-0 items-center gap-2 rounded-control-sm px-2 text-text-secondary transition-colors duration-150 hover:bg-surface-active hover:text-text-primary focus-visible:outline-2 focus-visible:outline-focus motion-reduce:transition-none"
        onClick={openSheet}
        type="button"
      >
        <LiveCoachFace compact state={faceState} />
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
        onClose={handleCancelOrClose}
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
                Asistencia de voz en tiempo real para tu entrenamiento.
              </p>
            </div>
            <button
              aria-label="Cerrar Live Coach"
              className="flex size-11 shrink-0 items-center justify-center rounded-control-sm text-text-secondary transition-colors duration-150 hover:bg-surface-interactive hover:text-text-primary focus-visible:outline-2 focus-visible:outline-focus motion-reduce:transition-none"
              onClick={handleCancelOrClose}
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
              <LiveCoachFace state={faceState} />
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
                  {errorMessage ?? presentation.description}
                </p>
              </div>
            </div>
          </section>

          {/* Interactive Control Area */}
          <div className="mt-6 space-y-4">
            {state === "idle" && (
              <button
                className="flex min-h-12 w-full items-center justify-center gap-2.5 rounded-control-sm bg-thermal-accent px-4 py-3 text-sm font-semibold text-black transition-colors duration-150 hover:bg-thermal-accent/90 focus-visible:outline-2 focus-visible:outline-focus motion-reduce:transition-none"
                onClick={startRecording}
                type="button"
              >
                <Mic aria-hidden="true" size={18} />
                <span>Hablar al Coach</span>
              </button>
            )}

            {state === "requestingPermission" && (
              <div className="flex min-h-12 w-full items-center justify-center gap-2.5 rounded-control-sm border border-border bg-surface-interactive px-4 py-3 text-sm font-semibold text-text-secondary">
                <Spinner className="size-4" />
                <span>Esperando permiso de micrófono...</span>
              </div>
            )}

            {state === "listening" && (
              <button
                className="flex min-h-12 w-full items-center justify-center gap-2.5 rounded-control-sm border border-thermal-accent/60 bg-thermal-accent/15 px-4 py-3 text-sm font-semibold text-thermal-accent transition-colors duration-150 hover:bg-thermal-accent/25 focus-visible:outline-2 focus-visible:outline-focus motion-reduce:transition-none"
                onClick={stopRecording}
                type="button"
              >
                <Square aria-hidden="true" className="fill-current" size={16} />
                <span>Detener y transcribir</span>
              </button>
            )}

            {state === "transcribing" && (
              <div className="flex min-h-12 w-full items-center justify-center gap-2.5 rounded-control-sm border border-border bg-surface-interactive px-4 py-3 text-sm font-semibold text-text-secondary">
                <Spinner className="size-4" />
                <span>Transcribiendo habla...</span>
              </div>
            )}

            {state === "success" && transcript && (
              <div className="space-y-4">
                <div className="rounded-control-sm border border-border-strong bg-surface-interactive p-4">
                  <span className="text-xs font-semibold uppercase tracking-wider text-text-muted">
                    Transcripción reconocida
                  </span>
                  <p className="mt-2 text-base font-medium leading-relaxed text-text-primary">
                    &ldquo;{transcript}&rdquo;
                  </p>
                </div>

                <button
                  className="flex min-h-12 w-full items-center justify-center gap-2.5 rounded-control-sm border border-border-strong bg-surface-interactive px-4 py-3 text-sm font-semibold text-text-primary transition-colors duration-150 hover:bg-surface-active focus-visible:outline-2 focus-visible:outline-focus motion-reduce:transition-none"
                  onClick={startRecording}
                  type="button"
                >
                  <RotateCcw aria-hidden="true" size={16} />
                  <span>Grabar otra consulta</span>
                </button>
              </div>
            )}

            {state === "error" && (
              <div className="space-y-4">
                <div className="flex items-start gap-3 rounded-control-sm border border-error/40 bg-error/10 p-3.5 text-[0.8125rem] text-text-primary">
                  <AlertCircle aria-hidden="true" className="mt-0.5 shrink-0 text-error" size={18} />
                  <p className="leading-5">{errorMessage ?? "Ocurrió un error inesperado."}</p>
                </div>

                <button
                  className="flex min-h-12 w-full items-center justify-center gap-2.5 rounded-control-sm border border-border-strong bg-surface-interactive px-4 py-3 text-sm font-semibold text-text-primary transition-colors duration-150 hover:bg-surface-active focus-visible:outline-2 focus-visible:outline-focus motion-reduce:transition-none"
                  onClick={startRecording}
                  type="button"
                >
                  <RotateCcw aria-hidden="true" size={16} />
                  <span>Reintentar grabación</span>
                </button>
              </div>
            )}
          </div>

          <p className="mt-6 border-t border-border/60 pt-4 text-[0.8125rem] leading-5 text-text-muted">
            Live Coach V0 STT — El audio se procesa en memoria y nunca se almacena en el servidor.
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
  state: FaceState;
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
