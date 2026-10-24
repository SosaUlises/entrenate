"use client";

import { X } from "lucide-react";
import { useRouter } from "next/navigation";
import {
  useId,
  useRef,
  useState,
  type FormEvent,
  type MouseEvent,
  type ReactNode,
} from "react";
import { Button } from "@/components/ui/button";
import { useTrainingProfileGate } from "@/features/training-profile/gate/training-profile-gate";
import { askCoachProgressQuestionAction } from "../actions/ask-coach-progress-question.action";

const MAX_QUESTION_LENGTH = 500;

const coachDecisionPresentationLabels = {
  IncreaseLoad: "Aumentar la carga",
  IncreaseRepetitions: "Aumentar las repeticiones",
  InsufficientData: "Datos insuficientes",
  Maintain: "Mantener el enfoque actual",
  ReduceLoad: "Reducir la carga",
  ReviewPerformance: "Revisar el rendimiento",
} as const;

const coachDecisionTokenPattern =
  /\b(ReviewPerformance|IncreaseLoad|IncreaseRepetitions|Maintain|ReduceLoad|InsufficientData)\b/g;

type ConversationExchange = {
  answer: string;
  id: number;
  question: string;
};

type MarkdownBlock =
  | { lines: string[]; type: "paragraph" }
  | { items: string[]; type: "ordered-list" | "unordered-list" };

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
                {exchanges.map((exchange) => (
                  <li className="border-b border-border/60 py-6" key={exchange.id}>
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
                        <dd className="mt-2.5 text-[0.9375rem] leading-7 text-text-primary">
                          <CoachMarkdown content={exchange.answer} />
                        </dd>
                      </div>
                    </dl>
                  </li>
                ))}
              </ol>
            ) : null}
          </div>

          <form
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
              aria-describedby={`${textareaId}-counter${isTooLong ? ` ${textareaId}-error` : ""}`}
              aria-invalid={isTooLong}
              className="mt-2.5 min-h-28 w-full resize-y rounded-control border border-border-strong bg-surface-interactive px-4 py-3.5 text-base leading-7 text-text-primary caret-thermal-accent outline-none transition-colors duration-150 placeholder:text-text-muted hover:border-border-strong focus:border-focus focus-visible:outline-none motion-reduce:transition-none"
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

            <div className="mt-4">
              {requestError && failedQuestion ? (
                <Button
                  fullWidth
                  isLoading={isSending}
                  onClick={() => void sendQuestion(failedQuestion)}
                  size="lg"
                >
                  Reintentar
                </Button>
              ) : (
                <Button
                  disabled={!canSubmit}
                  fullWidth
                  isLoading={isSending}
                  size="lg"
                  type="submit"
                >
                  Enviar
                </Button>
              )}
            </div>
          </form>
        </div>
      </dialog>
    </>
  );
}

function CoachMarkdown({ content }: { content: string }) {
  const blocks = parseMarkdownBlocks(formatCoachPresentationText(content));

  return (
    <div className="space-y-4">
      {blocks.map((block, blockIndex) => {
        if (block.type === "unordered-list") {
          return (
            <ul
              className="list-disc space-y-2 pl-5 marker:text-steel"
              key={`unordered-${blockIndex}`}
            >
              {block.items.map((item, itemIndex) => (
                <li key={`${itemIndex}-${item}`}>
                  {renderInlineMarkdown(item)}
                </li>
              ))}
            </ul>
          );
        }

        if (block.type === "ordered-list") {
          return (
            <ol
              className="list-decimal space-y-2 pl-5 marker:font-semibold marker:text-steel"
              key={`ordered-${blockIndex}`}
            >
              {block.items.map((item, itemIndex) => (
                <li key={`${itemIndex}-${item}`}>
                  {renderInlineMarkdown(item)}
                </li>
              ))}
            </ol>
          );
        }

        return (
          <p key={`paragraph-${blockIndex}`}>
            {block.lines.map((line, lineIndex) => (
              <span key={`${lineIndex}-${line}`}>
                {renderInlineMarkdown(line)}
                {lineIndex < block.lines.length - 1 ? <br /> : null}
              </span>
            ))}
          </p>
        );
      })}
    </div>
  );
}

function formatCoachPresentationText(content: string): string {
  return content.replace(coachDecisionTokenPattern, (token) =>
    token in coachDecisionPresentationLabels
      ? coachDecisionPresentationLabels[
          token as keyof typeof coachDecisionPresentationLabels
        ]
      : token,
  );
}

function parseMarkdownBlocks(content: string): MarkdownBlock[] {
  const blocks: MarkdownBlock[] = [];
  let paragraphLines: string[] = [];
  let listItems: string[] = [];
  let listType: "ordered-list" | "unordered-list" | null = null;

  function flushParagraph() {
    if (paragraphLines.length === 0) return;
    blocks.push({ lines: paragraphLines, type: "paragraph" });
    paragraphLines = [];
  }

  function flushList() {
    if (!listType || listItems.length === 0) return;
    blocks.push({ items: listItems, type: listType });
    listItems = [];
    listType = null;
  }

  for (const line of content.replace(/\r\n?/g, "\n").split("\n")) {
    if (line.trim().length === 0) {
      flushParagraph();
      flushList();
      continue;
    }

    const unorderedItem = /^\s*[-*]\s+(.+)$/.exec(line);
    const orderedItem = /^\s*\d+[.)]\s+(.+)$/.exec(line);
    const nextListType = unorderedItem
      ? "unordered-list"
      : orderedItem
        ? "ordered-list"
        : null;

    if (nextListType) {
      flushParagraph();
      if (listType && listType !== nextListType) flushList();
      listType = nextListType;
      listItems.push((unorderedItem?.[1] ?? orderedItem?.[1] ?? "").trim());
      continue;
    }

    flushList();
    paragraphLines.push(line);
  }

  flushParagraph();
  flushList();

  return blocks;
}

function renderInlineMarkdown(content: string): ReactNode[] {
  return content
    .split(/(\*\*[^*]+\*\*|__[^_]+__)/g)
    .filter(Boolean)
    .map((segment, index) => {
      const isBold =
        (segment.startsWith("**") && segment.endsWith("**")) ||
        (segment.startsWith("__") && segment.endsWith("__"));

      if (!isBold) return segment;

      return (
        <strong className="font-bold text-text-primary" key={`${index}-${segment}`}>
          {segment.slice(2, -2)}
        </strong>
      );
    });
}
