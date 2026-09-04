import { z } from "zod";

const emailSchema = z
  .string()
  .trim()
  .min(1, "El email es obligatorio.")
  .email("El email ingresado no es válido.");

const backendPasswordSchema = z
  .string()
  .min(1, "La contraseña es obligatoria.")
  .min(8, "La contraseña debe tener al menos 8 caracteres.")
  .regex(/[A-Z]/, "La contraseña debe contener al menos una letra mayúscula.")
  .regex(/[a-z]/, "La contraseña debe contener al menos una letra minúscula.")
  .regex(/[0-9]/, "La contraseña debe contener al menos un número.");

export const loginSchema = z.object({
  email: emailSchema,
  password: z.string().min(1, "La contraseña es obligatoria."),
});

export const forgotPasswordSchema = z.object({
  email: emailSchema,
});

const passwordConfirmationFields = {
  confirmPassword: z.string().min(1, "Confirmá tu contraseña."),
  newPassword: backendPasswordSchema,
};

export const registerSchema = z
  .object({
    confirmPassword: z.string().min(1, "Confirmá tu contraseña."),
    email: emailSchema,
    password: backendPasswordSchema,
  })
  .refine((values) => values.password === values.confirmPassword, {
    message: "Las contraseñas no coinciden.",
    path: ["confirmPassword"],
  });

export const resetPasswordFormSchema = z
  .object(passwordConfirmationFields)
  .refine((values) => values.newPassword === values.confirmPassword, {
    message: "Las contraseñas no coinciden.",
    path: ["confirmPassword"],
  });

export const resetPasswordSchema = z
  .object({
    email: emailSchema,
    token: z.string().min(1, "El token de recuperación es obligatorio."),
    ...passwordConfirmationFields,
  })
  .refine((values) => values.newPassword === values.confirmPassword, {
    message: "Las contraseñas no coinciden.",
    path: ["confirmPassword"],
  });

export type ForgotPasswordFormValues = z.infer<typeof forgotPasswordSchema>;
export type LoginFormValues = z.infer<typeof loginSchema>;
export type RegisterFormValues = z.infer<typeof registerSchema>;
export type ResetPasswordFormValues = z.infer<typeof resetPasswordFormSchema>;
export type ResetPasswordValues = z.infer<typeof resetPasswordSchema>;
