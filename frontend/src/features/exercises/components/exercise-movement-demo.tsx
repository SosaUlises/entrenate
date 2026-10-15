"use client";

import dynamic from "next/dynamic";
import { hasEntrenateExerciseDemo } from "./exercise-demo-registry";
import { getEntrenateExerciseDemoV2 } from "./exercise-demo-v2-registry";
import { EntrenateExerciseDemo } from "./entrenate-exercise-demo";

const WorkoutGuideExerciseDemo = dynamic(
  () => import("./workout-guide-exercise-demo"),
  { ssr: false },
);

type ExerciseMovementDemoProps = {
  exerciseName: string;
  preferV2?: boolean;
  workoutGuideId: string | null;
};

export default function ExerciseMovementDemo({
  exerciseName,
  preferV2 = false,
  workoutGuideId,
}: ExerciseMovementDemoProps) {
  const preferredDefinition = preferV2
    ? getEntrenateExerciseDemoV2(exerciseName)
    : null;

  if (preferredDefinition || hasEntrenateExerciseDemo(exerciseName)) {
    return (
      <EntrenateExerciseDemo
        exerciseName={exerciseName}
        preferredDefinition={preferredDefinition}
      />
    );
  }

  if (!workoutGuideId) return null;

  return <WorkoutGuideExerciseDemo workoutGuideId={workoutGuideId} />;
}
