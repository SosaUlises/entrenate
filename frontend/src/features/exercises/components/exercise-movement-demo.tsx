"use client";

import dynamic from "next/dynamic";
import { hasEntrenateExerciseDemo } from "./exercise-demo-registry";
import { EntrenateExerciseDemo } from "./entrenate-exercise-demo";

const WorkoutGuideExerciseDemo = dynamic(
  () => import("./workout-guide-exercise-demo"),
  { ssr: false },
);

type ExerciseMovementDemoProps = {
  exerciseName: string;
  workoutGuideId: string | null;
};

export default function ExerciseMovementDemo({
  exerciseName,
  workoutGuideId,
}: ExerciseMovementDemoProps) {
  if (hasEntrenateExerciseDemo(exerciseName)) {
    return <EntrenateExerciseDemo exerciseName={exerciseName} />;
  }

  if (!workoutGuideId) return null;

  return <WorkoutGuideExerciseDemo workoutGuideId={workoutGuideId} />;
}
