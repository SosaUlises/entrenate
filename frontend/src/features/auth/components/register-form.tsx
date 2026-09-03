"use client";

import { zodResolver } from "@hookform/resolvers/zod";
import { CheckCircle2, Circle } from "lucide-react";
import { useState } from "react";
import { useForm, useWatch } from "react-hook-form";
import { Alert } from "@/components/ui/alert";
import { Button } from "@/components/ui/button";
import { FormField } from "@/components/ui/form-field";
import { Input } from "@/components/ui/input";
import { PasswordInput } from "@/components/ui/password-input";
import { cn } from "@/lib/class-names";
import { registerAction } from "../actions/register.action";
import {
  registerSchema,
  type RegisterFormValues,
} from "../schemas/auth.schemas";
import { AuthPageHeader } from "./auth-page-header";
import { AuthSecondaryAction } from "./auth-secondary-action";

const emailFieldId = "register-email";
const passwordFieldId = "register-password";
const confirmPasswordFieldId = "register-confirm-password";
const passwordRequirementsId = "register-password-requirements";

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

type SubmitFeedback = {
  message: string;
  variant: "error" | "success";
};

export function RegisterForm() {
  const [submitFeedback, setSubmitFeedback] = useState<SubmitFeedback | null>(
    null,
  );
  const {
    control,
    formState: { errors, isSubmitting },
    handleSubmit,
    register,
    setError,
  } = useForm<RegisterFormValues>({
    defaultValues: {
      confirmPassword: "",
      email: "",
      password: "",
    },
    mode: "onTouched",
    resolver: zodResolver(registerSchema),
  });

  const password = useWatch({
    control,
    name: "password",
  });

  const onSubmit = handleSubmit(async (values) => {
    setSubmitFeedback(null);

    const result = await registerAction(values);

    if (result.ok) {
      setSubmitFeedback({
        message: "Cuenta creada correctamente.",
        variant: "success",
      });
      return;
    }

    if (result.fieldErrors) {
      for (const [field, message] of Object.entries(result.fieldErrors)) {
        setError(field as keyof RegisterFormValues, {
          message,
          type: "server",
        });
      }
    }

    setSubmitFeedback({
      message: result.message,
      variant: "error",
    });
  });

  const emailMessageId = errors.email ? `${emailFieldId}-message` : undefined;
  const passwordMessageId = errors.password
    ? `${passwordFieldId}-message`
    : undefined;
  const confirmPasswordMessageId = errors.confirmPassword
    ? `${confirmPasswordFieldId}-message`
    : undefined;
  const passwordDescribedBy = [passwordMessageId, passwordRequirementsId]
    .filter(Boolean)
    .join(" ");

  return (
    <div className="space-y-8 sm:space-y-9">
      <AuthPageHeader
        supportingText="Empezá a registrar tu progreso."
        title="Creá tu cuenta"
      />

      <form className="space-y-5" noValidate onSubmit={onSubmit}>
        {submitFeedback ? (
          <Alert variant={submitFeedback.variant}>{submitFeedback.message}</Alert>
        ) : null}

        <FormField
          error={errors.email?.message}
          id={emailFieldId}
          label="Correo electrónico"
        >
          <Input
            autoComplete="email"
            hasError={Boolean(errors.email)}
            id={emailFieldId}
            inputMode="email"
            placeholder="nombre@correo.com"
            type="email"
            {...register("email")}
            aria-describedby={emailMessageId}
            aria-invalid={Boolean(errors.email)}
          />
        </FormField>

        <div className="space-y-4">
          <div className="space-y-2">
            <FormField
              error={errors.password?.message}
              id={passwordFieldId}
              label="Contraseña"
            >
              <PasswordInput
                autoComplete="new-password"
                hasError={Boolean(errors.password)}
                id={passwordFieldId}
                placeholder="••••••••"
                {...register("password")}
                aria-describedby={passwordDescribedBy || undefined}
                aria-invalid={Boolean(errors.password)}
              />
            </FormField>

            <PasswordRequirementList password={password} />
          </div>

          <FormField
            error={errors.confirmPassword?.message}
            id={confirmPasswordFieldId}
            label="Confirmar contraseña"
          >
            <PasswordInput
              autoComplete="new-password"
              hasError={Boolean(errors.confirmPassword)}
              id={confirmPasswordFieldId}
              placeholder="••••••••"
              {...register("confirmPassword")}
              aria-describedby={confirmPasswordMessageId}
              aria-invalid={Boolean(errors.confirmPassword)}
            />
          </FormField>
        </div>

        <Button
          className="min-h-14"
          fullWidth
          isLoading={isSubmitting}
          type="submit"
          variant="gradient"
        >
          Crear cuenta
        </Button>
      </form>

      <AuthSecondaryAction
        action="Iniciar sesión"
        href="/login"
        prompt="¿Ya tenés cuenta?"
      />
    </div>
  );
}

function PasswordRequirementList({ password }: { password: string }) {
  return (
    <div className="space-y-1.5 pt-0.5" id={passwordRequirementsId}>
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
