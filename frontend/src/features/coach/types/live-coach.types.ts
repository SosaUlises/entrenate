export type LiveCoachTranscriptionResponse = {
  interactionId: string;
  clientTurnId: string;
  transcript: string;
  confidence?: number | null;
  detectedLocale?: string | null;
  isFinal: boolean;
  status: string;
};

export type LiveCoachClientSnapshot = {
  exerciseId?: string | null;
  setIndex?: number | null;
  restTimer?: {
    isActive: boolean;
    remainingSeconds: number;
    capturedAtUtc: string;
  } | null;
};

export type LiveCoachRespondRequest = {
  sessionId: string;
  clientTurnId: string;
  inputKind: "audio" | "text";
  text: string;
  clientSnapshot?: LiveCoachClientSnapshot | null;
};

export type LiveCoachRespondResponse = {
  responseId: string;
  clientTurnId: string;
  text: string;
  isStaleContext: boolean;
  canSynthesizeVoice: boolean;
  pendingAction?: LiveCoachPendingAction | null;
  updatedSession?: import("@/features/training/types/training.types").TrainingSession | null;
};

export type LiveCoachPendingAction = {
  actionId: string;
  type: "RegisterCurrentSet";
  summary: string;
  expiresAtUtc: string;
};
