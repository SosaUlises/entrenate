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
  restIsActive?: boolean | null;
  restRemainingSeconds?: number | null;
  restCapturedAtUtc?: string | null;
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
};

export type LiveCoachPendingAction = {
  actionId: string;
  type: "RegisterCurrentSet";
  summary: string;
  expiresAtUtc: string;
};
