"use client";

import Image from "next/image";
import { useEffect, useMemo, useState } from "react";
import {
  loadEntrenateExerciseDemo,
  type EntrenateExerciseDemoDefinition,
} from "./exercise-demo-registry";

type DemoView = "movement" | "arms";

const movementSequence = [0, 1, 2, 1] as const;
const armSequence = [0, 1] as const;

export function EntrenateExerciseDemo({ exerciseName }: { exerciseName: string }) {
  const [definition, setDefinition] = useState<EntrenateExerciseDemoDefinition | null>(null);
  const [view, setView] = useState<DemoView>("movement");
  const [sequenceIndex, setSequenceIndex] = useState(0);
  const [loadedFrames, setLoadedFrames] = useState<Set<string>>(() => new Set());
  const [failed, setFailed] = useState(false);
  const [reducedMotion, setReducedMotion] = useState(true);

  useEffect(() => {
    let active = true;
    void loadEntrenateExerciseDemo(exerciseName).then(
      (demo) => { if (active) setDefinition(demo); },
      () => { if (active) setFailed(true); },
    );
    return () => { active = false; };
  }, [exerciseName]);

  useEffect(() => {
    const preference = window.matchMedia("(prefers-reduced-motion: reduce)");
    const updatePreference = () => setReducedMotion(preference.matches);
    updatePreference();
    preference.addEventListener("change", updatePreference);
    return () => preference.removeEventListener("change", updatePreference);
  }, []);

  const currentFrames = useMemo(
    () => definition ? (view === "movement" ? definition.movementFrames : definition.armPositionFrames) : [],
    [definition, view],
  );
  const framesReady = currentFrames.length > 0 && currentFrames.every((frame) => loadedFrames.has(frame));

  useEffect(() => {
    if (!framesReady || reducedMotion || failed) return;
    const sequence = view === "movement" ? movementSequence : armSequence;
    const interval = window.setInterval(() => {
      setSequenceIndex((index) => (index + 1) % sequence.length);
    }, view === "movement" ? 650 : 1100);
    return () => window.clearInterval(interval);
  }, [failed, framesReady, reducedMotion, view]);

  const activeFrame = reducedMotion
    ? view === "movement" ? 1 : 0
    : (view === "movement" ? movementSequence : armSequence)[sequenceIndex];

  return (
    <section aria-label="Demostración Entrenate" className="mt-3">
      <div aria-label="Vista de la demostración" className="grid grid-cols-2 rounded-full bg-surface/70 p-1" role="group">
        <button
          aria-pressed={view === "movement"}
          className={`min-h-10 rounded-full px-3 text-xs font-semibold transition-colors focus-visible:outline-primary ${view === "movement" ? "bg-primary/15 text-primary" : "text-text-secondary hover:text-text-primary"}`}
          onClick={() => {
            setView("movement");
            setSequenceIndex(0);
          }}
          type="button"
        >
          Movimiento
        </button>
        <button
          aria-pressed={view === "arms"}
          className={`min-h-10 rounded-full px-3 text-xs font-semibold transition-colors focus-visible:outline-primary ${view === "arms" ? "bg-primary/15 text-primary" : "text-text-secondary hover:text-text-primary"}`}
          onClick={() => {
            setView("arms");
            setSequenceIndex(0);
          }}
          type="button"
        >
          Posición de brazos
        </button>
      </div>

      <div className="relative mx-auto mt-3 flex aspect-square w-full max-w-84 items-center justify-center overflow-hidden bg-surface/20 p-1">
        {failed ? (
          <p className="px-4 text-center text-sm text-text-secondary" role="status">No pudimos cargar la demostración.</p>
        ) : definition ? (
          reducedMotion && view === "arms" ? (
            <div className="grid size-full grid-cols-2 items-center gap-1">
              {definition.armPositionFrames.map((source) => (
                <Image
                  alt=""
                  className="h-auto w-full object-contain"
                  height={1254}
                  key={source}
                  onError={() => setFailed(true)}
                  onLoad={() => setLoadedFrames((loaded) => new Set(loaded).add(source))}
                  src={source}
                  unoptimized
                  width={1254}
                />
              ))}
            </div>
          ) : (
            <>
              {currentFrames.map((source, index) => (
                <Image
                  alt=""
                  aria-hidden="true"
                  className={`absolute inset-0 size-full object-contain transition-opacity duration-150 motion-reduce:transition-none ${activeFrame === index ? "opacity-100" : "opacity-0"}`}
                  height={1254}
                  key={source}
                  loading="eager"
                  onError={() => setFailed(true)}
                  onLoad={() => setLoadedFrames((loaded) => new Set(loaded).add(source))}
                  src={source}
                  unoptimized
                  width={1254}
                />
              ))}
              {!loadedFrames.has(currentFrames[0]) ? (
                <span className="relative z-10 text-xs text-text-secondary">Cargando demostración…</span>
              ) : null}
            </>
          )
        ) : (
          <span className="text-xs text-text-secondary">Cargando demostración…</span>
        )}
      </div>
    </section>
  );
}
