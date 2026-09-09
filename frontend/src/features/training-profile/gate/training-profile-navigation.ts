export type TrainingProfilePresence = "exists" | "missing";

// This is the only point that needs to change when the real app destination exists.
export const trainingProfileAppDestination: string | null = null;

export function getDestinationForTrainingProfile(
  presence: TrainingProfilePresence,
): string | null {
  return presence === "missing"
    ? "/onboarding"
    : trainingProfileAppDestination;
}
