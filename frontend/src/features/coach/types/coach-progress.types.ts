export type CoachProgressDecision =
  | "InsufficientData"
  | "Maintain"
  | "IncreaseRepetitions"
  | "IncreaseLoad"
  | "ReduceLoad"
  | "ReviewPerformance";

export type CoachProgressEvidenceLevel = "Low" | "Medium" | "High";

export type CoachProgressPrescription = {
  plannedSets: number;
  minReps: number;
  maxReps: number;
  minRir: number;
  maxRir: number;
  restSeconds: number;
};

export type CoachProgressLatestExposure = {
  exerciseSessionId: string;
  sessionId: string;
  startedAt: string;
  isComplete: boolean;
  completedPlannedSetCount: number;
  missingPlannedSetCount: number;
  extraSetCount: number;
  loadClassification: string | null;
  referenceLoad: number | null;
  rirCoverage: number | null;
  isTopMastery: boolean;
  isOverloaded: boolean;
  prescription: CoachProgressPrescription;
};

export type CoachProgressResponse = {
  exerciseId: string;
  analysis: {
    validExposureCount: number;
    distinctSessionCount: number;
    comparableExposureCount: number;
    prescriptionChanged: boolean;
    loadChanged: boolean;
    latestExposure: CoachProgressLatestExposure | null;
  };
  recommendation: {
    decision: CoachProgressDecision;
    evidenceLevel: CoachProgressEvidenceLevel;
    reasonCodes: string[];
  };
  explanation: {
    detection: string;
    recommendation: string;
    rationale: string;
  };
};

export type AskCoachProgressQuestionRequest = {
  question: string;
};

export type CoachProgressConversationResponse = {
  answer: string;
};
