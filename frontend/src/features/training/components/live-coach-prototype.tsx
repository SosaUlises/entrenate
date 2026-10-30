"use client";

import {
  AlertCircle,
  ArrowRight,
  MessageSquare,
  Mic,
  RotateCcw,
  Square,
  Volume2,
  VolumeX,
  X,
} from "lucide-react";
import { useEffect, useRef, useState, type FormEvent, type MouseEvent } from "react";
import { Button } from "@/components/ui/button";
import { respondLiveCoachAction } from "@/features/coach/actions/respond-live-coach.action";
import { transcribeLiveAudioAction } from "@/features/coach/actions/transcribe-live-audio.action";
import type {
  LiveCoachClientSnapshot,
  LiveCoachRespondResponse,
} from "@/features/coach/types/live-coach.types";
import { cn } from "@/lib/class-names";
import type { TrainingSession } from "../types/training.types";
import { LiveCoachFace, type LiveCoachFaceState } from "./live-coach-face";
import styles from "./live-coach-prototype.module.css";

type LiveCoachState =
  | "idle"
  | "requestingPermission"
  | "listening"
  | "transcribing"
  | "answering"
  | "speaking"
  | "success"
  | "error";

type StatePresentation = {
  description: string;
  label: string;
};

const statePresentations: Record<LiveCoachState, StatePresentation> = {
  idle: {
    description: "Tocá para hablar.",
    label: "En espera",
  },
  requestingPermission: {
    description: "Un momento.",
    label: "Preparando...",
  },
  listening: {
    description: "Tocá para enviar.",
    label: "Escuchando...",
  },
  transcribing: {
    description: "Procesando tu voz.",
    label: "Transcribiendo...",
  },
  answering: {
    description: "Un momento.",
    label: "Pensando...",
  },
  speaking: {
    description: "La respuesta está sonando.",
    label: "Respondiendo...",
  },
  success: {
    description: "Respuesta lista.",
    label: "Listo",
  },
  error: {
    description: "Intentá nuevamente.",
    label: "No pude responder",
  },
};

function getVisibleErrorMessage(message: string | null): string {
  if (!message) return "Intentá nuevamente.";

  const normalized = message.toLocaleLowerCase("es");
  if (normalized.includes("sesión activa")) return "No encontré una sesión activa.";
  if (normalized.includes("permiso") || normalized.includes("micrófono")) {
    return "Revisá el acceso al micrófono e intentá de nuevo.";
  }
  if (normalized.includes("audio") || normalized.includes("grabación")) {
    return "No pude escuchar el audio. Intentá de nuevo.";
  }

  return "No pude completar la consulta. Intentá de nuevo.";
}

function mapToFaceState(state: LiveCoachState): LiveCoachFaceState {
  switch (state) {
    case "idle":
      return "idle";
    case "requestingPermission":
    case "listening":
      return "listening";
    case "transcribing":
    case "answering":
      return "thinking";
    case "speaking":
      return "speaking";
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

type LiveCoachPrototypeProps = {
  session?: TrainingSession | null;
};

export function LiveCoachPrototype({ session }: LiveCoachPrototypeProps) {
  const dialogRef = useRef<HTMLDialogElement>(null);
  const [state, setState] = useState<LiveCoachState>("idle");
  const [transcript, setTranscript] = useState<string | null>(null);
  const [coachResponse, setCoachResponse] = useState<LiveCoachRespondResponse | null>(null);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const [voiceError, setVoiceError] = useState<string | null>(null);
  const [textInput, setTextInput] = useState("");
  const [showTextInput, setShowTextInput] = useState(false);
  const [voiceEnabled, setVoiceEnabled] = useState(() => {
    if (typeof window === "undefined") return true;
    try {
      const saved = localStorage.getItem("entrenate.live_coach.voice_enabled");
      return saved !== null ? saved === "true" : true;
    } catch {
      return true;
    }
  });

  const streamRef = useRef<MediaStream | null>(null);
  const recorderRef = useRef<MediaRecorder | null>(null);
  const chunksRef = useRef<Blob[]>([]);
  const timerRef = useRef<NodeJS.Timeout | null>(null);
  const startTimeRef = useRef<number>(0);

  const audioRef = useRef<HTMLAudioElement | null>(null);
  const currentAudioUrlRef = useRef<string | null>(null);

  function stopAudio(reason?: string) {
    void reason;
    if (audioRef.current) {
      const audio = audioRef.current;
      audioRef.current = null;
      audio.onplay = null;
      audio.onplaying = null;
      audio.onpause = null;
      audio.onended = null;
      audio.onerror = null;
      audio.onabort = null;
      audio.onemptied = null;
      audio.pause();
      audio.src = "";
    }
    if (currentAudioUrlRef.current) {
      URL.revokeObjectURL(currentAudioUrlRef.current);
      currentAudioUrlRef.current = null;
    }
  }

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
      stopAudio("component-unmount");
    };
  }, []);

  function toggleVoice() {
    const next = !voiceEnabled;
    setVoiceEnabled(next);
    try {
      localStorage.setItem("entrenate.live_coach.voice_enabled", String(next));
    } catch {
      // Ignore
    }

    if (!next && audioRef.current) {
      stopAudio("voice-disabled");
      setState("success");
    }
  }

  function getClientSnapshot(): LiveCoachClientSnapshot {
    const currentExercise =
      session?.ejercicios.find((e) => e.series.length < e.seriesObjetivo) ??
      session?.ejercicios[session.ejercicios.length - 1];

    const currentSetIndex = currentExercise
      ? Math.min(currentExercise.series.length + 1, currentExercise.seriesObjetivo)
      : null;

    return {
      exerciseId: currentExercise?.ejercicioId ?? null,
      restCapturedAtUtc: new Date().toISOString(),
      restIsActive: false,
      restRemainingSeconds: null,
      setIndex: currentSetIndex,
    };
  }

  function attachAudioEventListeners(audio: HTMLAudioElement) {
    audio.onplay = () => {
      setState("speaking");
    };
    audio.onplaying = () => {
      setState("speaking");
    };
    audio.onpause = () => {
      if (!audio.ended) setState("success");
    };
    audio.onended = () => {
      audio.currentTime = 0;
      setState("success");
    };
    audio.onerror = () => {
      stopAudio("audio-error");
      setState("success");
    };
  }

  async function playSpokenAnswer(responseId: string, isUserGesture = false) {
    if (audioRef.current && isUserGesture) {
      try {
        audioRef.current.currentTime = 0;
        await audioRef.current.play();
        return;
      } catch {
        stopAudio("play-rejected-reuse");
        setState("success");
        return;
      }
    }

    stopAudio("replace-audio");
    setVoiceError(null);

    try {
      const response = await fetch("/api/coach/live/speech", {
        body: JSON.stringify({ responseId }),
        headers: {
          "Content-Type": "application/json",
        },
        method: "POST",
      });

      if (!response.ok) {
        setVoiceError(
          response.status === 404
            ? "La respuesta de voz expiró. Hacé una nueva consulta."
            : "No se pudo generar la voz. Podés leer la respuesta en texto.",
        );
        setState("success");
        return;
      }

      const blob = await response.blob();
      if (blob.size === 0) {
        setVoiceError("El servicio de voz devolvió un audio vacío.");
        setState("success");
        return;
      }
      const url = URL.createObjectURL(blob);
      currentAudioUrlRef.current = url;

      const audio = new Audio(url);
      attachAudioEventListeners(audio);
      audioRef.current = audio;

      try {
        await audio.play();
      } catch {
        // Autoplay bloqueado por políticas del navegador (pérdida de user activation tras llamadas asíncronas).
        // Mantenemos audioRef y el URL cargados para que el usuario pueda reproducir al pulsar "Escuchar".
        setState("success");
      }
    } catch {
      setVoiceError("No se pudo conectar con el servicio de voz.");
      setState("success");
    }
  }

  async function askCoach(questionText: string, inputKind: "audio" | "text") {
    if (!session?.id) {
      setErrorMessage("No hay una sesión activa de entrenamiento.");
      setState("error");
      return;
    }

    stopAudio("new-query-ask");
    setState("answering");

    const snapshot = getClientSnapshot();

    const result = await respondLiveCoachAction({
      clientSnapshot: snapshot,
      clientTurnId: crypto.randomUUID(),
      inputKind,
      sessionId: session.id,
      text: questionText,
    });

    if (result.status === "success") {
      setCoachResponse(result.data);

      if (result.data.canSynthesizeVoice && voiceEnabled) {
        await playSpokenAnswer(result.data.responseId);
      } else {
        setState("success");
      }
    } else {
      setErrorMessage(result.message ?? "No se pudo obtener la respuesta del Coach.");
      setState("error");
    }
  }

  async function handleTextSubmit(event: FormEvent) {
    event.preventDefault();
    const query = textInput.trim();
    if (!query) return;

    setTranscript(query);
    setTextInput("");
    setShowTextInput(false);
    await askCoach(query, "text");
  }

  async function startRecording() {
    cleanupMedia();
    stopAudio("new-query-audio");
    setErrorMessage(null);
    setTranscript(null);
    setCoachResponse(null);

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
          autoGainControl: true,
          channelCount: 1,
          echoCancellation: true,
          noiseSuppression: true,
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
        const text = result.data.transcript;
        setTranscript(text);
        await askCoach(text, "audio");
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

  function handleReset() {
    cleanupMedia();
    stopAudio("manual-reset");
    setVoiceError(null);
    setState("idle");
    setTranscript(null);
    setCoachResponse(null);
    setErrorMessage(null);
    setTextInput("");
  }

  function handleCancelOrClose() {
    cleanupMedia();
    stopAudio("sheet-close");
    setVoiceError(null);
    setState("idle");
    setTranscript(null);
    setCoachResponse(null);
    setErrorMessage(null);
    setTextInput("");
    dialogRef.current?.close();
  }

  function openSheet() {
    const dialog = dialogRef.current;
    if (!dialog || dialog.open) return;
    dialog.showModal();
  }

  function handleBackdropClick(event: MouseEvent<HTMLDialogElement>) {
    if (event.target !== event.currentTarget) {
      return;
    }

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
        aria-controls="live-coach-dialog"
        aria-haspopup="dialog"
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
        aria-labelledby="live-coach-prototype-title"
        aria-modal="true"
        className="fixed inset-x-0 bottom-0 top-auto m-0 max-h-[calc(100dvh-0.5rem)] w-full max-w-none overflow-y-auto rounded-t-container border border-border-strong bg-surface-elevated p-0 text-text-primary shadow-elevated backdrop:bg-black/75 sm:inset-0 sm:m-auto sm:w-[calc(100%-2rem)] sm:max-w-md sm:rounded-container"
        onClick={handleBackdropClick}
        onClose={handleCancelOrClose}
        ref={dialogRef}
        id="live-coach-dialog"
      >
        <div className="mx-auto mt-2 h-1 w-10 rounded-full bg-border-strong sm:hidden" />
        <div className="px-5 pb-[calc(1.25rem+env(safe-area-inset-bottom))] pt-4 sm:p-6">
          <header className="flex items-start gap-3">
            <h2
              className="min-w-0 flex-1 font-brand text-2xl font-bold leading-8 text-text-primary"
              id="live-coach-prototype-title"
            >
              Live Coach
            </h2>
            <div className="flex items-center gap-1">
              <button
                aria-label={voiceEnabled ? "Desactivar respuestas por voz" : "Activar respuestas por voz"}
                aria-pressed={voiceEnabled}
                className={cn(
                  "flex size-11 shrink-0 items-center justify-center rounded-control-sm transition-colors duration-150 focus-visible:outline-2 focus-visible:outline-focus motion-reduce:transition-none",
                  voiceEnabled
                    ? "text-thermal-accent hover:bg-thermal-accent/15"
                    : "text-text-muted hover:bg-surface-interactive hover:text-text-secondary",
                )}
                onClick={toggleVoice}
                title={voiceEnabled ? "Voz activada" : "Voz desactivada"}
                type="button"
              >
                {voiceEnabled ? <Volume2 aria-hidden="true" size={20} /> : <VolumeX aria-hidden="true" size={20} />}
              </button>
              <button
                aria-label="Cerrar Live Coach"
                className="flex size-11 shrink-0 items-center justify-center rounded-control-sm text-text-secondary transition-colors duration-150 hover:bg-surface-interactive hover:text-text-primary focus-visible:outline-2 focus-visible:outline-focus motion-reduce:transition-none"
                onClick={handleCancelOrClose}
                type="button"
              >
                <X aria-hidden="true" size={20} />
              </button>
            </div>
          </header>

          <section
            aria-atomic="true"
            aria-busy={
              state === "requestingPermission" ||
              state === "transcribing" ||
              state === "answering"
            }
            aria-labelledby="live-coach-current-state"
            aria-live="polite"
            className="mt-4 flex flex-col items-center pb-1 pt-2 text-center"
          >
            <LiveCoachFace state={faceState} />
            <h3
              className="mt-1 font-brand text-xl font-bold leading-7 text-text-primary"
              id="live-coach-current-state"
            >
              {presentation.label}
            </h3>
            <p className="mt-1 text-sm leading-5 text-text-secondary">
              {presentation.description}
            </p>
          </section>

          <div className={cn(styles.stateBody, "mt-4 space-y-4")} key={state}>
            {state === "idle" && (
              <div className="space-y-2">
                <Button fullWidth onClick={startRecording}>
                  <Mic aria-hidden="true" size={18} />
                  <span>Hablar al Coach</span>
                </Button>

                {!showTextInput ? (
                  <Button
                    className="text-[0.8125rem]"
                    fullWidth
                    onClick={() => setShowTextInput(true)}
                    size="sm"
                    variant="ghost"
                  >
                    <MessageSquare aria-hidden="true" size={15} />
                    <span>Escribir</span>
                  </Button>
                ) : (
                  <form className="space-y-2 pt-1" onSubmit={handleTextSubmit}>
                    <div className="flex gap-2">
                      <input
                        aria-label="Pregunta al Live Coach"
                        className="min-h-11 flex-1 rounded-control-sm border border-border bg-surface-interactive px-3.5 text-sm text-text-primary placeholder:text-text-muted focus-visible:outline-2 focus-visible:outline-focus"
                        onChange={(e) => setTextInput(e.target.value)}
                        placeholder="Ej: ¿Qué ejercicio sigue?"
                        type="text"
                        value={textInput}
                      />
                      <button
                        aria-label="Enviar pregunta"
                        className="flex size-11 shrink-0 items-center justify-center rounded-control-sm bg-thermal-accent text-black transition-colors duration-150 hover:bg-thermal-accent/90 focus-visible:outline-2 focus-visible:outline-focus motion-reduce:transition-none disabled:opacity-50"
                        disabled={!textInput.trim()}
                        type="submit"
                      >
                        <ArrowRight aria-hidden="true" size={18} />
                      </button>
                    </div>
                  </form>
                )}
              </div>
            )}

            {state === "listening" && (
              <button
                className="flex min-h-12 w-full items-center justify-center gap-2.5 rounded-control-sm border border-thermal-accent/60 bg-thermal-accent/15 px-4 py-3 text-sm font-semibold text-thermal-accent transition-colors duration-150 hover:bg-thermal-accent/25 focus-visible:outline-2 focus-visible:outline-focus motion-reduce:transition-none"
                onClick={stopRecording}
                type="button"
              >
                <Square aria-hidden="true" className="fill-current" size={16} />
                <span>Detener y enviar</span>
              </button>
            )}

            {state === "speaking" && coachResponse && (
              <div className="space-y-4">
                <div className="max-h-[min(10rem,28dvh)] overflow-y-auto border-t border-border/60 pt-4 activity-scrollbar">
                  {coachResponse.isStaleContext && (
                    <p className="mb-2 text-xs font-semibold text-warning">Contexto actualizado</p>
                  )}
                  <p className="text-sm font-medium leading-6 text-text-secondary">
                    {coachResponse.text}
                  </p>
                </div>

                <div>
                  <Button
                    fullWidth
                    onClick={() => {
                      stopAudio("manual-stop");
                      setState("success");
                    }}
                    variant="secondary"
                  >
                    <Square aria-hidden="true" className="fill-current" size={15} />
                    <span>Detener voz</span>
                  </Button>
                </div>
              </div>
            )}

            {state === "success" && coachResponse && (
              <div className="space-y-4">
                <div className="max-h-[min(15rem,36dvh)] space-y-4 overflow-y-auto border-t border-border/60 pt-4 activity-scrollbar">
                  {transcript && (
                    <div>
                      <span className="text-[0.6875rem] font-semibold uppercase tracking-wider text-text-muted">
                        Tu consulta
                      </span>
                      <p className="mt-1 text-sm leading-5 text-text-secondary">
                        &ldquo;{transcript}&rdquo;
                      </p>
                    </div>
                  )}

                  <div>
                    <div className="flex items-center justify-between gap-2">
                      <span className="text-xs font-semibold text-thermal-accent">
                        Respuesta del Coach
                      </span>
                      {coachResponse.isStaleContext && (
                        <span className="text-[0.6875rem] font-medium text-warning">
                          Contexto actualizado
                        </span>
                      )}
                    </div>
                    <p className="mt-1.5 text-sm font-medium leading-6 text-text-primary">
                      {coachResponse.text}
                    </p>
                    {voiceError && (
                      <p className="mt-2 text-[0.8125rem] leading-5 text-warning" role="status">
                        {voiceError}
                      </p>
                    )}
                  </div>
                </div>

                <div className="flex gap-2">
                  {coachResponse.canSynthesizeVoice && (
                    <Button
                      className="flex-1"
                      onClick={() => playSpokenAnswer(coachResponse.responseId, true)}
                      variant="secondary"
                    >
                      <Volume2 aria-hidden="true" size={16} />
                      <span>Escuchar</span>
                    </Button>
                  )}

                  <Button
                    className="flex-1"
                    onClick={handleReset}
                    variant={coachResponse.canSynthesizeVoice ? "outline" : "primary"}
                  >
                    <RotateCcw aria-hidden="true" size={16} />
                    <span>Otra consulta</span>
                  </Button>
                </div>
              </div>
            )}

            {state === "error" && (
              <div className="space-y-4">
                <p
                  className="flex items-start justify-center gap-2 text-center text-sm leading-5 text-text-secondary"
                  role="alert"
                >
                  <AlertCircle aria-hidden="true" className="mt-0.5 shrink-0 text-error" size={17} />
                  <span>{getVisibleErrorMessage(errorMessage)}</span>
                </p>

                <Button fullWidth onClick={handleReset} variant="secondary">
                  <RotateCcw aria-hidden="true" size={16} />
                  <span>Reintentar</span>
                </Button>
              </div>
            )}
          </div>

        </div>
      </dialog>
    </>
  );
}
