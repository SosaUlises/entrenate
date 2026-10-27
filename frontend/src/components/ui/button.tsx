import type { ButtonHTMLAttributes, ReactNode } from "react";
import { cn } from "@/lib/class-names";
import { Spinner } from "./spinner";

export type ButtonVariant = "primary" | "secondary" | "outline" | "ghost" | "destructive";
export type ButtonSize = "sm" | "md" | "lg";

type ButtonProps = ButtonHTMLAttributes<HTMLButtonElement> & {
  fullWidth?: boolean;
  isLoading?: boolean;
  size?: ButtonSize;
  variant?: ButtonVariant;
};

const variantClasses: Record<ButtonVariant, string> = {
  primary:
    "border border-[color-mix(in_srgb,var(--technical-white)_24%,transparent)] bg-[color-mix(in_srgb,var(--surface-active)_84%,var(--technical-white))] text-text-primary hover:bg-[color-mix(in_srgb,var(--surface-active)_78%,var(--technical-white))] active:bg-[color-mix(in_srgb,var(--surface-active)_92%,var(--technical-white))] focus-visible:outline-focus [&_svg]:text-thermal-accent",
  secondary:
    "border border-border-strong bg-surface-elevated text-text-primary hover:bg-surface-active focus-visible:outline-focus",
  outline:
    "border border-border-strong bg-transparent text-text-primary hover:bg-surface-interactive active:bg-surface-active focus-visible:outline-focus",
  ghost:
    "border border-transparent bg-transparent text-text-secondary hover:bg-surface-interactive hover:text-text-primary focus-visible:outline-focus",
  destructive:
    "border border-error/70 bg-error text-background hover:bg-[color-mix(in_srgb,var(--error)_88%,white)] active:bg-[color-mix(in_srgb,var(--error)_82%,black)] focus-visible:outline-error",
};

const sizeClasses: Record<ButtonSize, string> = {
  sm: "min-h-11 px-3 text-sm",
  md: "min-h-12 px-5 text-sm",
  lg: "min-h-[3.25rem] px-6 text-[0.9375rem]",
};

export function buttonClassName({
  className,
  fullWidth = false,
  size = "md",
  variant = "primary",
}: {
  className?: string;
  fullWidth?: boolean;
  size?: ButtonSize;
  variant?: ButtonVariant;
} = {}): string {
  return cn(
    "inline-flex items-center justify-center gap-2 rounded-control font-semibold transition-[background-color,border-color,color,transform] duration-200 active:scale-[0.99] focus-visible:outline-2 focus-visible:outline-offset-2 disabled:cursor-not-allowed disabled:border-border disabled:bg-surface-active disabled:text-text-muted disabled:opacity-100 disabled:active:scale-100 disabled:[&_svg]:text-text-muted motion-reduce:transition-none motion-reduce:active:scale-100 [&_svg]:size-4 [&_svg]:shrink-0",
    sizeClasses[size],
    variantClasses[variant],
    fullWidth && "w-full",
    className,
  );
}

export function Button({
  children,
  className,
  disabled,
  fullWidth = false,
  isLoading = false,
  size = "md",
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
      className={buttonClassName({ className, fullWidth, size, variant })}
      disabled={disabled || isLoading}
      type={type}
      {...props}
    >
      {content}
    </button>
  );
}
