"use client";

import dynamic from "next/dynamic";
import { useState } from "react";
import { getEntrenateExerciseDemoV2 } from "./exercise-demo-v2-registry";
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
  const [failedExerciseName, setFailedExerciseName] = useState<string | null>(null);
  const definition = getEntrenateExerciseDemoV2(exerciseName);
  const v2Failed = failedExerciseName === exerciseName;

  if (definition && !v2Failed) {
    return (
      <EntrenateExerciseDemo
        definition={definition}
        onFailure={() => setFailedExerciseName(exerciseName)}
      />
    );
  }

  if (!workoutGuideId) return null;

  return <WorkoutGuideExerciseDemo workoutGuideId={workoutGuideId} />;
}
