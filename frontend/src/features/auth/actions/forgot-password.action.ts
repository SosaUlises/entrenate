"use server";

import { ApiClientError } from "@/services/api-client";
import {
  forgotPasswordSchema,
  type ForgotPasswordFormValues,
} from "../schemas/auth.schemas";
import { forgotPassword } from "../services/auth.service";

type ForgotPasswordFieldErrors = Partial<
  Record<keyof ForgotPasswordFormValues, string>
>;

export type ForgotPasswordActionResult =
  | {
      ok: true;
    }
  | {
      fieldErrors?: ForgotPasswordFieldErrors;
      message: string;
      ok: false;
    };

export async function forgotPasswordAction(
  values: ForgotPasswordFormValues,
): Promise<ForgotPasswordActionResult> {
  const parsedValues = forgotPasswordSchema.safeParse(values);

  if (!parsedValues.success) {
    return {
      fieldErrors: flattenForgotPasswordFieldErrors(
        parsedValues.error.flatten().fieldErrors,
      ),
      message: "Revisa los datos ingresados.",
      ok: false,
    };
  }

  try {
    await forgotPassword(parsedValues.data);

    return { ok: true };
  } catch (error) {
    return getForgotPasswordErrorResult(error);
  }
}

function flattenForgotPasswordFieldErrors(
  errors: Partial<Record<keyof ForgotPasswordFormValues, string[]>>,
): ForgotPasswordFieldErrors {
  const message = errors.email?.[0];

  return message ? { email: message } : {};
}

function getForgotPasswordErrorResult(
  error: unknown,
): ForgotPasswordActionResult {
  if (error instanceof ApiClientError) {
    if (error.status === 400) {
      return {
        fieldErrors: mapApiValidationErrors(error.details),
        message: "Revisa el email ingresado.",
        ok: false,
      };
    }

    if (error.status === 429) {
      return {
        message: "Demasiados intentos. Esperá un momento y volvé a probar.",
        ok: false,
      };
    }

    if (error.code === "missing_api_url") {
      return {
        message: "La URL de la API no está configurada.",
        ok: false,
      };
    }

    if (error.code === "network_error") {
      return {
        message: "No pudimos conectar con la API. Intentalo nuevamente.",
        ok: false,
      };
    }
  }

  return {
    message: "No pudimos enviar las instrucciones. Intentalo nuevamente.",
    ok: false,
  };
}

function mapApiValidationErrors(details: unknown): ForgotPasswordFieldErrors {
  if (!isValidationErrorMap(details)) {
    return {};
  }

  const message = details.Email?.[0] ?? details.email?.[0];

  return message ? { email: message } : {};
}

function isValidationErrorMap(
  value: unknown,
): value is Record<string, string[]> {
  if (!value || typeof value !== "object") {
    return false;
  }

  return Object.values(value).every(
    (messages) =>
      Array.isArray(messages) &&
      messages.every((message) => typeof message === "string"),
  );
}
