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

export type LiveCoachProactiveRequest = {
  sessionId: string;
  snapshot?: {
    restExerciseSessionId?: string | null;
    restStartedAtUtc?: string | null;
  } | null;
};

export type LiveCoachProactiveSuggestion = {
  trigger: "RepeatedUnderperformance" | "ExcessiveEffort" | "SessionTimeDrift" | "LongRest";
  text: string;
  actionLabel?: string | null;
  actionPrompt?: string | null;
};

export type LiveCoachProactiveEvaluation = {
  suggestion?: LiveCoachProactiveSuggestion | null;
};

export type PostSessionBestSet = {
  criterion: "Volume" | "Weight" | "Repetitions";
  exerciseName: string;
  setNumber: number;
  weightKg: number;
  repetitions: number;
  rir: number | null;
  volume: number;
};

export type PostSessionAdaptation = {
  type: string;
  label: string;
  detail: string | null;
};

export type PostSessionExerciseSummary = {
  exerciseSessionId: string;
  exerciseName: string;
  completed: boolean;
  omitted: boolean;
  completedSets: number;
  missedRepetitionSets: number;
  excessiveEffortSets: number;
};

export type PostSessionCoachSummary = {
  sessionId: string;
  totalExercises: number;
  completedExercises: number;
  omittedExercises: number;
  completedSets: number;
  underperformanceEvents: number;
  excessiveEffortEvents: number;
  adaptationsApplied: PostSessionAdaptation[];
  bestSets: PostSessionBestSet[];
  exercises: PostSessionExerciseSummary[];
  notableObservations: string[];
};

export type PostSessionCoachAnswer = { answer: string };

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
  | { type: "StartRestTimer"; durationSeconds: number; exerciseSessionId?: string | null }
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
    | "ApplyCombinedAdaptation"
    | "ApplyPrescriptionAdaptation";
  summary: string;
  expiresAtUtc: string;
  adaptationPreview?: LiveAdaptationPreview | null;
};

export type LiveAdaptationTarget = {
  repMin: number;
  repMax: number;
  rirMin: number;
  rirMax: number;
};

export type LiveAdaptationPreview = {
  title: string;
  explanation: string;
  pendingSets: number;
  minutesBefore: number | null;
  minutesAfter: number | null;
  exercises: {
    id: string;
    name: string;
    status: "retained" | "completed" | "newlyOmitted" | "alreadyOmitted";
    pendingSets: number;
    target: LiveAdaptationTarget;
    restSeconds: number;
  }[];
  prescriptionChange: {
    exerciseName: string;
    fromSet: number;
    before: LiveAdaptationTarget;
    after: LiveAdaptationTarget;
  } | null;
};

export type LiveCoachActionConfirmation = {
  updatedSession: import("@/features/training/types/training.types").TrainingSession;
  text: string;
  clientAction?: LiveCoachClientAction | null;
};
