import { CheckCircle2, Circle } from "lucide-react";
import { cn } from "@/lib/class-names";

const passwordRequirements = [
  {
    isMet: (value: string) => value.length >= 8,
    label: "Mínimo 8 caracteres",
  },
  {
    isMet: (value: string) => /[A-Z]/.test(value),
    label: "Una letra mayúscula",
  },
  {
    isMet: (value: string) => /[a-z]/.test(value),
    label: "Una letra minúscula",
  },
  {
    isMet: (value: string) => /[0-9]/.test(value),
    label: "Un número",
  },
] as const;

type PasswordRequirementsProps = {
  id: string;
  password: string;
};

export function PasswordRequirements({
  id,
  password,
}: PasswordRequirementsProps) {
  return (
    <div className="space-y-1.5 pt-0.5" id={id}>
      <p className="text-xs font-medium leading-5 text-text-secondary">
        Requisitos de contraseña
      </p>
      <ul className="grid gap-1">
        {passwordRequirements.map((requirement) => {
          const isMet = requirement.isMet(password);
          const Icon = isMet ? CheckCircle2 : Circle;

          return (
            <li
              className={cn(
                "flex items-center gap-2 text-xs leading-5 transition-colors duration-200",
                isMet ? "text-success" : "text-text-secondary",
              )}
              key={requirement.label}
            >
              <Icon
                aria-hidden="true"
                className={cn(
                  "size-3.5 shrink-0 transition-colors duration-200",
                  isMet ? "text-success" : "text-text-secondary/70",
                )}
                strokeWidth={2}
              />
              <span>{requirement.label}</span>
            </li>
          );
        })}
      </ul>
    </div>
  );
}
