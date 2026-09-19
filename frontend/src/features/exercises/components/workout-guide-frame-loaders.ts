import type { StaticImageData } from "next/image";

type FrameModule = { default: StaticImageData };
type FrameSources = readonly [StaticImageData, StaticImageData, StaticImageData];

async function loadFrames(
  first: Promise<FrameModule>,
  second: Promise<FrameModule>,
  third: Promise<FrameModule>,
): Promise<FrameSources> {
  const frames = await Promise.all([first, second, third]);
  return [frames[0].default, frames[1].default, frames[2].default];
}

// Workout Guide documents literal asset imports for bundlers. Each loader imports
// only the three published PNGs for the selected exercise.
const frameLoaders: Record<string, () => Promise<FrameSources>> = {
  "exercise-bench-press": () => loadFrames(
    import("@bryllim/workout-guide/assets/bench-press/frame-1.png"),
    import("@bryllim/workout-guide/assets/bench-press/frame-2.png"),
    import("@bryllim/workout-guide/assets/bench-press/frame-3.png"),
  ),
  "exercise-dumbbell-bench-press": () => loadFrames(
    import("@bryllim/workout-guide/assets/dumbbell-bench-press/frame-1.png"),
    import("@bryllim/workout-guide/assets/dumbbell-bench-press/frame-2.png"),
    import("@bryllim/workout-guide/assets/dumbbell-bench-press/frame-3.png"),
  ),
  "exercise-incline-dumbbell-press": () => loadFrames(
    import("@bryllim/workout-guide/assets/incline-dumbbell-press/frame-1.png"),
    import("@bryllim/workout-guide/assets/incline-dumbbell-press/frame-2.png"),
    import("@bryllim/workout-guide/assets/incline-dumbbell-press/frame-3.png"),
  ),
  "exercise-dumbbell-fly": () => loadFrames(
    import("@bryllim/workout-guide/assets/dumbbell-fly/frame-1.png"),
    import("@bryllim/workout-guide/assets/dumbbell-fly/frame-2.png"),
    import("@bryllim/workout-guide/assets/dumbbell-fly/frame-3.png"),
  ),
  "exercise-pec-deck": () => loadFrames(
    import("@bryllim/workout-guide/assets/pec-deck/frame-1.png"),
    import("@bryllim/workout-guide/assets/pec-deck/frame-2.png"),
    import("@bryllim/workout-guide/assets/pec-deck/frame-3.png"),
  ),
  "exercise-push-up": () => loadFrames(
    import("@bryllim/workout-guide/assets/push-up/frame-1.png"),
    import("@bryllim/workout-guide/assets/push-up/frame-2.png"),
    import("@bryllim/workout-guide/assets/push-up/frame-3.png"),
  ),
  "exercise-pull-up": () => loadFrames(
    import("@bryllim/workout-guide/assets/pull-up/frame-1.png"),
    import("@bryllim/workout-guide/assets/pull-up/frame-2.png"),
    import("@bryllim/workout-guide/assets/pull-up/frame-3.png"),
  ),
  "exercise-barbell-row": () => loadFrames(
    import("@bryllim/workout-guide/assets/barbell-row/frame-1.png"),
    import("@bryllim/workout-guide/assets/barbell-row/frame-2.png"),
    import("@bryllim/workout-guide/assets/barbell-row/frame-3.png"),
  ),
  "exercise-lat-pulldown": () => loadFrames(
    import("@bryllim/workout-guide/assets/lat-pulldown/frame-1.png"),
    import("@bryllim/workout-guide/assets/lat-pulldown/frame-2.png"),
    import("@bryllim/workout-guide/assets/lat-pulldown/frame-3.png"),
  ),
  "exercise-overhead-press": () => loadFrames(
    import("@bryllim/workout-guide/assets/overhead-press/frame-1.png"),
    import("@bryllim/workout-guide/assets/overhead-press/frame-2.png"),
    import("@bryllim/workout-guide/assets/overhead-press/frame-3.png"),
  ),
  "exercise-lateral-raise": () => loadFrames(
    import("@bryllim/workout-guide/assets/lateral-raise/frame-1.png"),
    import("@bryllim/workout-guide/assets/lateral-raise/frame-2.png"),
    import("@bryllim/workout-guide/assets/lateral-raise/frame-3.png"),
  ),
  "exercise-face-pull": () => loadFrames(
    import("@bryllim/workout-guide/assets/face-pull/frame-1.png"),
    import("@bryllim/workout-guide/assets/face-pull/frame-2.png"),
    import("@bryllim/workout-guide/assets/face-pull/frame-3.png"),
  ),
  "exercise-pike-push-up": () => loadFrames(
    import("@bryllim/workout-guide/assets/pike-push-up/frame-1.png"),
    import("@bryllim/workout-guide/assets/pike-push-up/frame-2.png"),
    import("@bryllim/workout-guide/assets/pike-push-up/frame-3.png"),
  ),
  "exercise-bicep-curl": () => loadFrames(
    import("@bryllim/workout-guide/assets/bicep-curl/frame-1.png"),
    import("@bryllim/workout-guide/assets/bicep-curl/frame-2.png"),
    import("@bryllim/workout-guide/assets/bicep-curl/frame-3.png"),
  ),
  "exercise-ez-bar-curl": () => loadFrames(
    import("@bryllim/workout-guide/assets/ez-bar-curl/frame-1.png"),
    import("@bryllim/workout-guide/assets/ez-bar-curl/frame-2.png"),
    import("@bryllim/workout-guide/assets/ez-bar-curl/frame-3.png"),
  ),
  "exercise-cable-curl": () => loadFrames(
    import("@bryllim/workout-guide/assets/cable-curl/frame-1.png"),
    import("@bryllim/workout-guide/assets/cable-curl/frame-2.png"),
    import("@bryllim/workout-guide/assets/cable-curl/frame-3.png"),
  ),
  "exercise-dip": () => loadFrames(
    import("@bryllim/workout-guide/assets/dip/frame-1.png"),
    import("@bryllim/workout-guide/assets/dip/frame-2.png"),
    import("@bryllim/workout-guide/assets/dip/frame-3.png"),
  ),
  "exercise-squat": () => loadFrames(
    import("@bryllim/workout-guide/assets/squat/frame-1.png"),
    import("@bryllim/workout-guide/assets/squat/frame-2.png"),
    import("@bryllim/workout-guide/assets/squat/frame-3.png"),
  ),
  "exercise-goblet-squat": () => loadFrames(
    import("@bryllim/workout-guide/assets/goblet-squat/frame-1.png"),
    import("@bryllim/workout-guide/assets/goblet-squat/frame-2.png"),
    import("@bryllim/workout-guide/assets/goblet-squat/frame-3.png"),
  ),
  "exercise-bodyweight-squat": () => loadFrames(
    import("@bryllim/workout-guide/assets/bodyweight-squat/frame-1.png"),
    import("@bryllim/workout-guide/assets/bodyweight-squat/frame-2.png"),
    import("@bryllim/workout-guide/assets/bodyweight-squat/frame-3.png"),
  ),
  "exercise-leg-press": () => loadFrames(
    import("@bryllim/workout-guide/assets/leg-press/frame-1.png"),
    import("@bryllim/workout-guide/assets/leg-press/frame-2.png"),
    import("@bryllim/workout-guide/assets/leg-press/frame-3.png"),
  ),
  "exercise-hack-squat": () => loadFrames(
    import("@bryllim/workout-guide/assets/hack-squat/frame-1.png"),
    import("@bryllim/workout-guide/assets/hack-squat/frame-2.png"),
    import("@bryllim/workout-guide/assets/hack-squat/frame-3.png"),
  ),
  "exercise-leg-extension": () => loadFrames(
    import("@bryllim/workout-guide/assets/leg-extension/frame-1.png"),
    import("@bryllim/workout-guide/assets/leg-extension/frame-2.png"),
    import("@bryllim/workout-guide/assets/leg-extension/frame-3.png"),
  ),
  "exercise-romanian-deadlift": () => loadFrames(
    import("@bryllim/workout-guide/assets/romanian-deadlift/frame-1.png"),
    import("@bryllim/workout-guide/assets/romanian-deadlift/frame-2.png"),
    import("@bryllim/workout-guide/assets/romanian-deadlift/frame-3.png"),
  ),
  "exercise-hip-thrust": () => loadFrames(
    import("@bryllim/workout-guide/assets/hip-thrust/frame-1.png"),
    import("@bryllim/workout-guide/assets/hip-thrust/frame-2.png"),
    import("@bryllim/workout-guide/assets/hip-thrust/frame-3.png"),
  ),
  "exercise-glute-bridge": () => loadFrames(
    import("@bryllim/workout-guide/assets/glute-bridge/frame-1.png"),
    import("@bryllim/workout-guide/assets/glute-bridge/frame-2.png"),
    import("@bryllim/workout-guide/assets/glute-bridge/frame-3.png"),
  ),
  "exercise-calf-raise": () => loadFrames(
    import("@bryllim/workout-guide/assets/calf-raise/frame-1.png"),
    import("@bryllim/workout-guide/assets/calf-raise/frame-2.png"),
    import("@bryllim/workout-guide/assets/calf-raise/frame-3.png"),
  ),
  "exercise-plank": () => loadFrames(
    import("@bryllim/workout-guide/assets/plank/frame-1.png"),
    import("@bryllim/workout-guide/assets/plank/frame-2.png"),
    import("@bryllim/workout-guide/assets/plank/frame-3.png"),
  ),
  "exercise-crunch": () => loadFrames(
    import("@bryllim/workout-guide/assets/crunch/frame-1.png"),
    import("@bryllim/workout-guide/assets/crunch/frame-2.png"),
    import("@bryllim/workout-guide/assets/crunch/frame-3.png"),
  ),
};

export function loadWorkoutGuideFrames(workoutGuideId: string): Promise<FrameSources> {
  return frameLoaders[workoutGuideId]?.() ?? Promise.reject(new Error("No bundled frames for exercise"));
}
