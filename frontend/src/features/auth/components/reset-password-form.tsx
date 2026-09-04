"use client";

import { zodResolver } from "@hookform/resolvers/zod";
import { useState } from "react";
import { useForm, useWatch } from "react-hook-form";
import { Alert } from "@/components/ui/alert";
import { Button } from "@/components/ui/button";
import { FormField } from "@/components/ui/form-field";
import { PasswordInput } from "@/components/ui/password-input";
import { resetPasswordAction } from "../actions/reset-password.action";
import {
  resetPasswordFormSchema,
  type ResetPasswordFormValues,
} from "../schemas/auth.schemas";
import { AuthPageHeader } from "./auth-page-header";
import { AuthPrimaryLink } from "./auth-primary-link";
import { AuthSecondaryAction } from "./auth-secondary-action";
import { PasswordRequirements } from "./password-requirements";

const newPasswordFieldId = "reset-password-new-password";
const confirmPasswordFieldId = "reset-password-confirm-password";
const passwordRequirementsId = "reset-password-requirements";

type ResetPasswordFormProps = {
  email?: string;
  token?: string;
};

type SubmitFeedback = {
  message: string;
  variant: "error";
};

export function ResetPasswordForm({ email, token }: ResetPasswordFormProps) {
  const [submitFeedback, setSubmitFeedback] = useState<SubmitFeedback | null>(
    null,
  );
  const [invalidLinkMessage, setInvalidLinkMessage] = useState<string | null>(
    null,
  );
  const [isComplete, setIsComplete] = useState(false);
  const {
    control,
    formState: { errors, isSubmitting },
    handleSubmit,
    register,
    setError,
  } = useForm<ResetPasswordFormValues>({
    defaultValues: {
      confirmPassword: "",
      newPassword: "",
    },
    mode: "onTouched",
    resolver: zodResolver(resetPasswordFormSchema),
  });

  const newPassword = useWatch({
    control,
    name: "newPassword",
  });

  if (!email || !token || invalidLinkMessage) {
    return (
      <InvalidResetLink
        message={
          invalidLinkMessage ??
          "Solicitá un nuevo enlace para restablecer tu contraseña."
        }
      />
    );
  }

  if (isComplete) {
    return <ResetPasswordSuccess />;
  }

  const onSubmit = handleSubmit(async (values) => {
    setSubmitFeedback(null);

    const result = await resetPasswordAction({
      ...values,
      email,
      token,
    });

    if (result.ok) {
      setIsComplete(true);
      return;
    }

    if (result.reason === "invalid_link") {
      setInvalidLinkMessage(result.message);
      return;
    }

    if (result.fieldErrors) {
      for (const [field, message] of Object.entries(result.fieldErrors)) {
        setError(field as keyof ResetPasswordFormValues, {
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

  const newPasswordMessageId = errors.newPassword
    ? `${newPasswordFieldId}-message`
    : undefined;
  const confirmPasswordMessageId = errors.confirmPassword
    ? `${confirmPasswordFieldId}-message`
    : undefined;
  const newPasswordDescribedBy = [
    newPasswordMessageId,
    passwordRequirementsId,
  ]
    .filter(Boolean)
    .join(" ");

  return (
    <div className="space-y-8 sm:space-y-9">
      <AuthPageHeader
        supportingText="Elegí una contraseña segura para volver a acceder."
        title="Creá una nueva contraseña"
      />

      <form className="space-y-5" noValidate onSubmit={onSubmit}>
        {submitFeedback ? (
          <Alert variant={submitFeedback.variant}>{submitFeedback.message}</Alert>
        ) : null}

        <div className="space-y-4">
          <div className="space-y-2">
            <FormField
              error={errors.newPassword?.message}
              id={newPasswordFieldId}
              label="Nueva contraseña"
            >
              <PasswordInput
                autoComplete="new-password"
                hasError={Boolean(errors.newPassword)}
                id={newPasswordFieldId}
                placeholder="••••••••"
                {...register("newPassword")}
                aria-describedby={newPasswordDescribedBy || undefined}
                aria-invalid={Boolean(errors.newPassword)}
              />
            </FormField>

            <PasswordRequirements
              id={passwordRequirementsId}
              password={newPassword}
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
          Guardar contraseña
        </Button>
      </form>

      <AuthSecondaryAction action="Volver a iniciar sesión" href="/login" />
    </div>
  );
}

function InvalidResetLink({ message }: { message: string }) {
  return (
    <div className="space-y-8 sm:space-y-9">
      <AuthPageHeader
        supportingText={message}
        title="El enlace no es válido"
      />

      <AuthPrimaryLink href="/forgot-password">
        Solicitar nuevo enlace
      </AuthPrimaryLink>
    </div>
  );
}

function ResetPasswordSuccess() {
  return (
    <div className="space-y-8 sm:space-y-9">
      <AuthPageHeader
        supportingText="Tu contraseña fue restablecida correctamente. Ya podés volver a iniciar sesión."
        title="Contraseña actualizada"
      />

      <AuthPrimaryLink href="/login">Iniciar sesión</AuthPrimaryLink>
    </div>
  );
}
