"use server";

import { ApiClientError } from "@/services/api-client";
import { changePasswordSchema, type ChangePasswordValues } from "../schemas/auth.schemas";
import { changePassword, getCurrentUser } from "../services/auth.service";
import { clearAuthSession, getAuthToken } from "../services/session.service";

type ChangePasswordField = keyof ChangePasswordValues;

export type ChangePasswordActionResult =
  | { ok: true }
  | {
      fieldErrors?: Partial<Record<ChangePasswordField, string>>;
      kind: "error" | "invalid_current_password" | "unauthenticated" | "validation";
      message: string;
      ok: false;
    };

export async function changePasswordAction(
  values: ChangePasswordValues,
): Promise<ChangePasswordActionResult> {
  const parsedValues = changePasswordSchema.safeParse(values);

  if (!parsedValues.success) {
    const errors = parsedValues.error.flatten().fieldErrors;
    return {
      fieldErrors: Object.fromEntries(
        Object.entries(errors).flatMap(([field, messages]) =>
          messages?.[0] ? [[field, messages[0]]] : [],
        ),
      ),
      kind: "validation",
      message: "Revisá los datos ingresados.",
      ok: false,
    };
  }

  const authToken = await getAuthToken();
  if (!authToken) {
    return { kind: "unauthenticated", message: "Tu sesión venció.", ok: false };
  }

  try {
    await changePassword(authToken, {
      currentPassword: parsedValues.data.currentPassword,
      newPassword: parsedValues.data.newPassword,
    });
    return { ok: true };
  } catch (error) {
    if (error instanceof ApiClientError && error.status === 401) {
      try {
        await getCurrentUser(authToken);
        return {
          fieldErrors: { currentPassword: "La contraseña actual es incorrecta." },
          kind: "invalid_current_password",
          message: "Revisá la contraseña actual.",
          ok: false,
        };
      } catch (sessionError) {
        if (sessionError instanceof ApiClientError && sessionError.status === 401) {
          await clearAuthSession();
          return { kind: "unauthenticated", message: "Tu sesión venció.", ok: false };
        }
      }
    }

    if (error instanceof ApiClientError && error.status === 400) {
      return {
        kind: "validation",
        message: "La nueva contraseña no cumple los requisitos.",
        ok: false,
      };
    }

    return {
      kind: "error",
      message: "No pudimos actualizar la contraseña. Intentá nuevamente.",
      ok: false,
    };
  }
}
