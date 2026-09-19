"use client";

import { getExercise } from "@bryllim/workout-guide";
import Image, { type StaticImageData } from "next/image";
import { useEffect, useMemo, useState } from "react";
import { loadWorkoutGuideFrames } from "./workout-guide-frame-loaders";

const frameSequence = [0, 1, 2, 1] as const;

type ExerciseMovementDemoProps = {
  workoutGuideId: string;
};

export default function WorkoutGuideExerciseDemo({ workoutGuideId }: ExerciseMovementDemoProps) {
  const exercise = useMemo(() => getExercise(workoutGuideId), [workoutGuideId]);
  const [frameSources, setFrameSources] = useState<readonly [StaticImageData, StaticImageData, StaticImageData] | null>(null);
  const [loadedFrames, setLoadedFrames] = useState<Set<number>>(() => new Set());
  const [framesFailed, setFramesFailed] = useState(false);
  const [reducedMotion, setReducedMotion] = useState(true);
  const [sequenceIndex, setSequenceIndex] = useState(0);

  useEffect(() => {
    let active = true;
    void loadWorkoutGuideFrames(workoutGuideId).then(
      (sources) => { if (active) setFrameSources(sources); },
      () => { if (active) setFramesFailed(true); },
    );

    return () => {
      active = false;
    };
  }, [workoutGuideId]);

  useEffect(() => {
    const preference = window.matchMedia("(prefers-reduced-motion: reduce)");
    const updatePreference = () => setReducedMotion(preference.matches);
    updatePreference();
    preference.addEventListener("change", updatePreference);
    return () => preference.removeEventListener("change", updatePreference);
  }, []);

  useEffect(() => {
    if (loadedFrames.size !== 3 || reducedMotion || framesFailed) return;
    const interval = window.setInterval(() => {
      setSequenceIndex((index) => (index + 1) % frameSequence.length);
    }, 750);
    return () => window.clearInterval(interval);
  }, [loadedFrames, reducedMotion, framesFailed]);

  if (!exercise) return null;

  const framesReady = loadedFrames.size === 3;
  const frameIndex = !framesReady || reducedMotion ? 0 : frameSequence[sequenceIndex];
  const attribution = exercise.attribution;

  return (
    <section aria-label="Demostración del movimiento" className="mt-4">
      <div
        aria-label={frameSources && !framesFailed ? "Demostración visual del movimiento" : undefined}
        className="relative mx-auto flex aspect-square w-full max-w-84 items-center justify-center overflow-hidden rounded-control-sm bg-surface/35 p-2"
        role={frameSources && !framesFailed ? "img" : undefined}
      >
        <div className="relative flex size-full items-center justify-center">
          {framesFailed ? (
            <p className="px-4 text-center text-sm text-text-secondary" role="status">No pudimos cargar la demostración.</p>
          ) : frameSources ? (
            <>
              {frameSources.map((source, index) => (
                <Image
                  alt=""
                  aria-hidden="true"
                  className={`absolute inset-0 size-full object-contain transition-opacity duration-300 motion-reduce:transition-none ${frameIndex === index ? "opacity-100" : "opacity-0"}`}
                  height={512}
                  key={index}
                  loading="eager"
                  onError={() => setFramesFailed(true)}
                  onLoad={() => setLoadedFrames((loaded) => new Set(loaded).add(index))}
                  src={source}
                  unoptimized
                  width={512}
                />
              ))}
              {!loadedFrames.has(0) ? (
                <span className="relative z-10 text-xs text-text-secondary">Cargando demostración…</span>
              ) : null}
            </>
          ) : (
            <span className="text-xs text-text-secondary">Cargando demostración…</span>
          )}
        </div>
      </div>
      {frameSources && loadedFrames.has(0) && !framesFailed ? (
        <div aria-hidden="true" className="mt-3 flex items-center justify-center gap-2">
          {[0, 1, 2].map((index) => (
            <span
              className={`size-1.5 rounded-full transition-colors ${frameIndex === index ? "bg-primary/80" : "bg-text-secondary/35"}`}
              key={index}
            />
          ))}
        </div>
      ) : null}
      {frameSources && loadedFrames.has(0) && !framesFailed ? (
        <footer className="mt-3 text-center text-xs leading-5 text-text-secondary/70">
          <a className="hover:underline focus-visible:underline" href={attribution.creatorUrl} rel="noopener noreferrer" target="_blank">{attribution.creator}</a>
          {attribution.source ? (
            <> · base: <a className="hover:underline focus-visible:underline" href={attribution.source.url} rel="noopener noreferrer" target="_blank">{attribution.source.name}</a></>
          ) : null}
          {" · "}<a className="hover:underline focus-visible:underline" href="https://bryllim.github.io/workout-guide/" rel="noopener noreferrer" target="_blank">Workout Guide</a>
          {" · "}<a className="hover:underline focus-visible:underline" href={attribution.licenseUrl} rel="noopener noreferrer" target="_blank">{attribution.license}</a>
        </footer>
      ) : null}
    </section>
  );
}
