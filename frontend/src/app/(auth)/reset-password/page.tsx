import type { Metadata } from "next";
import { ResetPasswordForm } from "@/features/auth/components/reset-password-form";

type SearchParams = Promise<
  Record<string, string | string[] | undefined>
>;

type ResetPasswordPageProps = {
  searchParams: SearchParams;
};

export const metadata: Metadata = {
  title: "Restablecer contraseña | Entrenate",
};

export default async function ResetPasswordPage({
  searchParams,
}: ResetPasswordPageProps) {
  const params = await searchParams;

  return (
    <ResetPasswordForm
      email={getSingleSearchParam(params.email)}
      token={getSingleSearchParam(params.token)}
    />
  );
}

function getSingleSearchParam(
  value: string | string[] | undefined,
): string | undefined {
  if (Array.isArray(value)) {
    return value[0];
  }

  return value;
}
