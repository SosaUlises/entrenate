"use server";

import { ApiClientError } from "@/services/api-client";
import { loginSchema, type LoginFormValues } from "../schemas/auth.schemas";
import { login } from "../services/auth.service";
import { createAuthSession } from "../services/session.service";

type LoginFieldErrors = Partial<Record<keyof LoginFormValues, string>>;

export type LoginActionResult =
  | {
      ok: true;
    }
  | {
      fieldErrors?: LoginFieldErrors;
      message: string;
      ok: false;
    };

export async function loginAction(
  values: LoginFormValues,
): Promise<LoginActionResult> {
  const parsedValues = loginSchema.safeParse(values);

  if (!parsedValues.success) {
    return {
      fieldErrors: flattenLoginFieldErrors(
        parsedValues.error.flatten().fieldErrors,
      ),
      message: "Revisa los datos ingresados.",
      ok: false,
    };
  }

  try {
    const response = await login(parsedValues.data);
    await createAuthSession(response);

    return { ok: true };
  } catch (error) {
    return {
      message: getLoginErrorMessage(error),
      ok: false,
    };
  }
}

function flattenLoginFieldErrors(
  errors: Partial<Record<keyof LoginFormValues, string[]>>,
): LoginFieldErrors {
  const fieldErrors: LoginFieldErrors = {};
  const fields = ["email", "password"] as const;

  for (const field of fields) {
    const message = errors[field]?.[0];

    if (message) {
      fieldErrors[field] = message;
    }
  }

  return fieldErrors;
}

function getLoginErrorMessage(error: unknown): string {
  if (error instanceof ApiClientError) {
    if (error.status === 401) {
      return "Email o contraseña incorrectos.";
    }

    if (error.status === 400) {
      return "Revisa el email y la contraseña ingresados.";
    }

    if (error.code === "missing_api_url") {
      return "La URL de la API no está configurada.";
    }

    if (error.code === "network_error") {
      return "No pudimos conectar con la API. Intentalo nuevamente.";
    }
  }

  return "No pudimos iniciar sesión. Intentalo nuevamente.";
}
