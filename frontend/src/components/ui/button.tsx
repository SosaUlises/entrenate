import type { ButtonHTMLAttributes, ReactNode } from "react";
import { cn } from "@/lib/class-names";
import { Spinner } from "./spinner";

type ButtonVariant = "primary" | "secondary" | "ghost" | "destructive" | "text";

type ButtonProps = ButtonHTMLAttributes<HTMLButtonElement> & {
  fullWidth?: boolean;
  isLoading?: boolean;
  variant?: ButtonVariant;
};

const variantClasses: Record<ButtonVariant, string> = {
  primary:
    "bg-primary text-primary-foreground hover:bg-[color-mix(in_srgb,var(--primary)_88%,var(--primary-pressed))] active:bg-primary-pressed focus-visible:outline-focus",
  secondary:
    "border border-border-strong bg-surface-elevated text-text-primary hover:bg-surface-active focus-visible:outline-focus",
  ghost:
    "bg-transparent text-text-secondary hover:bg-surface-interactive hover:text-text-primary focus-visible:outline-focus",
  destructive:
    "bg-error text-primary-foreground hover:bg-[color-mix(in_srgb,var(--error)_88%,white)] focus-visible:outline-error",
  text: "px-2 text-text-secondary hover:text-text-primary focus-visible:outline-focus",
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
        "inline-flex min-h-12 items-center justify-center gap-2 rounded-control px-5 text-sm font-semibold transition-[background-color,border-color,color,opacity,transform] duration-200 active:scale-[0.99] disabled:cursor-not-allowed disabled:opacity-45 disabled:active:scale-100 motion-reduce:transition-none motion-reduce:active:scale-100",
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
