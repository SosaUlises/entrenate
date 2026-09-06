import type { ReactNode } from "react";
import { OnboardingShell } from "@/components/layout/onboarding-shell";
import { OnboardingProvider } from "@/features/training-profile/onboarding/context/onboarding-context";

export default function OnboardingLayout({ children }: { children: ReactNode }) {
  return (
    <OnboardingShell>
      <OnboardingProvider>{children}</OnboardingProvider>
    </OnboardingShell>
  );
}
