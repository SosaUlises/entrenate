export type LiveCoachTranscriptionResponse = {
  interactionId: string;
  clientTurnId: string;
  transcript: string;
  confidence?: number | null;
  detectedLocale?: string | null;
  isFinal: boolean;
  status: string;
};

