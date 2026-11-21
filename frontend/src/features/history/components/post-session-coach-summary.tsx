"use client";

import { useEffect, useState } from "react";
import { Button } from "@/components/ui/button";
import {
  answerPostSessionCoachQuestionAction,
  getPostSessionCoachSummaryAction,
} from "@/features/coach/actions/post-session-coach.actions";
import type { PostSessionCoachSummary } from "@/features/coach/types/live-coach.types";

const questions = [
  "¿Cómo me fue hoy?",
  "¿Qué salió bien?",
  "¿Dónde rendí peor?",
  "¿Qué adaptaciones hicimos?",
  "¿Qué debería mirar la próxima vez?",
];

type SummaryState =
  | { status: "loading" }
  | { status: "error" }
  | { status: "ready"; summary: PostSessionCoachSummary };

export function PostSessionCoachSummary({
  onUnauthenticated,
  sessionId,
}: {
  onUnauthenticated: () => void;
  sessionId: string;
}) {
  const [state, setState] = useState<SummaryState>({ status: "loading" });
  const [questionOpen, setQuestionOpen] = useState(false);
  const [answer, setAnswer] = useState<string | null>(null);
  const [answerPending, setAnswerPending] = useState(false);
  const [answerError, setAnswerError] = useState(false);
  const [requestKey, setRequestKey] = useState(0);

  useEffect(() => {
    let active = true;
    void getPostSessionCoachSummaryAction(sessionId).then((result) => {
      if (!active) return;
      if (result.status === "unauthenticated") onUnauthenticated();
      else if (result.status === "ready") setState({ status: "ready", summary: result.summary });
      else setState({ status: "error" });
    });
    return () => { active = false; };
  }, [onUnauthenticated, requestKey, sessionId]);

  async function ask(question: string) {
    if (answerPending) return;
    setAnswerPending(true);
    setAnswerError(false);
    setAnswer(null);
    const result = await answerPostSessionCoachQuestionAction(sessionId, question);
    if (result.status === "unauthenticated") onUnauthenticated();
    else if (result.status === "ready") setAnswer(result.answer);
    else setAnswerError(true);
    setAnswerPending(false);
  }

  if (state.status === "loading") {
    return <div aria-label="Cargando resumen del entrenamiento" className="mt-9 border-y border-border/60 py-6" role="status">
      <div className="h-5 w-48 animate-pulse rounded bg-surface-elevated" />
      <div className="mt-4 h-14 animate-pulse rounded-control-sm bg-surface/60" />
    </div>;
  }
  if (state.status === "error") {
    return <div className="mt-9 border-y border-border/60 py-6" role="alert">
      <p className="text-sm text-text-secondary">No pudimos cargar el resumen del entrenamiento.</p>
      <Button className="mt-3" onClick={() => { setState({ status: "loading" }); setRequestKey((key) => key + 1); }} size="sm" variant="secondary">Reintentar</Button>
    </div>;
  }

  const { summary } = state;
  return (
    <section aria-labelledby="post-session-coach-title" className="mt-9 border-y border-border/70 py-7">
      <div className="flex flex-col items-start justify-between gap-4 sm:flex-row sm:items-end">
        <h2 className="font-brand text-xl font-bold text-text-primary" id="post-session-coach-title">Resumen del entrenamiento</h2>
        <Button onClick={() => setQuestionOpen((open) => !open)} size="sm" variant="secondary">
          {questionOpen ? "Cerrar" : "Preguntarle al Coach"}
        </Button>
      </div>

      <dl className="mt-5 grid grid-cols-2 gap-x-5 gap-y-4 sm:grid-cols-4">
        <Metric label="Ejercicios" value={`${summary.completedExercises}/${summary.totalExercises}`} />
        <Metric label="Series" value={String(summary.completedSets)} />
        <Metric label="Omitidos" value={String(summary.omittedExercises)} />
        <Metric label="Ajustes" value={String(summary.adaptationsApplied.length)} />
      </dl>

      {summary.notableObservations.length > 0 ? (
        <ul className="mt-6 space-y-3 border-t border-border/45 pt-5">
          {summary.notableObservations.map((observation) => (
            <li className="text-sm leading-6 text-text-secondary" key={observation}>{observation}</li>
          ))}
        </ul>
      ) : null}

      {questionOpen ? (
        <div className="mt-6 border-t border-border/45 pt-5">
          <p className="text-sm font-semibold text-text-primary">¿Qué querés revisar?</p>
          <div className="mt-3 flex flex-wrap gap-2">
            {questions.map((question) => (
              <button className="min-h-11 rounded-control-sm border border-border px-3 text-left text-sm font-semibold text-text-secondary transition-colors hover:bg-surface-interactive hover:text-text-primary focus-visible:outline-focus disabled:opacity-50" disabled={answerPending} key={question} onClick={() => void ask(question)} type="button">
                {question}
              </button>
            ))}
          </div>
          {answerPending ? <p className="mt-4 text-sm text-text-muted" role="status">Revisando la sesión...</p> : null}
          {answer ? <p className="mt-4 border-l border-thermal-accent pl-4 text-[0.9375rem] leading-6 text-text-primary">{answer}</p> : null}
          {answerError ? <p className="mt-4 text-sm text-error" role="alert">No pudimos responder. Intentá nuevamente.</p> : null}
        </div>
      ) : null}
    </section>
  );
}

function Metric({ label, value }: { label: string; value: string }) {
  return <div><dt className="text-xs font-semibold text-steel">{label}</dt><dd className="mt-1 font-brand text-xl font-bold tabular-nums text-text-primary">{value}</dd></div>;
}
