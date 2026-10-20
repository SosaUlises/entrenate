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
        "min-h-14 w-full rounded-control border bg-surface-interactive px-4 text-sm text-text-primary caret-thermal-accent outline-none transition-[background-color,border-color,box-shadow] duration-200 placeholder:text-text-muted hover:border-border-strong hover:bg-surface-elevated disabled:cursor-not-allowed disabled:opacity-45 motion-reduce:transition-none",
        hasError
          ? "border-error/80 focus:border-error focus-visible:outline-error"
          : "border-border focus:border-focus focus:bg-surface-elevated focus:shadow-[inset_0_-1px_0_var(--focus)] focus-visible:outline-focus",
        className,
      )}
      ref={ref}
      {...props}
    />
  ),
);

Input.displayName = "Input";
