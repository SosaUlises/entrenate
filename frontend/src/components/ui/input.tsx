import type { InputHTMLAttributes } from "react";
import { forwardRef } from "react";
import { cn } from "@/lib/class-names";

type InputProps = InputHTMLAttributes<HTMLInputElement> & {
  hasError?: boolean;
};

export const Input = forwardRef<HTMLInputElement, InputProps>(
  ({ className, hasError = false, ...props }, ref) => (
    <input
      className={cn(
        "min-h-12 w-full rounded-control border bg-surface-elevated px-4 text-sm text-text-primary outline-none transition-colors duration-200 placeholder:text-text-secondary/70 disabled:cursor-not-allowed disabled:opacity-60",
        hasError
          ? "border-error focus:border-error"
          : "border-border focus:border-primary",
        className,
      )}
      ref={ref}
      {...props}
    />
  ),
);

Input.displayName = "Input";
