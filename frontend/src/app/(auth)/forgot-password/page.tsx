import type { Metadata } from "next";
import { ForgotPasswordForm } from "@/features/auth/components/forgot-password-form";

export const metadata: Metadata = {
  title: "Recuperar contraseña | Entrenate",
};

export default function ForgotPasswordPage() {
  return <ForgotPasswordForm />;
}
