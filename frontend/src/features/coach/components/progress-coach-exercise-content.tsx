"use client";

import { ArrowLeft } from "lucide-react";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { useEffect, useState } from "react";
import { Button } from "@/components/ui/button";
import type { Exercise } from "@/features/exercises/types/exercise.types";
import { useTrainingProfileGate } from "@/features/training-profile/gate/training-profile-gate";
import {
  getCoachProgressAction,
  type GetCoachProgressActionResult,
} from "../actions/get-coach-progress.action";
import type {
  CoachProgressDecision,
  CoachProgressEvidenceLevel,
  CoachProgressResponse,
} from "../types/coach-progress.types";

type ProgressState =
  | { status: "loading" }
  | { status: "ready"; progress: CoachProgressResponse }
  | { status: "not-found" }
  | { status: "error" };

const decisionLabels: Record<CoachProgressDecision, string> = {
  InsufficientData: "Todav\u00eda faltan datos",
  Maintain: "Manten\u00e9 tu enfoque actual",
  IncreaseRepetitions: "Aument\u00e1 las repeticiones",
  IncreaseLoad: "Aument\u00e1 la carga",
  ReduceLoad: "Reduc\u00ed la carga",
  ReviewPerformance: "Revis\u00e1 tu rendimiento",
};

const evidenceLabels: Record<CoachProgressEvidenceLevel, string> = {
  Low: "Baja",
  Medium: "Media",
  High: "Alta",
};

export function ProgressCoachExerciseContent({
  exercise,
  exerciseId,
  loadFailed,
}: {
  exercise: Exercise | undefined;
  exerciseId: string;
  loadFailed: boolean;
}) {
  const router = useRouter();
  const { invalidateSession } = useTrainingProfileGate();
  const [requestKey, setRequestKey] = useState(0);
  const [state, setState] = useState<ProgressState>(
    exercise && !loadFailed ? { status: "loading" } : { status: "error" },
  );

  useEffect(() => {
    if (!exercise || loadFailed) return;

    let active = true;

    const handleResult = (result: GetCoachProgressActionResult) => {
      if (!active) return;

      if (result.status === "unauthenticated") {
        invalidateSession();
        router.replace("/login");
        return;
      }

      setState(result);
    };

    void getCoachProgressAction(exerciseId).then(handleResult, () => {
      if (active) setState({ status: "error" });
    });

    return () => {
      active = false;
    };
  }, [exercise, exerciseId, invalidateSession, loadFailed, requestKey, router]);

  return (
    <section
      aria-labelledby="progress-coach-exercise-title"
      className="mx-auto w-full min-w-0 max-w-xl"
    >
      <header className="max-w-lg">
        <div className="flex min-w-0 items-center gap-1">
          <Link
            aria-label="Elegir otro ejercicio"
            className="flex size-11 shrink-0 items-center justify-center rounded-control-sm text-text-secondary transition-colors duration-150 hover:bg-surface-interactive hover:text-text-primary focus-visible:outline-2 focus-visible:outline-focus motion-reduce:transition-none"
            href="/coach/progress"
          >
            <ArrowLeft aria-hidden="true" size={19} />
          </Link>
          <h1
            className="min-w-0 truncate font-brand text-2xl font-bold leading-tight text-text-primary"
            id="progress-coach-exercise-title"
            title={exercise?.nombre}
          >
            {exercise?.nombre ?? "Coach de progreso"}
          </h1>
        </div>
        {exercise ? (
          <p className="mt-1 pl-12 text-[0.8125rem] font-semibold leading-5 text-info">
            {exercise.grupoMuscularPrincipal}
          </p>
        ) : null}
      </header>

      {!exercise ? (
        <LoadError
          description={
            loadFailed
              ? "No pudimos cargar el ejercicio."
              : "No encontramos el ejercicio."
          }
          onRetry={loadFailed ? () => router.refresh() : undefined}
        />
      ) : state.status === "loading" ? (
        <ProgressAnalysisSkeleton />
      ) : state.status === "error" ? (
        <LoadError
          description={"No pudimos cargar el an\u00e1lisis de progreso."}
          onRetry={() => {
            setState({ status: "loading" });
            setRequestKey((key) => key + 1);
          }}
        />
      ) : state.status === "not-found" ? (
        <LoadError
          description={"No encontramos un an\u00e1lisis para este ejercicio."}
        />
      ) : (
        <ProgressAnalysis progress={state.progress} />
      )}
    </section>
  );
}

function ProgressAnalysis({ progress }: { progress: CoachProgressResponse }) {
  const decision = decisionLabels[progress.recommendation.decision];
  const evidence = evidenceLabels[progress.recommendation.evidenceLevel];

  return (
    <div className="mt-8">
      <section
        aria-labelledby="coach-decision-title"
        className="border-y border-border/70 py-7"
      >
        <div className="flex items-center gap-3">
          <span
            aria-hidden="true"
            className="h-0.5 w-5 shrink-0 rounded-full bg-thermal-accent"
          />
          <h2
            className="font-brand text-2xl font-bold leading-tight text-text-primary"
            id="coach-decision-title"
          >
            {decision}
          </h2>
        </div>

        <div className="mt-6 flex items-baseline justify-between gap-4 border-t border-border/60 pt-4">
          <span className="text-sm font-medium text-text-secondary">
            Nivel de evidencia
          </span>
          <span className="text-[0.8125rem] font-semibold text-info">
            {evidence}
          </span>
        </div>
      </section>

      <section
        aria-label={"Explicaci\u00f3n"}
        className="divide-y divide-border/60"
      >
        <ExplanationSection
          body={progress.explanation.detection}
          title={"Qu\u00e9 detectamos"}
        />
        <ExplanationSection
          body={progress.explanation.recommendation}
          title={"Recomendaci\u00f3n"}
        />
        <ExplanationSection
          body={progress.explanation.rationale}
          title={"Por qu\u00e9"}
        />
      </section>
    </div>
  );
}

function ExplanationSection({ body, title }: { body: string; title: string }) {
  return (
    <div className="py-6">
      <h2 className="font-brand text-lg font-bold leading-6 text-text-primary">
        {title}
      </h2>
      <p className="mt-2 max-w-lg text-sm leading-6 text-text-secondary">
        {body}
      </p>
    </div>
  );
}

function LoadError({
  description,
  onRetry,
}: {
  description: string;
  onRetry?: () => void;
}) {
  return (
    <div className="mt-8 border-t border-border/70 pt-6" role="alert">
      <p className="text-sm leading-6 text-text-primary">{description}</p>
      {onRetry ? (
        <Button className="mt-4" onClick={onRetry} variant="secondary">
          Reintentar
        </Button>
      ) : null}
    </div>
  );
}

function ProgressAnalysisSkeleton() {
  return (
    <div
      aria-label={"Cargando an\u00e1lisis de progreso"}
      className="mt-8 animate-pulse"
      role="status"
    >
      <div className="border-y border-border/50 py-7">
        <div className="h-7 w-4/5 rounded bg-surface-elevated" />
        <div className="mt-6 flex justify-between border-t border-border/50 pt-4">
          <div className="h-4 w-32 rounded bg-surface-elevated" />
          <div className="h-4 w-12 rounded bg-surface-elevated" />
        </div>
      </div>
      {[0, 1, 2].map((item) => (
        <div className="border-b border-border/50 py-6" key={item}>
          <div className="h-5 w-36 rounded bg-surface-elevated" />
          <div className="mt-3 h-4 w-full rounded bg-surface-elevated" />
          <div className="mt-2 h-4 w-4/5 rounded bg-surface-elevated" />
        </div>
      ))}
    </div>
  );
}
