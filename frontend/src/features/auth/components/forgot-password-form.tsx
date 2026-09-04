"use client";

import { zodResolver } from "@hookform/resolvers/zod";
import { useState } from "react";
import { useForm } from "react-hook-form";
import { Alert } from "@/components/ui/alert";
import { Button } from "@/components/ui/button";
import { FormField } from "@/components/ui/form-field";
import { Input } from "@/components/ui/input";
import { forgotPasswordAction } from "../actions/forgot-password.action";
import {
  forgotPasswordSchema,
  type ForgotPasswordFormValues,
} from "../schemas/auth.schemas";
import { AuthPageHeader } from "./auth-page-header";
import { AuthSecondaryAction } from "./auth-secondary-action";

const emailFieldId = "forgot-password-email";

type SubmitFeedback = {
  message: string;
  variant: "error";
};

export function ForgotPasswordForm() {
  const [submitFeedback, setSubmitFeedback] = useState<SubmitFeedback | null>(
    null,
  );
  const [submittedEmail, setSubmittedEmail] = useState<string | null>(null);
  const {
    formState: { errors, isSubmitting },
    handleSubmit,
    register,
    setError,
  } = useForm<ForgotPasswordFormValues>({
    defaultValues: {
      email: "",
    },
    mode: "onTouched",
    resolver: zodResolver(forgotPasswordSchema),
  });

  const onSubmit = handleSubmit(async (values) => {
    setSubmitFeedback(null);

    const result = await forgotPasswordAction(values);

    if (result.ok) {
      setSubmittedEmail(values.email.trim());
      return;
    }

    if (result.fieldErrors) {
      for (const [field, message] of Object.entries(result.fieldErrors)) {
        setError(field as keyof ForgotPasswordFormValues, {
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

  if (submittedEmail) {
    return <ForgotPasswordSuccess email={submittedEmail} />;
  }

  const emailMessageId = errors.email ? `${emailFieldId}-message` : undefined;

  return (
    <div className="space-y-8 sm:space-y-9">
      <AuthPageHeader
        supportingText="Ingresá tu correo y te enviaremos las instrucciones para restablecerla."
        title="Recuperá tu contraseña"
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

        <Button
          className="min-h-14"
          fullWidth
          isLoading={isSubmitting}
          type="submit"
          variant="gradient"
        >
          Enviar instrucciones
        </Button>
      </form>

      <AuthSecondaryAction action="Volver a iniciar sesión" href="/login" />
    </div>
  );
}

function ForgotPasswordSuccess({ email }: { email: string }) {
  return (
    <div className="space-y-8 sm:space-y-9">
      <AuthPageHeader
        supportingText={
          <>
            Si existe una cuenta asociada a{" "}
            <span className="break-all font-medium text-text-primary">
              {email}
            </span>
            , vas a recibir las instrucciones para restablecer tu contraseña.
          </>
        }
        title="Revisá tu correo"
      />

      <AuthSecondaryAction action="Volver a iniciar sesión" href="/login" />
    </div>
  );
}
