"use server";

import { ApiClientError } from "@/services/api-client";
import {
  registerSchema,
  type RegisterFormValues,
} from "../schemas/auth.schemas";
import { register as registerUser } from "../services/auth.service";
import { createAuthSession } from "../services/session.service";

type RegisterFieldErrors = Partial<Record<keyof RegisterFormValues, string>>;

export type RegisterActionResult =
  | {
      ok: true;
    }
  | {
      fieldErrors?: RegisterFieldErrors;
      message: string;
      ok: false;
    };

export async function registerAction(
  values: RegisterFormValues,
): Promise<RegisterActionResult> {
  const parsedValues = registerSchema.safeParse(values);

  if (!parsedValues.success) {
    return {
      fieldErrors: flattenRegisterFieldErrors(
        parsedValues.error.flatten().fieldErrors,
      ),
      message: "Revisa los datos ingresados.",
      ok: false,
    };
  }

  const { nombre, email, password } = parsedValues.data;

  try {
    const response = await registerUser({ nombre, email, password });
    await createAuthSession(response);

    return { ok: true };
  } catch (error) {
    return getRegisterErrorResult(error);
  }
}

function flattenRegisterFieldErrors(
  errors: Partial<Record<keyof RegisterFormValues, string[]>>,
): RegisterFieldErrors {
  const fieldErrors: RegisterFieldErrors = {};
  const fields = [
    "nombre",
    "email",
    "password",
    "confirmPassword",
  ] as const;

  for (const field of fields) {
    const message = errors[field]?.[0];

    if (message) {
      fieldErrors[field] = message;
    }
  }

  return fieldErrors;
}

function getRegisterErrorResult(error: unknown): RegisterActionResult {
  if (error instanceof ApiClientError) {
    if (error.status === 400) {
      return {
        fieldErrors: mapApiValidationErrors(error.details),
        message: "Revisa los datos ingresados.",
        ok: false,
      };
    }

    if (error.status === 409) {
      return {
        fieldErrors: {
          email: "Ya existe una cuenta registrada con ese email.",
        },
        message: "Ya existe una cuenta registrada con ese email.",
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
    message: "No pudimos crear la cuenta. Intentalo nuevamente.",
    ok: false,
  };
}

function mapApiValidationErrors(details: unknown): RegisterFieldErrors {
  if (!isValidationErrorMap(details)) {
    return {};
  }

  const fieldErrors: RegisterFieldErrors = {};
  const fieldMap = {
    Email: "email",
    Nombre: "nombre",
    Password: "password",
    email: "email",
    nombre: "nombre",
    password: "password",
  } as const;

  for (const [apiField, formField] of Object.entries(fieldMap)) {
    const message = details[apiField]?.[0];

    if (message) {
      fieldErrors[formField] = message;
    }
  }

  return fieldErrors;
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
