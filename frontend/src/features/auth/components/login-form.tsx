"use client";

import Link from "next/link";
import { zodResolver } from "@hookform/resolvers/zod";
import { useState } from "react";
import { useForm } from "react-hook-form";
import { Alert } from "@/components/ui/alert";
import { Button } from "@/components/ui/button";
import { FormField } from "@/components/ui/form-field";
import { Input } from "@/components/ui/input";
import { PasswordInput } from "@/components/ui/password-input";
import { loginAction } from "../actions/login.action";
import {
  loginSchema,
  type LoginFormValues,
} from "../schemas/auth.schemas";
import { AuthPageHeader } from "./auth-page-header";

const emailFieldId = "login-email";
const passwordFieldId = "login-password";

type SubmitFeedback = {
  message: string;
  variant: "error" | "success";
};

export function LoginForm() {
  const [submitFeedback, setSubmitFeedback] = useState<SubmitFeedback | null>(
    null,
  );
  const {
    formState: { errors, isSubmitting },
    handleSubmit,
    register,
    setError,
  } = useForm<LoginFormValues>({
    defaultValues: {
      email: "",
      password: "",
    },
    mode: "onTouched",
    resolver: zodResolver(loginSchema),
  });

  const onSubmit = handleSubmit(async (values) => {
    setSubmitFeedback(null);

    const result = await loginAction(values);

    if (result.ok) {
      setSubmitFeedback({
        message: "Sesión iniciada correctamente.",
        variant: "success",
      });
      return;
    }

    if (result.fieldErrors) {
      for (const [field, message] of Object.entries(result.fieldErrors)) {
        setError(field as keyof LoginFormValues, {
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

  return (
    <div className="space-y-8 sm:space-y-9">
      <AuthPageHeader
        supportingText="Continuá tu progreso."
        title="Bienvenido de nuevo"
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

        <div className="space-y-3">
          <FormField
            error={errors.password?.message}
            id={passwordFieldId}
            label="Contraseña"
          >
            <PasswordInput
              autoComplete="current-password"
              hasError={Boolean(errors.password)}
              id={passwordFieldId}
              placeholder="••••••••"
              {...register("password")}
              aria-describedby={passwordMessageId}
              aria-invalid={Boolean(errors.password)}
            />
          </FormField>

          <div className="flex justify-end">
            <Link
              className="min-h-12 content-center text-sm font-semibold text-primary underline-offset-4 transition-colors duration-200 hover:text-text-primary hover:underline focus-visible:outline-primary"
              href="/forgot-password"
            >
              ¿Olvidaste tu contraseña?
            </Link>
          </div>
        </div>

        <Button
          className="min-h-14"
          fullWidth
          isLoading={isSubmitting}
          type="submit"
          variant="gradient"
        >
          Iniciar sesión
        </Button>
      </form>

      <p className="flex flex-col items-center gap-1 text-center text-sm leading-6 text-text-secondary">
        <span>¿No tenés cuenta?</span>
        <Link
          className="font-semibold text-primary underline-offset-4 transition-colors duration-200 hover:text-text-primary hover:underline focus-visible:outline-primary"
          href="/register"
        >
          Crear cuenta
        </Link>
      </p>
    </div>
  );
}
