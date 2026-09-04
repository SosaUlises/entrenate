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
        "min-h-14 w-full rounded-control border bg-surface-elevated/70 px-4 text-sm text-text-primary outline-none transition-[background-color,border-color,box-shadow] duration-200 placeholder:text-text-secondary/55 hover:border-border-strong hover:bg-surface-elevated/85 disabled:cursor-not-allowed disabled:opacity-60",
        hasError
          ? "border-error/80 focus:border-error focus-visible:outline-error"
          : "border-border/70 focus:border-primary focus:ring-2 focus:ring-primary/25 focus-visible:outline-primary",
        className,
      )}
      ref={ref}
      {...props}
    />
  ),
);

Input.displayName = "Input";
