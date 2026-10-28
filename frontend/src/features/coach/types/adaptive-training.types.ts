export type SessionEffortPreference = "Normal" | "Lighter";

export type SessionAdaptationRequest = {
  availableEquipmentIds: string[] | null;
  availableMinutes: number | null;
  effortPreference: SessionEffortPreference;
  excludedExerciseIds: string[];
  routineDayId: string;
};

