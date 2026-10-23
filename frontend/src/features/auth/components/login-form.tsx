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
import {
  TrainingProfileGateStatus,
  usePostAuthTrainingProfileGate,
  useTrainingProfileGate,
} from "@/features/training-profile/gate/training-profile-gate";
import { loginAction } from "../actions/login.action";
import {
  loginSchema,
  type LoginFormValues,
} from "../schemas/auth.schemas";

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
  const [profileCheckFailed, setProfileCheckFailed] = useState(false);
  const [isRetryingProfile, setIsRetryingProfile] = useState(false);
  const { complete, retry } =
    usePostAuthTrainingProfileGate();
  const { invalidateSession } = useTrainingProfileGate();
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

  const handleProfileCheckRetry = async () => {
    setSubmitFeedback(null);
    setIsRetryingProfile(true);

    try {
      const status = await retry();

      if (status === "error") {
        setSubmitFeedback({
          message: "No pudimos verificar tu perfil. Intentá nuevamente.",
          variant: "error",
        });
        return;
      }

      setProfileCheckFailed(false);
    } finally {
      setIsRetryingProfile(false);
    }
  };

  const onSubmit = handleSubmit(async (values) => {
    if (profileCheckFailed) {
      await handleProfileCheckRetry();
      return;
    }

    setSubmitFeedback(null);

    const result = await loginAction(values);

    if (result.ok) {
      complete({
        destination: result.destination,
        presence: result.profilePresence,
      });
      return;
    }

    if (result.sessionExpired) {
      invalidateSession();
    }

    setProfileCheckFailed(result.profileCheckFailed === true);

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

  if (isRetryingProfile) {
    return <TrainingProfileGateStatus type="loading" />;
  }

  if (profileCheckFailed) {
    return (
      <TrainingProfileGateStatus
        onRetry={() => void handleProfileCheckRetry()}
        type="error"
      />
    );
  }

  return (
    <div>
      <header className="space-y-4">
        <h1 className="font-brand text-[2.25rem] font-extrabold leading-[1.05] tracking-[-0.025em] text-text-primary sm:text-[2.5rem]">
          Bienvenido de nuevo
        </h1>
        <p className="text-[0.9375rem] leading-6 text-text-secondary">
          Continuá tu progreso.
        </p>
      </header>

      <form className="mt-7 space-y-5" noValidate onSubmit={onSubmit}>
        {submitFeedback ? (
          <Alert variant={submitFeedback.variant}>{submitFeedback.message}</Alert>
        ) : null}

        <div className="space-y-4">
          <FormField
            error={errors.email?.message}
            id={emailFieldId}
            label="Correo electrónico"
          >
            <Input
              autoComplete="email"
              className="text-base font-medium"
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

          <div className="space-y-2">
            <FormField
              error={errors.password?.message}
              id={passwordFieldId}
              label="Contraseña"
            >
              <PasswordInput
                autoComplete="current-password"
                className="text-base font-medium"
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
                className="inline-flex min-h-11 items-center rounded-control-sm px-1 text-[0.8125rem] font-semibold text-text-secondary transition-colors duration-200 hover:text-text-primary focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-focus motion-reduce:transition-none"
                href="/forgot-password"
              >
                ¿Olvidaste tu contraseña?
              </Link>
            </div>
          </div>
        </div>

        <Button
          fullWidth
          isLoading={isSubmitting}
          size="lg"
          type="submit"
          variant="secondary"
        >
          Iniciar sesión
        </Button>
      </form>

      <p className="mt-4 flex min-h-12 items-center justify-center gap-2 border-t border-border pt-3 text-center text-sm text-text-secondary sm:mt-5 sm:pt-4">
        <span>¿No tenés cuenta?</span>
        <Link
          className="inline-flex min-h-11 items-center rounded-control-sm font-semibold text-text-primary underline-offset-4 transition-colors duration-200 hover:text-thermal-accent hover:underline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-focus motion-reduce:transition-none"
          href="/register"
        >
          Crear cuenta
        </Link>
      </p>
    </div>
  );
}
