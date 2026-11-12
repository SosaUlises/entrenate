"use client";

import {
  AlertCircle,
  ArrowRight,
  Check,
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
import {
  cancelLiveCoachPendingActionRequest,
  confirmLiveCoachPendingAction,
} from "@/features/coach/actions/live-coach-pending-action.actions";
import { transcribeLiveAudioAction } from "@/features/coach/actions/transcribe-live-audio.action";
import type {
  LiveCoachClientSnapshot,
  LiveCoachClientAction,
  LiveCoachPendingAction,
  LiveCoachRespondResponse,
} from "@/features/coach/types/live-coach.types";
import { cn } from "@/lib/class-names";
import { getFirstMissingTarget } from "../training-position";
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

const pendingActionLabels: Record<
  LiveCoachPendingAction["type"],
  { cancel: string; confirm: string }
> = {
  ApplyTimeConstraintAdaptation: {
    cancel: "Mantener sesión",
    confirm: "Aplicar ajuste",
  },
  ReactivateExercise: {
    cancel: "Cancelar",
    confirm: "Reactivar ejercicio",
  },
  RegisterCurrentSet: {
    cancel: "Cancelar",
    confirm: "Registrar serie",
  },
  SkipCurrentExercise: {
    cancel: "Cancelar",
    confirm: "Saltar ejercicio",
  },
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

  if (
    message === "Tu sesión cambió desde que preparé el ajuste. Pedímelo de nuevo y lo recalculo." ||
    message === "Ese ajuste ya venció. Pedímelo de nuevo y lo recalculo." ||
    message === "Ese ajuste ya no está disponible. Pedímelo de nuevo y lo recalculo."
  ) {
    return message;
  }

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

function mapToFaceState(
  state: LiveCoachState,
  hasPendingAction: boolean = false,
): LiveCoachFaceState {
  if (hasPendingAction && (state === "idle" || state === "success")) {
    return "waitingConfirmation";
  }

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
  visibleExerciseSessionId?: string | null;
  onSessionUpdated?: (
    session: TrainingSession,
    clientAction?: LiveCoachClientAction | null,
  ) => boolean;
  onClientAction?: (
    action: LiveCoachClientAction,
    updatedSession?: TrainingSession | null,
  ) => boolean;
  restTimer?: {
    isActive: boolean;
    remainingSeconds: number | null;
  };
};

export function LiveCoachPrototype({ onClientAction, onSessionUpdated, restTimer, session, visibleExerciseSessionId }: LiveCoachPrototypeProps) {
  const dialogRef = useRef<HTMLDialogElement>(null);
  const [state, setState] = useState<LiveCoachState>("idle");
  const [transcript, setTranscript] = useState<string | null>(null);
  const [coachResponse, setCoachResponse] = useState<LiveCoachRespondResponse | null>(null);
  const [pendingAction, setPendingAction] = useState<LiveCoachPendingAction | null>(null);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const [voiceError, setVoiceError] = useState<string | null>(null);
  const [textInput, setTextInput] = useState("");
  const [showTextInput, setShowTextInput] = useState(false);
  const [actionPending, setActionPending] = useState(false);
  const [actionError, setActionError] = useState<string | null>(null);
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
  const interactionSnapshotRef = useRef<LiveCoachClientSnapshot | null>(null);
  const appliedClientActionsRef = useRef<Set<string>>(new Set());

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
    const recorder = recorderRef.current;
    recorderRef.current = null;
    if (recorder) {
      recorder.ondataavailable = null;
      recorder.onerror = null;
      recorder.onstop = null;
    }
    if (recorder && recorder.state !== "inactive") {
      try {
        recorder.stop();
      } catch {
        // ignore if already stopped
      }
    }
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
    const authoritativeTarget = session ? getFirstMissingTarget(session) : null;
    const currentExercise = authoritativeTarget?.exercise ?? null;
    const currentSetIndex = authoritativeTarget?.setNumber ?? null;

    const capturedAtUtc = new Date().toISOString();

    return {
      exerciseId: currentExercise?.ejercicioId ?? null,
      restTimer: {
        capturedAtUtc,
        isActive: restTimer?.isActive ?? false,
        remainingSeconds: restTimer?.remainingSeconds ?? 0,
      },
      setIndex: currentSetIndex,
      visibleExerciseSessionId: visibleExerciseSessionId ?? currentExercise?.id ?? null,
    };
  }

  function attachAudioEventListeners(
    audio: HTMLAudioElement,
    listenForPendingConfirmation: boolean,
  ) {
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
      if (listenForPendingConfirmation && dialogRef.current?.open) {
        void startRecording(true);
      } else {
        setState("success");
      }
    };
    audio.onerror = () => {
      stopAudio("audio-error");
      setState("success");
    };
  }

  async function playSpokenAnswer(
    responseId: string,
    isUserGesture = false,
    listenForPendingConfirmation = false,
  ) {
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
      attachAudioEventListeners(audio, listenForPendingConfirmation);
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
    setActionError(null);
    setState("answering");

    const snapshot = inputKind === "audio"
      ? interactionSnapshotRef.current ?? getClientSnapshot()
      : getClientSnapshot();
    interactionSnapshotRef.current = null;

    const result = await respondLiveCoachAction({
      clientSnapshot: snapshot,
      clientTurnId: crypto.randomUUID(),
      inputKind,
      sessionId: session.id,
      text: questionText,
    });

    if (result.status === "success") {
      const nextPendingAction = result.data.pendingAction ?? null;
      const shouldApplyClientAction = Boolean(
        result.data.clientAction &&
        !appliedClientActionsRef.current.has(result.data.responseId),
      );
      let clientActionApplied = true;
      if (result.data.updatedSession) {
        clientActionApplied = onSessionUpdated?.(
          result.data.updatedSession,
          shouldApplyClientAction ? result.data.clientAction : null,
        ) ?? false;
      } else if (result.data.clientAction && shouldApplyClientAction) {
        clientActionApplied = onClientAction?.(result.data.clientAction) ?? false;
      }
      if (result.data.clientAction && shouldApplyClientAction) {
        if (!clientActionApplied) {
          setErrorMessage("No pude aplicar la acción en Training.");
          setState("error");
          return;
        }
        appliedClientActionsRef.current.add(result.data.responseId);
      }
      setCoachResponse({ ...result.data, pendingAction: null });
      setPendingAction(nextPendingAction);

      if (result.data.canSynthesizeVoice && voiceEnabled) {
        await playSpokenAnswer(
          result.data.responseId,
          false,
          inputKind === "audio" && Boolean(nextPendingAction),
        );
      } else {
        setState("success");
        if (inputKind === "audio" && nextPendingAction) {
          await startRecording(true);
        }
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

    if (!coachResponse?.clarification || pendingAction) {
      setTranscript(query);
    }
    setTextInput("");
    setShowTextInput(false);
    await askCoach(query, "text");
  }

  async function startRecording(preserveCurrentResponse = false) {
    if (actionPending) return;
    interactionSnapshotRef.current = getClientSnapshot();
    cleanupMedia();
    stopAudio("new-query-audio");
    setErrorMessage(null);
    if (!preserveCurrentResponse) {
      setTranscript(null);
      setCoachResponse(null);
    }
    setActionError(null);

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
        if (!coachResponse?.clarification || pendingAction) {
          setTranscript(text);
        }
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
    setPendingAction(null);
    setErrorMessage(null);
    setTextInput("");
    setActionPending(false);
    setActionError(null);
  }

  function handleCancelOrClose() {
    const pendingActionId = pendingAction?.actionId;
    if (pendingActionId) void cancelLiveCoachPendingActionRequest(pendingActionId);
    cleanupMedia();
    stopAudio("sheet-close");
    setVoiceError(null);
    setState("idle");
    setTranscript(null);
    setCoachResponse(null);
    setPendingAction(null);
    setErrorMessage(null);
    setTextInput("");
    setActionPending(false);
    setActionError(null);
    dialogRef.current?.close();
  }

  async function confirmPendingAction() {
    if (!pendingAction || actionPending) return;

    cleanupMedia();
    stopAudio("confirm-pending-action");
    setActionPending(true);
    setActionError(null);

    const result = await confirmLiveCoachPendingAction(
      pendingAction.actionId,
      restTimer?.isActive === true,
    );
    setActionPending(false);

    if (result.status === "success") {
      onSessionUpdated?.(result.data.updatedSession, result.data.clientAction);
      setTranscript(null);
      setCoachResponse((current) => current ? {
        ...current,
        canSynthesizeVoice: false,
        pendingAction: null,
        text: result.data.text,
      } : current);
      setPendingAction(null);
      setState("success");
      return;
    }

    if (result.status === "conflict" || result.status === "expired" || result.status === "not-found") {
      setCoachResponse((current) => current ? { ...current, pendingAction: null } : current);
      setPendingAction(null);
      if (pendingAction.type === "ApplyTimeConstraintAdaptation") {
        setErrorMessage(result.status === "expired"
          ? "Ese ajuste ya venció. Pedímelo de nuevo y lo recalculo."
          : result.status === "not-found"
            ? "Ese ajuste ya no está disponible. Pedímelo de nuevo y lo recalculo."
            : "Tu sesión cambió desde que preparé el ajuste. Pedímelo de nuevo y lo recalculo.");
        setState("error");
        return;
      }
      setErrorMessage(pendingAction.type === "SkipCurrentExercise"
        ? "La sesión cambió. Volvé a pedirme que salte el ejercicio."
        : pendingAction.type === "ReactivateExercise"
          ? "La sesión cambió. Volvé a pedirme que reactive el ejercicio."
        : "La sesión cambió. Volvé a indicarme la serie.");
      setState("error");
      return;
    }

    if (pendingAction.type === "ApplyTimeConstraintAdaptation") {
      setActionError("No pudimos aplicar el ajuste. Intentá nuevamente.");
      setState("success");
      return;
    }

    setActionError(pendingAction.type === "SkipCurrentExercise"
      ? "No pudimos omitir el ejercicio. Intentá nuevamente."
      : pendingAction.type === "ReactivateExercise"
        ? "No pudimos reactivar el ejercicio. Intentá nuevamente."
      : "No pudimos registrar la serie. Intentá nuevamente.");
    setState("success");
  }

  function cancelPendingAction() {
    const actionId = pendingAction?.actionId;
    if (actionId) void cancelLiveCoachPendingActionRequest(actionId);
    handleReset();
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

  const clarification = pendingAction ? null : coachResponse?.clarification ?? null;
  const presentation = clarification && state === "success"
    ? { description: "Elegí una opción o respondé con tu voz.", label: "Te escucho" }
    : statePresentations[state];
  const faceState = mapToFaceState(state, pendingAction !== null);
  const confirmationControlsDisabled =
    actionPending ||
    state === "requestingPermission" ||
    state === "transcribing" ||
    state === "answering";
  const showVoiceConfirmationAction =
    state !== "requestingPermission" &&
    state !== "listening" &&
    state !== "transcribing" &&
    state !== "answering";
  const pendingLabels = pendingAction
    ? pendingActionLabels[pendingAction.type] ?? { cancel: "Cancelar", confirm: "Confirmar" }
    : null;
  const pendingActionPanel = pendingAction ? (
    <div className="border-t border-border/60 pt-4">
      <p className="font-brand text-lg font-bold leading-6 text-text-primary">
        {pendingAction.summary.split(" · ")[0]}
      </p>
      {pendingAction.summary.includes(" · ") ? (
        <p className="mt-1 text-sm font-medium text-text-secondary">
          {pendingAction.summary.split(" · ").slice(1).join(" · ")}
        </p>
      ) : null}
      {actionError ? (
        <p className="mt-3 text-sm leading-5 text-error" role="alert">{actionError}</p>
      ) : null}
      <div className="mt-4 grid grid-cols-2 gap-2">
        <Button disabled={confirmationControlsDisabled} onClick={cancelPendingAction} variant="ghost">
          {pendingLabels?.cancel}
        </Button>
        <Button disabled={confirmationControlsDisabled} isLoading={actionPending} onClick={() => void confirmPendingAction()}>
          <Check aria-hidden="true" size={16} />
          {pendingLabels?.confirm}
        </Button>
      </div>
      {showVoiceConfirmationAction ? <Button
        className="mt-2"
        disabled={actionPending}
        fullWidth
        onClick={() => void startRecording(true)}
        size="sm"
        variant="outline"
      >
        <Mic aria-hidden="true" size={15} />
        Responder por voz
      </Button> : null}
    </div>
  ) : null;

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
                <Button fullWidth onClick={() => void startRecording()}>
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
              <div className="space-y-4">
                <button
                  className="flex min-h-12 w-full items-center justify-center gap-2.5 rounded-control-sm border border-thermal-accent/60 bg-thermal-accent/15 px-4 py-3 text-sm font-semibold text-thermal-accent transition-colors duration-150 hover:bg-thermal-accent/25 focus-visible:outline-2 focus-visible:outline-focus motion-reduce:transition-none"
                  onClick={stopRecording}
                  type="button"
                >
                  <Square aria-hidden="true" className="fill-current" size={16} />
                  <span>Detener y enviar</span>
                </button>
                {pendingActionPanel}
              </div>
            )}

            {(state === "requestingPermission" || state === "transcribing" || state === "answering") && pendingActionPanel}

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

                {pendingActionPanel}

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

                  {pendingActionPanel}
                </div>

                {clarification ? (
                  <div className="space-y-3" aria-label="Opciones para responder al Coach">
                    <div className="grid grid-cols-3 gap-2">
                      {clarification.options.map((option) => (
                        <button
                          className="min-h-11 rounded-control-sm border border-border-strong bg-surface-interactive px-2.5 py-2 text-sm font-semibold text-text-primary transition-colors duration-150 hover:border-thermal-accent/60 hover:bg-surface-active focus-visible:outline-2 focus-visible:outline-focus motion-reduce:transition-none"
                          key={option.value}
                          onClick={() => void askCoach(option.value, "text")}
                          type="button"
                        >
                          {option.label}
                        </button>
                      ))}
                    </div>

                    {showTextInput ? (
                      <form className="pt-1" onSubmit={handleTextSubmit}>
                        <div className="flex gap-2">
                          <input
                            aria-label="Respuesta para el Live Coach"
                            autoFocus
                            className="min-h-11 flex-1 rounded-control-sm border border-border bg-surface-interactive px-3.5 text-sm text-text-primary placeholder:text-text-muted focus-visible:outline-2 focus-visible:outline-focus"
                            onChange={(event) => setTextInput(event.target.value)}
                            placeholder="Escribí tu respuesta"
                            type="text"
                            value={textInput}
                          />
                          <button
                            aria-label="Enviar respuesta"
                            className="flex size-11 shrink-0 items-center justify-center rounded-control-sm bg-thermal-accent text-black transition-colors duration-150 hover:bg-thermal-accent/90 focus-visible:outline-2 focus-visible:outline-focus motion-reduce:transition-none disabled:opacity-50"
                            disabled={!textInput.trim()}
                            type="submit"
                          >
                            <ArrowRight aria-hidden="true" size={18} />
                          </button>
                        </div>
                      </form>
                    ) : null}

                    <div className="grid grid-cols-2 gap-2">
                      <Button onClick={() => void startRecording(true)}>
                        <Mic aria-hidden="true" size={16} />
                        <span>Responder</span>
                      </Button>
                      <Button onClick={handleReset} variant="outline">
                        <RotateCcw aria-hidden="true" size={16} />
                        <span>Otra consulta</span>
                      </Button>
                    </div>

                    <div className="flex flex-wrap justify-center gap-1">
                      {coachResponse.canSynthesizeVoice ? (
                        <Button
                          onClick={() => playSpokenAnswer(coachResponse.responseId, true)}
                          size="sm"
                          variant="ghost"
                        >
                          <Volume2 aria-hidden="true" size={15} />
                          <span>Escuchar</span>
                        </Button>
                      ) : null}
                      <Button
                        onClick={() => setShowTextInput((current) => !current)}
                        size="sm"
                        variant="ghost"
                      >
                        <MessageSquare aria-hidden="true" size={15} />
                        <span>{showTextInput ? "Ocultar texto" : "Escribir"}</span>
                      </Button>
                    </div>
                  </div>
                ) : !pendingAction ? (
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
                ) : null}
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

                {pendingAction ? pendingActionPanel : (
                  <Button fullWidth onClick={handleReset} variant="secondary">
                    <RotateCcw aria-hidden="true" size={16} />
                    <span>Reintentar</span>
                  </Button>
                )}
              </div>
            )}
          </div>

        </div>
      </dialog>
    </>
  );
}
