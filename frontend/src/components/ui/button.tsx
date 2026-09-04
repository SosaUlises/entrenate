import type { ButtonHTMLAttributes, ReactNode } from "react";
import { cn } from "@/lib/class-names";
import { Spinner } from "./spinner";

type ButtonVariant = "gradient" | "primary" | "secondary" | "text";

type ButtonProps = ButtonHTMLAttributes<HTMLButtonElement> & {
  fullWidth?: boolean;
  isLoading?: boolean;
  variant?: ButtonVariant;
};

const variantClasses: Record<ButtonVariant, string> = {
  gradient:
    "bg-linear-to-r from-primary to-secondary text-text-primary hover:brightness-110 active:scale-[0.99] active:brightness-95 focus-visible:outline-primary",
  primary:
    "bg-primary-strong text-text-primary hover:bg-primary focus-visible:outline-primary",
  secondary:
    "border border-border bg-surface-elevated text-text-primary hover:border-border-strong hover:bg-surface",
  text: "min-h-0 px-0 text-primary hover:text-text-primary",
};

export function Button({
  children,
  className,
  disabled,
  fullWidth = false,
  isLoading = false,
  type = "button",
  variant = "primary",
  ...props
}: ButtonProps) {
  const content: ReactNode = isLoading ? (
    <>
      <Spinner />
      <span>{children}</span>
    </>
  ) : (
    children
  );

  return (
    <button
      className={cn(
        "inline-flex min-h-12 items-center justify-center gap-2 rounded-control px-5 text-sm font-semibold transition-[background-color,border-color,color,filter,opacity,transform] duration-200 disabled:cursor-not-allowed disabled:opacity-60 disabled:active:scale-100",
        variantClasses[variant],
        fullWidth && "w-full",
        className,
      )}
      disabled={disabled || isLoading}
      type={type}
      {...props}
    >
      {content}
    </button>
  );
}
