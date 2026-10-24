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
  error: "border-error/45",
  info: "border-info/45",
  success: "border-success/45",
  warning: "border-warning/45",
};

const titleClasses: Record<AlertVariant, string> = {
  error: "text-error",
  info: "text-info",
  success: "text-success",
  warning: "text-warning",
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
        "rounded-control border bg-surface-interactive px-4 py-3 text-sm leading-6 text-text-primary",
        variantClasses[variant],
        className,
      )}
      role={variant === "error" ? "alert" : "status"}
    >
      {title ? <p className={cn("font-semibold", titleClasses[variant])}>{title}</p> : null}
      <div className={title ? "mt-1 text-text-secondary" : ""}>{children}</div>
    </div>
  );
}
