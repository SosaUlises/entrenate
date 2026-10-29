export type SessionEffortPreference = "Normal" | "Lighter";

export type SessionAdaptationRequest = {
  availableEquipmentIds: string[] | null;
  availableMinutes: number | null;
  effortPreference: SessionEffortPreference;
  excludedExerciseIds: string[];
  routineDayId: string;
};

export type AskSessionAdaptationQuestionRequest = SessionAdaptationRequest & {
  proposalHash: string;
  question: string;
};

export type ConfirmSessionAdaptationProposalRequest = SessionAdaptationRequest & {
  proposalHash: string;
};

export type SessionAdaptationConfirmationResponse = {
  createdNow: boolean;
  proposalId: string;
  sessionId: string;
};

export type SessionAdaptationConversationResponse = {
  answer: string;
};

export type SessionAdaptationFeasibility =
  | "Feasible"
  | "FeasibleWithChanges"
  | "NotFeasible";

export type SessionAdaptationExercise = {
  exerciseId: string;
  name: string;
  order: number;
  sets: number;
  minReps: number;
  maxReps: number;
  minRir: number;
  maxRir: number;
  restSeconds: number;
};

export type SessionAdaptationChangeType =
  | "ExerciseSubstituted"
  | "OptionalAccessoryRemovedNoSubstitute"
  | "RequiredExerciseUnavailable"
  | "ExerciseSetsReducedForTime"
  | "OptionalAccessoryRemovedForTime"
  | "RirIncreased"
  | "OptionalAccessorySetsReducedForLighter";

export type SessionAdaptationChange = {
  adaptedExerciseCode: string | null;
  adaptedExerciseId: string | null;
  adaptedRirMax: number | null;
  adaptedRirMin: number | null;
  adaptedSets: number | null;
  changeType: SessionAdaptationChangeType;
  originalExerciseCode: string | null;
  originalExerciseId: string;
  originalRirMax: number | null;
  originalRirMin: number | null;
  originalSets: number | null;
  reason: string | null;
  sourceRoutineExerciseId: string;
};

export type SessionAdaptationDiagnostic = {
  code: string;
  message: string;
  severity: string;
};

export type SessionAdaptationProposalResponse = {
  adaptedDurationSeconds: number;
  adaptedExercises: SessionAdaptationExercise[];
  changes: SessionAdaptationChange[];
  diagnostics: SessionAdaptationDiagnostic[];
  feasibility: SessionAdaptationFeasibility;
  originalDurationSeconds: number;
  originalExercises: SessionAdaptationExercise[];
  policyVersion: string;
  proposalHash: string;
  proposalId: string;
};

