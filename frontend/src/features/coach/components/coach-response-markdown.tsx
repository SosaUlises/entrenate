import type { ReactNode } from "react";

const coachPresentationLabels = {
  AcondicionamientoGeneral: "acondicionamiento general",
  Accessory: "accesorio",
  AbduccionHombro: "abducción de hombro",
  Aislamiento: "aislamiento",
  Avanzado: "avanzado",
  BisagraCadera: "bisagra de cadera",
  Compuesto: "compuesto",
  CoreCalves: "core y gemelos",
  CoreAntiExtension: "anti-extensión de core",
  CoreFlexion: "flexión de core",
  Derived: "sugerido",
  DominanteRodilla: "dominante de rodilla",
  EmpujeHorizontal: "empuje horizontal",
  EmpujeVertical: "empuje vertical",
  EvidenceLevel: "nivel de evidencia",
  ExerciseReusedAcrossVariants:
    "ejercicio reutilizado entre sesiones por falta de una alternativa compatible",
  ExerciseSetsReducedForDuration:
    "series reducidas para respetar el tiempo disponible",
  ExtensionCadera: "extensión de cadera",
  ExtensionCodo: "extensión de codo",
  ExtensionRodilla: "extensión de rodilla",
  Feasible: "factible",
  FeasibleWithWarnings: "factible con observaciones",
  FlexionCodo: "flexión de codo",
  FlexionPlantar: "flexión plantar",
  FlexionRodilla: "flexión de rodilla",
  FullBody: "cuerpo completo",
  GanarFuerza: "ganar fuerza",
  GanarMasaMuscular: "ganar masa muscular",
  GanarmasaMuscularYFuerza: "ganar masa muscular y fuerza",
  High: "alto",
  IncreaseLoad: "Aumentar la carga",
  IncreaseRepetitions: "Aumentar las repeticiones",
  InsufficientData: "Datos insuficientes",
  Intermedio: "intermedio",
  Legs: "piernas",
  Low: "bajo",
  Lower: "tren inferior",
  Maintain: "Mantener el enfoque actual",
  Medium: "medio",
  MissingOptionalExercise:
    "sin una opción compatible para un complemento opcional",
  MissingRequiredExercise:
    "sin una opción compatible para un movimiento necesario",
  None: "sin variante",
  NotFeasible: "no confirmable",
  OptionalExerciseRemovedForDuration:
    "complemento opcional retirado para respetar el tiempo disponible",
  Preferred: "preferido",
  Principiante: "principiante",
  PrimaryCompound: "ejercicio principal",
  PolicyVersion: "versión de planificación",
  ProposalHash: "identificador de la propuesta",
  ProposalId: "identificador de la propuesta",
  Pull: "tracción",
  Push: "empuje",
  ReduceLoad: "Reducir la carga",
  reasonCodes: "motivos",
  ReasonCode: "motivo",
  ReviewPerformance: "Revisar el rendimiento",
  RotacionExternaHombro: "rotación externa de hombro",
  SecondaryCompound: "ejercicio secundario",
  SelectionNotFeasible:
    "selección incompleta para cubrir los ejercicios necesarios",
  SessionDurationExceeded: "sesión por encima de la duración objetivo",
  SinExperiencia: "inicial",
  TraccionHorizontal: "tracción horizontal",
  TraccionVertical: "tracción vertical",
  Upper: "tren superior",
  WeeklyVolumeTargetUnreachable:
    "volumen semanal previsto no alcanzado con las restricciones actuales",
  diagnosticCodes: "observaciones",
  diagnostics: "observaciones",
  feasibility: "factibilidad",
  policyVersion: "versión de planificación",
  proposalHash: "identificador de la propuesta",
  proposalId: "identificador de la propuesta",
  warnings: "observaciones",
} as const;

const coachPresentationTokenPattern = new RegExp(
  `\\b(${Object.keys(coachPresentationLabels).join("|")})\\b`,
  "g",
);

type MarkdownBlock =
  | { lines: string[]; type: "paragraph" }
  | { items: string[]; type: "ordered-list" }
  | { items: string[]; type: "unordered-list" };

export function CoachResponseMarkdown({
  content,
  sensitiveValues = [],
}: {
  content: string;
  sensitiveValues?: string[];
}) {
  const blocks = parseMarkdownBlocks(
    formatCoachPresentationText(content, sensitiveValues),
  );

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

function formatCoachPresentationText(
  content: string,
  sensitiveValues: string[],
): string {
  const sanitizedContent = sensitiveValues.reduce(
    (current, value) =>
      value.length > 0 ? current.replaceAll(value, "la propuesta") : current,
    content,
  );

  return sanitizedContent.replace(coachPresentationTokenPattern, (token) =>
    token in coachPresentationLabels
      ? coachPresentationLabels[
          token as keyof typeof coachPresentationLabels
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
        <strong
          className="font-bold text-text-primary"
          key={`${index}-${segment}`}
        >
          {segment.slice(2, -2)}
        </strong>
      );
    });
}
