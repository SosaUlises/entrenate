import type { TrainingSession, TrainingSessionExercise, TrainingSet } from "./types/training.types";

export type TrainingTarget = {
  exercise: TrainingSessionExercise;
  exercisePosition: number;
  setNumber: number;
};

export function getOrderedExercises(session: TrainingSession): TrainingSessionExercise[] {
  return [...session.ejercicios].sort((first, second) => first.orden - second.orden);
}

export function getFirstMissingTarget(
  session: TrainingSession,
  justSaved?: { exerciseId: string; set: TrainingSet },
): TrainingTarget | null {
  for (const [index, exercise] of getOrderedExercises(session).entries()) {
    const completedNumbers = new Set(
      exercise.series.filter((set) => set.completada).map((set) => set.numeroSerie),
    );

    if (justSaved?.exerciseId === exercise.id) {
      if (justSaved.set.completada) completedNumbers.add(justSaved.set.numeroSerie);
      else completedNumbers.delete(justSaved.set.numeroSerie);
    }

    for (let setNumber = 1; setNumber <= exercise.seriesObjetivo; setNumber += 1) {
      if (!completedNumbers.has(setNumber)) {
        return { exercise, exercisePosition: index + 1, setNumber };
      }
    }
  }

  return null;
}

export function areTargetSetsComplete(session: TrainingSession): boolean {
  return getFirstMissingTarget(session) === null;
}

export function getCompletedTargetSetCount(exercise: TrainingSessionExercise): number {
  return new Set(
    exercise.series
      .filter(
        (set) =>
          set.completada &&
          set.numeroSerie >= 1 &&
          set.numeroSerie <= exercise.seriesObjetivo,
      )
      .map((set) => set.numeroSerie),
  ).size;
}

export function getSessionTargetProgress(session: TrainingSession): {
  completed: number;
  total: number;
} {
  return getOrderedExercises(session).reduce(
    (progress, exercise) => ({
      completed: progress.completed + getCompletedTargetSetCount(exercise),
      total: progress.total + exercise.seriesObjetivo,
    }),
    { completed: 0, total: 0 },
  );
}
