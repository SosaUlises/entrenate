"use server";

import { ApiClientError } from "@/services/api-client";
import { resolvePostAuthDestination } from "@/features/training-profile/gate/training-profile-gate.service";
import type { TrainingProfilePresence } from "@/features/training-profile/gate/training-profile-navigation";
import { loginSchema, type LoginFormValues } from "../schemas/auth.schemas";
import { login } from "../services/auth.service";
import { createAuthSession } from "../services/session.service";
import type { AuthResponse } from "../types/auth.types";

type LoginFieldErrors = Partial<Record<keyof LoginFormValues, string>>;

export type LoginActionResult =
  | {
      destination: string | null;
      ok: true;
      profilePresence: TrainingProfilePresence;
    }
  | {
      fieldErrors?: LoginFieldErrors;
      message: string;
      ok: false;
      profileCheckFailed?: true;
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

  let authResponse: AuthResponse;

  try {
    authResponse = await login(parsedValues.data);
    await createAuthSession(authResponse);
  } catch (error) {
    return {
      message: getLoginErrorMessage(error),
      ok: false,
    };
  }

  const destinationResolution = await resolvePostAuthDestination(
    authResponse.token,
  );

  if (destinationResolution.status === "error") {
    return {
      message: "No pudimos verificar tu perfil. Intentá nuevamente.",
      ok: false,
      profileCheckFailed: true,
    };
  }

  return {
    destination: destinationResolution.destination,
    ok: true,
    profilePresence: destinationResolution.presence,
  };
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
