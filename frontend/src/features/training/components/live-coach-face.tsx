import { cn } from "@/lib/class-names";
import styles from "./live-coach-face.module.css";

export type LiveCoachFaceState =
  | "idle"
  | "listening"
  | "thinking"
  | "speaking"
  | "success"
  | "error"
  | "waitingConfirmation";

type LiveCoachFaceProps = {
  compact?: boolean;
  isAnimating?: boolean;
  state: LiveCoachFaceState;
};

function Eye({ side }: { side: "left" | "right" }) {
  const isLeft = side === "left";

  return (
    <g className={cn(styles.eyeAnchor, isLeft ? styles.eyeLeft : styles.eyeRight)}>
      <g className={styles.eyePosture}>
        <g className={styles.eyeLid}>
          <ellipse
            className={styles.eyeShape}
            cx={isLeft ? 33 : 87}
            cy="36"
            rx="17"
            ry="17"
          />
        </g>
      </g>
    </g>
  );
}

export function LiveCoachFace({
  compact = false,
  isAnimating = true,
  state,
}: LiveCoachFaceProps) {
  return (
    <span
      aria-hidden="true"
      className={cn(
        "flex shrink-0 items-center justify-center select-none",
        compact ? "h-8 w-12" : "h-24 w-36",
      )}
    >
      <svg
        className={cn("size-full overflow-visible", isAnimating && styles.animated)}
        data-state={state}
        focusable="false"
        viewBox="0 0 120 72"
      >
        <g className={styles.faceMotion}>
          <g className={styles.gazeMotion}>
            <Eye side="left" />
            <Eye side="right" />
          </g>

          {/* Micro-elementos temporales: Círculos de pensamiento */}
          <g aria-hidden="true" className={styles.thoughtBubbles}>
            <circle className={cn(styles.thoughtDot, styles.dot1)} cx="95" cy="18" r="1.8" />
            <circle className={cn(styles.thoughtDot, styles.dot2)} cx="102" cy="12" r="2.4" />
            <circle className={cn(styles.thoughtDot, styles.dot3)} cx="109" cy="6" r="3.2" />
          </g>

          {/* Micro-elemento temporal: Foquito minimalista técnico de idea/resolución */}
          <g aria-hidden="true" className={styles.ideaBeacon}>
            <line className={styles.ideaRay} x1="60" x2="60" y1="4.5" y2="2.5" />
            <line className={styles.ideaRay} x1="55" x2="53.5" y1="6.5" y2="5" />
            <line className={styles.ideaRay} x1="65" x2="66.5" y1="6.5" y2="5" />
            <circle className={styles.ideaBulb} cx="60" cy="10" r="3.2" />
            <circle className={styles.ideaFilament} cx="60" cy="10" r="1.1" />
            <line className={styles.ideaBase1} x1="58.5" x2="61.5" y1="13.8" y2="13.8" />
            <line className={styles.ideaBase2} x1="59" x2="61" y1="15.2" y2="15.2" />
          </g>
        </g>
      </svg>
    </span>
  );
}
