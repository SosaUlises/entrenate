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
  visibleExerciseSessionId?: string | null;
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
  clientAction?: LiveCoachClientAction | null;
  clarification?: LiveCoachClarification | null;
};

export type LiveCoachClarification = {
  kind: "BestSetCriterion";
  options: LiveCoachClarificationOption[];
};

export type LiveCoachClarificationOption = {
  value: "peso" | "repeticiones" | "volumen";
  label: string;
};

export type LiveCoachClientAction =
  | { type: "StartRestTimer"; durationSeconds: number }
  | { type: "StopRestTimer"; durationSeconds?: null }
  | {
    type: "NavigateNextExercise" | "NavigatePreviousExercise" | "NavigateToExercise";
    exerciseSessionId: string;
    durationSeconds?: null;
  };

export type LiveCoachPendingAction = {
  actionId: string;
  type:
    | "RegisterCurrentSet"
    | "SkipCurrentExercise"
    | "ReactivateExercise"
    | "ApplyTimeConstraintAdaptation"
    | "ApplyFatigueAdaptation"
    | "ApplyPerformanceAdaptation"
    | "ApplyCombinedAdaptation";
  summary: string;
  expiresAtUtc: string;
};

export type LiveCoachActionConfirmation = {
  updatedSession: import("@/features/training/types/training.types").TrainingSession;
  text: string;
  clientAction?: LiveCoachClientAction | null;
};
