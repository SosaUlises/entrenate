"use server";

import { ApiClientError } from "@/services/api-client";
import {
  resetPasswordSchema,
  type ResetPasswordValues,
} from "../schemas/auth.schemas";
import { resetPassword } from "../services/auth.service";

type ResetPasswordFieldErrors = Partial<
  Record<"confirmPassword" | "newPassword", string>
>;

export type ResetPasswordActionResult =
  | {
      ok: true;
    }
  | {
      fieldErrors?: ResetPasswordFieldErrors;
      message: string;
      ok: false;
      reason?: "invalid_link";
    };

export async function resetPasswordAction(
  values: ResetPasswordValues,
): Promise<ResetPasswordActionResult> {
  const parsedValues = resetPasswordSchema.safeParse(values);

  if (!parsedValues.success) {
    return getValidationErrorResult(parsedValues.error.flatten().fieldErrors);
  }

  const { email, newPassword, token } = parsedValues.data;

  try {
    await resetPassword({ email, newPassword, token });

    return { ok: true };
  } catch (error) {
    return getResetPasswordErrorResult(error);
  }
}

function getValidationErrorResult(
  errors: Partial<Record<keyof ResetPasswordValues, string[]>>,
): ResetPasswordActionResult {
  if (errors.email?.[0] || errors.token?.[0]) {
    return {
      message: "Solicitá un nuevo enlace para restablecer tu contraseña.",
      ok: false,
      reason: "invalid_link",
    };
  }

  const fieldErrors: ResetPasswordFieldErrors = {};
  const fields = ["newPassword", "confirmPassword"] as const;

  for (const field of fields) {
    const message = errors[field]?.[0];

    if (message) {
      fieldErrors[field] = message;
    }
  }

  return {
    fieldErrors,
    message: "Revisa los datos ingresados.",
    ok: false,
  };
}

function getResetPasswordErrorResult(
  error: unknown,
): ResetPasswordActionResult {
  if (error instanceof ApiClientError) {
    if (error.status === 400) {
      return getApiValidationErrorResult(error.details);
    }

    if (error.status === 401 || error.status === 500) {
      return {
        message:
          "Este enlace ya no es válido. Solicitá uno nuevo para continuar.",
        ok: false,
        reason: "invalid_link",
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
    message: "No pudimos restablecer la contraseña. Intentalo nuevamente.",
    ok: false,
  };
}

function getApiValidationErrorResult(
  details: unknown,
): ResetPasswordActionResult {
  if (!isValidationErrorMap(details)) {
    return {
      message: "Revisa los datos ingresados.",
      ok: false,
    };
  }

  if (details.Email?.[0] || details.email?.[0] || details.Token?.[0]) {
    return {
      message: "Solicitá un nuevo enlace para restablecer tu contraseña.",
      ok: false,
      reason: "invalid_link",
    };
  }

  const fieldErrors: ResetPasswordFieldErrors = {};
  const newPasswordMessage =
    details.NewPassword?.[0] ?? details.newPassword?.[0];

  if (newPasswordMessage) {
    fieldErrors.newPassword = newPasswordMessage;
  }

  return {
    fieldErrors,
    message: "Revisa los datos ingresados.",
    ok: false,
  };
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
