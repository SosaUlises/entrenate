export type SessionEffortPreference = "Normal" | "Lighter";

export type SessionAdaptationRequest = {
  availableEquipmentIds: string[] | null;
  availableMinutes: number | null;
  effortPreference: SessionEffortPreference;
  excludedExerciseIds: string[];
  routineDayId: string;
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

export type SessionAdaptationChange = {
  changeType: string;
  reason: string | null;
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

