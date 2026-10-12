import type { TrainingSession } from "../types/training.types";
import { getSessionTargetProgress } from "../training-position";

export function TrainingProgressRail({ session }: { session: TrainingSession }) {
  const { completed, total } = getSessionTargetProgress(session);
  const progress = total > 0 ? (completed / total) * 100 : 0;
  const label = `${completed} de ${total} series objetivo completadas`;

  return (
    <div
      aria-label={label}
      aria-valuemax={total}
      aria-valuemin={0}
      aria-valuenow={completed}
      className="h-0.5 w-full overflow-hidden rounded-full bg-surface-elevated"
      role="progressbar"
    >
      <div
        className="h-full rounded-full bg-primary transition-[width] duration-200 motion-reduce:transition-none"
        style={{ width: `${progress}%` }}
      />
    </div>
  );
}
