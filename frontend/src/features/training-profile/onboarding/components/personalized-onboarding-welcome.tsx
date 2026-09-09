import { getCurrentUser } from "@/features/auth/services/auth.service";
import { getAuthToken } from "@/features/auth/services/session.service";
import { OnboardingWelcome } from "./onboarding-welcome";

export async function PersonalizedOnboardingWelcome() {
  const authToken = await getAuthToken();

  if (!authToken) {
    return <OnboardingWelcome />;
  }

  let nombre: string | undefined;

  try {
    const currentUser = await getCurrentUser(authToken);
    nombre = currentUser.nombre;
  } catch {
    nombre = undefined;
  }

  return <OnboardingWelcome nombre={nombre} />;
}
