"use client";

import { zodResolver } from "@hookform/resolvers/zod";
import Link from "next/link";
import { useState } from "react";
import { useRouter } from "next/navigation";
import { useForm, useWatch } from "react-hook-form";
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
import { registerAction } from "../actions/register.action";
import {
  registerSchema,
  type RegisterFormValues,
} from "../schemas/auth.schemas";
import { PasswordRequirements } from "./password-requirements";

const nombreFieldId = "register-nombre";
const emailFieldId = "register-email";
const passwordFieldId = "register-password";
const confirmPasswordFieldId = "register-confirm-password";
const passwordRequirementsId = "register-password-requirements";

type SubmitFeedback = {
  message: string;
  variant: "error" | "success";
};

export function RegisterForm() {
  const router = useRouter();
  const [submitFeedback, setSubmitFeedback] = useState<SubmitFeedback | null>(
    null,
  );
  const [profileCheckFailed, setProfileCheckFailed] = useState(false);
  const [isRetryingProfile, setIsRetryingProfile] = useState(false);
  const { complete, retry } =
    usePostAuthTrainingProfileGate();
  const { invalidateSession } = useTrainingProfileGate();
  const {
    control,
    formState: { errors, isSubmitting },
    handleSubmit,
    register,
    setError,
  } = useForm<RegisterFormValues>({
    defaultValues: {
      nombre: "",
      email: "",
      password: "",
      confirmPassword: "",
    },
    mode: "onTouched",
    resolver: zodResolver(registerSchema),
  });

  const password = useWatch({
    control,
    name: "password",
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

    const result = await registerAction(values);

    if (result.ok) {
      complete({
        destination: result.destination,
        presence: result.profilePresence,
      });
      return;
    }

    if (result.sessionExpired) {
      invalidateSession();
      router.replace("/login");
      return;
    }

    setProfileCheckFailed(result.profileCheckFailed === true);

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

  const nombreMessageId = errors.nombre
    ? `${nombreFieldId}-message`
    : undefined;
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
      <header className="space-y-3">
        <h1 className="font-brand text-[2.25rem] font-extrabold leading-[1.05] tracking-[-0.025em] text-text-primary sm:text-[2.5rem]">
          Creá tu cuenta
        </h1>
        <p className="text-[0.9375rem] leading-6 text-text-secondary">
          Empezá a registrar tu progreso.
        </p>
      </header>

      <form className="mt-5 space-y-4" noValidate onSubmit={onSubmit}>
        {submitFeedback ? (
          <Alert variant={submitFeedback.variant}>{submitFeedback.message}</Alert>
        ) : null}

        <div className="space-y-3">
          <FormField
            error={errors.nombre?.message}
            id={nombreFieldId}
            label="Nombre"
          >
            <Input
              autoComplete="name"
              className="text-base font-medium"
              hasError={Boolean(errors.nombre)}
              id={nombreFieldId}
              maxLength={100}
              placeholder="Tu nombre"
              type="text"
              {...register("nombre")}
              aria-describedby={nombreMessageId}
              aria-invalid={Boolean(errors.nombre)}
            />
          </FormField>

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

          <div className="space-y-4">
            <div className="space-y-2.5">
              <FormField
                error={errors.password?.message}
                id={passwordFieldId}
                label="Contraseña"
              >
                <PasswordInput
                  autoComplete="new-password"
                  className="text-base font-medium"
                  hasError={Boolean(errors.password)}
                  id={passwordFieldId}
                  placeholder="••••••••"
                  {...register("password")}
                  aria-describedby={passwordDescribedBy || undefined}
                  aria-invalid={Boolean(errors.password)}
                />
              </FormField>

              <div className="[&>div]:space-y-1 [&>div]:pt-0 [&_li]:text-[0.8125rem] [&_p]:text-[0.8125rem] [&_p]:font-semibold [&_p]:text-steel [&_ul]:grid-cols-2 [&_ul]:gap-x-4 [&_ul]:gap-y-0.5">
                <PasswordRequirements
                  id={passwordRequirementsId}
                  password={password}
                />
              </div>
            </div>

            <FormField
              error={errors.confirmPassword?.message}
              id={confirmPasswordFieldId}
              label="Confirmar contraseña"
            >
              <PasswordInput
                autoComplete="new-password"
                className="text-base font-medium"
                hasError={Boolean(errors.confirmPassword)}
                id={confirmPasswordFieldId}
                placeholder="••••••••"
                {...register("confirmPassword")}
                aria-describedby={confirmPasswordMessageId}
                aria-invalid={Boolean(errors.confirmPassword)}
              />
            </FormField>
          </div>
        </div>

        <Button
          fullWidth
          isLoading={isSubmitting}
          size="lg"
          type="submit"
          variant="secondary"
        >
          Crear cuenta
        </Button>
      </form>

      <p className="mt-2 flex min-h-12 items-center justify-center gap-2 border-t border-border pt-2 text-center text-sm text-text-secondary sm:mt-4 sm:pt-3">
        <span>¿Ya tenés cuenta?</span>
        <Link
          className="inline-flex min-h-11 items-center rounded-control-sm font-semibold text-text-primary underline-offset-4 transition-colors duration-200 hover:text-thermal-accent hover:underline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-focus motion-reduce:transition-none"
          href="/login"
        >
          Iniciar sesión
        </Link>
      </p>
    </div>
  );
}
