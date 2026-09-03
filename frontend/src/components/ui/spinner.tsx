import { cn } from "@/lib/class-names";

type SpinnerProps = {
  className?: string;
  label?: string;
};

export function Spinner({ className, label = "Cargando" }: SpinnerProps) {
  return (
    <span
      aria-label={label}
      className={cn(
        "inline-block size-4 animate-spin rounded-full border-2 border-current border-r-transparent",
        className,
      )}
      role="status"
    />
  );
}
