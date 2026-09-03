"use client";

import type { ComponentPropsWithoutRef } from "react";
import { useId, useState } from "react";
import { Eye, EyeOff } from "lucide-react";
import { cn } from "@/lib/class-names";
import { Input } from "./input";

type PasswordInputProps = Omit<ComponentPropsWithoutRef<typeof Input>, "type">;

export function PasswordInput({
  className,
  disabled,
  id,
  ...props
}: PasswordInputProps) {
  const generatedId = useId();
  const inputId = id ?? generatedId;
  const [isVisible, setIsVisible] = useState(false);
  const Icon = isVisible ? EyeOff : Eye;

  return (
    <div className="relative">
      <Input
        className={cn("pr-12", className)}
        disabled={disabled}
        id={inputId}
        type={isVisible ? "text" : "password"}
        {...props}
      />
      <button
        aria-controls={inputId}
        aria-label={isVisible ? "Ocultar contrasena" : "Mostrar contrasena"}
        className="absolute right-2 top-1/2 grid size-9 -translate-y-1/2 place-items-center rounded-control-sm text-text-secondary transition-colors duration-200 hover:text-text-primary disabled:cursor-not-allowed disabled:opacity-60"
        disabled={disabled}
        onClick={() => setIsVisible((value) => !value)}
        type="button"
      >
        <Icon aria-hidden="true" size={18} strokeWidth={2} />
      </button>
    </div>
  );
}
