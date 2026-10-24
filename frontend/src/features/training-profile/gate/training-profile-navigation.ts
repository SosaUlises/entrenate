export type TrainingProfilePresence = "exists" | "missing";

export const trainingProfileAppDestination = "/home";

export function getDestinationForTrainingProfile(
  presence: TrainingProfilePresence,
): string {
  return presence === "missing"
    ? "/onboarding"
    : trainingProfileAppDestination;
}
