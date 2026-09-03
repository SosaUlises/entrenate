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

export type LoginFormValues = z.infer<typeof loginSchema>;
export type RegisterFormValues = z.infer<typeof registerSchema>;
