import { cn } from "@/lib/class-names";
import styles from "./live-coach-face.module.css";

export type LiveCoachFaceState =
  | "idle"
  | "listening"
  | "thinking"
  | "speaking"
  | "success"
  | "error";

type LiveCoachFaceProps = {
  compact?: boolean;
  isAnimating?: boolean;
  state: LiveCoachFaceState;
};

function Eye({ side }: { side: "left" | "right" }) {
  const isLeft = side === "left";

  return (
    <g className={cn(styles.eyePosition, isLeft ? styles.eyeLeft : styles.eyeRight)}>
      <g className={styles.eyeMotion}>
        <ellipse
          className={styles.eyeShape}
          cx={isLeft ? 33 : 87}
          cy="35"
          fill="var(--technical-white)"
          rx="17"
          ry="17"
        />
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
        "flex shrink-0 items-center justify-center",
        compact ? "h-8 w-12" : "h-24 w-36",
      )}
    >
      <svg
        className={cn("size-full overflow-visible", isAnimating && styles.animated)}
        data-state={state}
        focusable="false"
        viewBox="0 0 120 72"
      >
        <g className={styles.gaze}>
          <Eye side="left" />
          <Eye side="right" />
        </g>
      </svg>
    </span>
  );
}
