"use client";

import { X } from "lucide-react";
import { useRouter } from "next/navigation";
import {
  useEffect,
  useId,
  useRef,
  useState,
  type FormEvent,
  type MouseEvent,
} from "react";
import { Button } from "@/components/ui/button";
import { useTrainingProfileGate } from "@/features/training-profile/gate/training-profile-gate";
import { askCoachProgressQuestionAction } from "../actions/ask-coach-progress-question.action";
import { CoachResponseMarkdown } from "./coach-response-markdown";

const MAX_QUESTION_LENGTH = 500;

type ConversationExchange = {
  answer: string;
  id: number;
  question: string;
};

export function ProgressCoachConversationSheet({
  exerciseId,
  exerciseName,
}: {
  exerciseId: string;
  exerciseName: string;
}) {
  const router = useRouter();
  const { invalidateSession } = useTrainingProfileGate();
  const dialogRef = useRef<HTMLDialogElement>(null);
  const latestExchangeRef = useRef<HTMLLIElement>(null);
  const textareaRef = useRef<HTMLTextAreaElement>(null);
  const requestEpochRef = useRef(0);
  const exchangeIdRef = useRef(0);
  const isSendingRef = useRef(false);
  const textareaId = useId();
  const [exchanges, setExchanges] = useState<ConversationExchange[]>([]);
  const [question, setQuestion] = useState("");
  const [failedQuestion, setFailedQuestion] = useState<string | null>(null);
  const [requestError, setRequestError] = useState<string | null>(null);
  const [isSending, setIsSending] = useState(false);

  const trimmedQuestion = question.trim();
  const isTooLong = question.length > MAX_QUESTION_LENGTH;
  const canSubmit =
    trimmedQuestion.length > 0 && !isTooLong && !isSending;

  useEffect(() => {
    if (exchanges.length === 0) return;

    const frame = window.requestAnimationFrame(() => {
      latestExchangeRef.current?.scrollIntoView({ block: "start" });
    });

    return () => window.cancelAnimationFrame(frame);
  }, [exchanges.length]);

  function openSheet() {
    const dialog = dialogRef.current;
    if (!dialog || dialog.open) return;

    dialog.showModal();
    window.requestAnimationFrame(() => textareaRef.current?.focus());
  }

  function resetConversation() {
    requestEpochRef.current += 1;
    exchangeIdRef.current = 0;
    setExchanges([]);
    setQuestion("");
    setFailedQuestion(null);
    setRequestError(null);
    isSendingRef.current = false;
    setIsSending(false);
  }

  function handleBackdropClick(event: MouseEvent<HTMLDialogElement>) {
    const bounds = event.currentTarget.getBoundingClientRect();
    const outside =
      event.clientX < bounds.left ||
      event.clientX > bounds.right ||
      event.clientY < bounds.top ||
      event.clientY > bounds.bottom;

    if (outside) event.currentTarget.close();
  }

  async function sendQuestion(questionToSend: string) {
    if (isSendingRef.current) return;

    const normalizedQuestion = questionToSend.trim();
    if (
      normalizedQuestion.length === 0 ||
      questionToSend.length > MAX_QUESTION_LENGTH
    ) {
      return;
    }

    const requestEpoch = requestEpochRef.current;
    isSendingRef.current = true;
    setIsSending(true);
    setRequestError(null);
    setFailedQuestion(null);

    try {
      const result = await askCoachProgressQuestionAction(
        exerciseId,
        normalizedQuestion,
      );

      if (requestEpoch !== requestEpochRef.current) return;

      if (result.status === "unauthenticated") {
        invalidateSession();
        router.replace("/login");
        return;
      }

      if (result.status !== "ready") {
        setFailedQuestion(normalizedQuestion);
        setRequestError(
          result.status === "not-found"
            ? "No encontramos el ejercicio para responder esta pregunta."
            : "No pudimos obtener una respuesta. Pod\u00e9s reintentar la \u00faltima pregunta.",
        );
        return;
      }

      exchangeIdRef.current += 1;
      setExchanges((current) => [
        ...current,
        {
          answer: result.answer,
          id: exchangeIdRef.current,
          question: normalizedQuestion,
        },
      ]);
      setQuestion((current) =>
        current.trim() === normalizedQuestion ? "" : current,
      );
    } catch {
      if (requestEpoch !== requestEpochRef.current) return;

      setFailedQuestion(normalizedQuestion);
      setRequestError(
        "No pudimos obtener una respuesta. Pod\u00e9s reintentar la \u00faltima pregunta.",
      );
    } finally {
      if (requestEpoch === requestEpochRef.current) {
        isSendingRef.current = false;
        setIsSending(false);
      }
    }
  }

  function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!canSubmit) return;
    void sendQuestion(question);
  }

  return (
    <>
      <div className="mt-3 border-t border-border/70 pt-6">
        <Button fullWidth onClick={openSheet} size="lg">
          Preguntar al Coach
        </Button>
      </div>

      <dialog
        aria-labelledby="progress-coach-conversation-title"
        aria-modal="true"
        className="fixed inset-x-0 top-auto bottom-0 m-0 h-[82dvh] max-h-[calc(100dvh-0.5rem)] w-full max-w-none overflow-hidden rounded-t-container border border-border-strong bg-surface-elevated p-0 text-text-primary shadow-elevated backdrop:bg-black/75 sm:inset-0 sm:m-auto sm:h-[min(78dvh,46rem)] sm:max-h-[calc(100dvh-3rem)] sm:max-w-lg sm:rounded-container"
        onClick={handleBackdropClick}
        onClose={resetConversation}
        ref={dialogRef}
      >
        <div className="flex h-full max-h-[calc(100dvh-0.5rem)] flex-col sm:max-h-[calc(100dvh-3rem)]">
          <div className="mx-auto mt-2 h-1 w-10 shrink-0 rounded-full bg-border-strong sm:hidden" />

          <header className="flex shrink-0 items-start gap-3 border-b border-border/70 px-5 pb-5 pt-4 sm:px-6 sm:pt-5">
            <div className="min-w-0 flex-1">
              <h2
                className="font-brand text-2xl font-bold leading-8 text-text-primary"
                id="progress-coach-conversation-title"
              >
                Preguntar al Coach
              </h2>
              <p className="mt-1.5 truncate text-sm font-semibold leading-5 text-info">
                {exerciseName}
              </p>
            </div>
            <button
              aria-label={"Cerrar conversaci\u00f3n"}
              className="flex size-11 shrink-0 items-center justify-center rounded-control-sm text-text-secondary transition-colors duration-150 hover:bg-surface-interactive hover:text-text-primary focus-visible:outline-2 focus-visible:outline-focus motion-reduce:transition-none"
              onClick={() => dialogRef.current?.close()}
              type="button"
            >
              <X aria-hidden="true" size={20} />
            </button>
          </header>

          <div className="min-h-0 flex-1 overflow-y-auto overscroll-contain px-5 py-5 sm:px-6 sm:py-6">
            <p className="text-[0.8125rem] leading-5 text-text-muted">
              {"Esta conversaci\u00f3n es temporal y no se guarda."}
            </p>

            {exchanges.length > 0 ? (
              <ol aria-live="polite" className="mt-5 border-t border-border/70">
                {exchanges.map((exchange, exchangeIndex) => (
                  <li
                    className="border-b border-border/60 py-6"
                    key={exchange.id}
                    ref={
                      exchangeIndex === exchanges.length - 1
                        ? latestExchangeRef
                        : undefined
                    }
                  >
                    <dl>
                      <div>
                        <dt className="text-[0.8125rem] font-semibold leading-5 text-text-secondary">
                          Tu pregunta
                        </dt>
                        <dd className="mt-2 whitespace-pre-wrap text-base leading-7 text-text-primary">
                          {exchange.question}
                        </dd>
                      </div>
                      <div className="mt-6 border-y border-border/60 bg-surface-interactive/60 px-4 py-5">
                        <dt className="text-sm font-semibold leading-5 text-info">
                          Coach
                        </dt>
                        <dd className="mt-2.5 break-words text-[0.9375rem] leading-7 text-text-primary">
                          <CoachResponseMarkdown content={exchange.answer} />
                        </dd>
                      </div>
                    </dl>
                  </li>
                ))}
              </ol>
            ) : null}
          </div>

          <form
            aria-busy={isSending}
            className="shrink-0 border-t border-border/70 bg-surface-elevated px-5 pb-[calc(1rem+env(safe-area-inset-bottom))] pt-5 sm:px-6 sm:pb-5"
            noValidate
            onSubmit={handleSubmit}
          >
            <label
              className="text-[0.9375rem] font-semibold leading-5 text-text-primary"
              htmlFor={textareaId}
            >
              Tu pregunta
            </label>
            <textarea
              aria-describedby={`${textareaId}-counter${isTooLong ? ` ${textareaId}-error` : requestError ? ` ${textareaId}-request-error` : ""}`}
              aria-invalid={isTooLong}
              className="mt-2.5 min-h-28 w-full resize-y rounded-control border border-border-strong bg-surface-interactive px-4 py-3.5 text-base leading-7 text-text-primary caret-thermal-accent outline-none transition-colors duration-150 placeholder:text-text-muted hover:border-border-strong focus:border-focus focus-visible:outline-none disabled:cursor-wait disabled:bg-surface-active disabled:text-text-secondary motion-reduce:transition-none"
              disabled={isSending}
              id={textareaId}
              onChange={(event) => {
                setQuestion(event.target.value);
                setRequestError(null);
                setFailedQuestion(null);
              }}
              placeholder={"Escrib\u00ed una pregunta sobre tu progreso"}
              ref={textareaRef}
              rows={3}
              value={question}
            />
            <div className="mt-1.5 flex min-h-5 items-start justify-between gap-3">
              <div className="min-w-0 flex-1">
                {isTooLong ? (
                  <p
                    className="text-[0.8125rem] leading-5 text-error"
                    id={`${textareaId}-error`}
                    role="alert"
                  >
                    La pregunta no puede superar los 500 caracteres.
                  </p>
                ) : requestError ? (
                  <p
                    className="text-[0.8125rem] leading-5 text-error"
                    id={`${textareaId}-request-error`}
                    role="alert"
                  >
                    {requestError}
                  </p>
                ) : null}
              </div>
              <span
                className={`shrink-0 text-[0.8125rem] font-semibold tabular-nums ${isTooLong ? "text-error" : "text-text-muted"}`}
                id={`${textareaId}-counter`}
              >
                {question.length}/{MAX_QUESTION_LENGTH}
              </span>
            </div>

            {isSending ? (
              <p aria-live="polite" className="sr-only">
                Enviando pregunta al Coach.
              </p>
            ) : null}

            <div className="mt-4">
              {requestError && failedQuestion ? (
                <Button
                  fullWidth
                  isLoading={isSending}
                  onClick={() => void sendQuestion(failedQuestion)}
                  size="lg"
                >
                  {isSending ? "Reintentando" : "Reintentar"}
                </Button>
              ) : (
                <Button
                  disabled={!canSubmit}
                  fullWidth
                  isLoading={isSending}
                  size="lg"
                  type="submit"
                >
                  {isSending ? "Enviando" : "Enviar"}
                </Button>
              )}
            </div>
          </form>
        </div>
      </dialog>
    </>
  );
}
