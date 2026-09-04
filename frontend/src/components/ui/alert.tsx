import type { ReactNode } from "react";
import { cn } from "@/lib/class-names";

type AlertVariant = "error" | "info" | "success" | "warning";

type AlertProps = {
  children: ReactNode;
  className?: string;
  title?: string;
  variant?: AlertVariant;
};

const variantClasses: Record<AlertVariant, string> = {
  error: "border-error/40 text-error",
  info: "border-info/40 text-text-primary",
  success: "border-success/40 text-success",
  warning: "border-warning/40 text-warning",
};

export function Alert({
  children,
  className,
  title,
  variant = "info",
}: AlertProps) {
  return (
    <div
      className={cn(
        "rounded-control border bg-surface-elevated px-4 py-3 text-sm leading-6",
        variantClasses[variant],
        className,
      )}
      role={variant === "error" ? "alert" : "status"}
    >
      {title ? <p className="font-semibold">{title}</p> : null}
      <div className={title ? "mt-1 text-text-secondary" : ""}>{children}</div>
    </div>
  );
}
