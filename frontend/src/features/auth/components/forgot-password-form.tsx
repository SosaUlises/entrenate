"use client";

import { zodResolver } from "@hookform/resolvers/zod";
import Link from "next/link";
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
    <div>
      <header className="space-y-4">
        <h1 className="font-brand text-[2.25rem] font-extrabold leading-[1.05] tracking-[-0.025em] text-text-primary sm:text-[2.5rem]">
          Recuperá tu contraseña
        </h1>
        <p className="max-w-[38ch] text-[0.9375rem] leading-6 text-text-secondary">
          Ingresá tu correo y te enviaremos las instrucciones para restablecerla.
        </p>
      </header>

      <form className="mt-7 space-y-5" noValidate onSubmit={onSubmit}>
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

        <Button
          fullWidth
          isLoading={isSubmitting}
          size="lg"
          type="submit"
          variant="secondary"
        >
          Enviar instrucciones
        </Button>
      </form>

      <AuthBackToLogin />
    </div>
  );
}

function ForgotPasswordSuccess({ email }: { email: string }) {
  return (
    <div>
      <header className="space-y-4">
        <h1 className="font-brand text-[2.25rem] font-extrabold leading-[1.05] tracking-[-0.025em] text-text-primary sm:text-[2.5rem]">
          Revisá tu correo
        </h1>
        <p className="max-w-[38ch] text-[0.9375rem] leading-6 text-text-secondary">
          Si existe una cuenta asociada a{" "}
          <span className="break-all font-semibold text-text-primary">
            {email}
          </span>
          , vas a recibir las instrucciones para restablecer tu contraseña.
        </p>
      </header>

      <AuthBackToLogin />
    </div>
  );
}

function AuthBackToLogin() {
  return (
    <div className="mt-4 flex min-h-12 items-center justify-center border-t border-border pt-3 sm:mt-5 sm:pt-4">
      <Link
        className="inline-flex min-h-11 items-center rounded-control-sm px-1 text-sm font-semibold text-text-primary underline-offset-4 transition-colors duration-200 hover:text-thermal-accent hover:underline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-focus motion-reduce:transition-none"
        href="/login"
      >
        Volver a iniciar sesión
      </Link>
    </div>
  );
}
