export type RoutineProposalFeasibility =
  | "Feasible"
  | "FeasibleWithWarnings"
  | "NotFeasible";

export type RoutinePlannerScheduledDay =
  | "Lunes"
  | "Martes"
  | "Miercoles"
  | "Jueves"
  | "Viernes"
  | "Sabado"
  | "Domingo";

export type RoutinePlannerDayOrigin = "Preferred" | "Derived";

export type RoutinePlannerSessionFocus =
  | "FullBody"
  | "Upper"
  | "Lower"
  | "Push"
  | "Pull"
  | "Legs"
  | "CoreCalves";

export type RoutinePlannerSessionVariant = "None" | "A" | "B" | "C";

export type RoutinePlannerProposalExercise = {
  code: string;
  exerciseId: string;
  exerciseType: string;
  maxReps: number;
  maxRir: number;
  minReps: number;
  minRir: number;
  movementPattern: string;
  name: string;
  order: number;
  restSeconds: number;
  role: string;
  sets: number;
};

export type RoutinePlannerProposalDay = {
  dayOrigin: RoutinePlannerDayOrigin;
  estimatedDurationSeconds: number;
  exercises: RoutinePlannerProposalExercise[];
  focus: RoutinePlannerSessionFocus;
  order: number;
  scheduledDay: RoutinePlannerScheduledDay;
  targetDurationMinutes: number;
  variant: RoutinePlannerSessionVariant;
};

export type RoutinePlannerProposalResponse = {
  days: RoutinePlannerProposalDay[];
  experienceLevel: string;
  feasibility: RoutineProposalFeasibility;
  goal: string;
  policyVersion: string;
  prescriptionDiagnostics: Array<{
    code: string;
    dayOrder: number | null;
    exerciseCode: string | null;
    movementPattern: string | null;
    muscleGroup: string | null;
  }>;
  proposalHash: string;
  proposalId: string;
  requestedDaysPerWeek: number;
  selectionDiagnostics: Array<{
    code: string;
    dayOrder: number;
    slotPosition: number;
  }>;
  weeklyVolumes: Array<{
    effectiveSets: number;
    muscleGroup: string;
  }>;
};
