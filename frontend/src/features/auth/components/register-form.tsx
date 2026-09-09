"use client";

import { zodResolver } from "@hookform/resolvers/zod";
import { useState } from "react";
import { useForm, useWatch } from "react-hook-form";
import { Alert } from "@/components/ui/alert";
import { Button } from "@/components/ui/button";
import { FormField } from "@/components/ui/form-field";
import { Input } from "@/components/ui/input";
import { PasswordInput } from "@/components/ui/password-input";
import {
  TrainingProfileGateStatus,
  usePostAuthTrainingProfileGate,
} from "@/features/training-profile/gate/training-profile-gate";
import { registerAction } from "../actions/register.action";
import {
  registerSchema,
  type RegisterFormValues,
} from "../schemas/auth.schemas";
import { AuthPageHeader } from "./auth-page-header";
import { AuthSecondaryAction } from "./auth-secondary-action";
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
  const [submitFeedback, setSubmitFeedback] = useState<SubmitFeedback | null>(
    null,
  );
  const [profileCheckFailed, setProfileCheckFailed] = useState(false);
  const [isRetryingProfile, setIsRetryingProfile] = useState(false);
  const { complete, isProfileReady, retry } =
    usePostAuthTrainingProfileGate();
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

  if (isProfileReady) {
    return <TrainingProfileGateStatus type="ready" />;
  }

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
          error={errors.nombre?.message}
          id={nombreFieldId}
          label="Nombre"
        >
          <Input
            autoComplete="name"
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

            <PasswordRequirements
              id={passwordRequirementsId}
              password={password}
            />
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
