"use client";

import { zodResolver } from "@hookform/resolvers/zod";
import Link from "next/link";
import { useState } from "react";
import { useForm, useWatch } from "react-hook-form";
import { Alert } from "@/components/ui/alert";
import { Button, buttonClassName } from "@/components/ui/button";
import { FormField } from "@/components/ui/form-field";
import { PasswordInput } from "@/components/ui/password-input";
import { resetPasswordAction } from "../actions/reset-password.action";
import {
  resetPasswordFormSchema,
  type ResetPasswordFormValues,
} from "../schemas/auth.schemas";
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
    <div>
      <header className="space-y-3">
        <h1 className="font-brand text-[2.25rem] font-extrabold leading-[1.05] tracking-[-0.025em] text-text-primary sm:text-[2.5rem]">
          Creá una nueva contraseña
        </h1>
        <p className="text-[0.9375rem] leading-6 text-text-secondary">
          Elegí una contraseña segura para volver a acceder.
        </p>
      </header>

      <form className="mt-5 space-y-4" noValidate onSubmit={onSubmit}>
        {submitFeedback ? (
          <Alert variant={submitFeedback.variant}>{submitFeedback.message}</Alert>
        ) : null}

        <div className="space-y-4">
          <div className="space-y-2.5">
            <FormField
              error={errors.newPassword?.message}
              id={newPasswordFieldId}
              label="Nueva contraseña"
            >
              <PasswordInput
                autoComplete="new-password"
                className="text-base font-medium"
                hasError={Boolean(errors.newPassword)}
                id={newPasswordFieldId}
                placeholder="••••••••"
                {...register("newPassword")}
                aria-describedby={newPasswordDescribedBy || undefined}
                aria-invalid={Boolean(errors.newPassword)}
              />
            </FormField>

            <div className="[&>div]:space-y-1 [&>div]:pt-0 [&_li]:text-[0.8125rem] [&_p]:text-[0.8125rem] [&_p]:font-semibold [&_p]:text-steel [&_ul]:grid-cols-2 [&_ul]:gap-x-4 [&_ul]:gap-y-0.5">
              <PasswordRequirements
                id={passwordRequirementsId}
                password={newPassword}
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

        <Button
          fullWidth
          isLoading={isSubmitting}
          size="lg"
          type="submit"
          variant="secondary"
        >
          Guardar contraseña
        </Button>
      </form>

      <BackToLogin />
    </div>
  );
}

function InvalidResetLink({ message }: { message: string }) {
  return (
    <div>
      <AuthStateHeader description={message} title="El enlace no es válido" />

      <Link
        className={buttonClassName({
          className: "mt-7",
          fullWidth: true,
          size: "lg",
          variant: "secondary",
        })}
        href="/forgot-password"
      >
        Solicitar nuevo enlace
      </Link>
    </div>
  );
}

function ResetPasswordSuccess() {
  return (
    <div>
      <AuthStateHeader
        description="Tu contraseña fue restablecida correctamente. Ya podés volver a iniciar sesión."
        title="Contraseña actualizada"
      />

      <Link
        className={buttonClassName({
          className: "mt-7",
          fullWidth: true,
          size: "lg",
          variant: "secondary",
        })}
        href="/login"
      >
        Iniciar sesión
      </Link>
    </div>
  );
}

function AuthStateHeader({
  description,
  title,
}: {
  description: string;
  title: string;
}) {
  return (
    <header className="space-y-3">
      <h1 className="font-brand text-[2.25rem] font-extrabold leading-[1.05] tracking-[-0.025em] text-text-primary sm:text-[2.5rem]">
        {title}
      </h1>
      <p className="text-[0.9375rem] leading-6 text-text-secondary">
        {description}
      </p>
    </header>
  );
}

function BackToLogin() {
  return (
    <p className="mt-2 flex min-h-12 items-center justify-center border-t border-border pt-2 text-center text-sm sm:mt-4 sm:pt-3">
      <Link
        className="inline-flex min-h-11 items-center rounded-control-sm font-semibold text-text-primary underline-offset-4 transition-colors duration-200 hover:text-thermal-accent hover:underline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-focus motion-reduce:transition-none"
        href="/login"
      >
        Volver a iniciar sesión
      </Link>
    </p>
  );
}
