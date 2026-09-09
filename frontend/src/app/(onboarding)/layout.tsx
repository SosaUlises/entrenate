import type { ReactNode } from "react";
import { OnboardingShell } from "@/components/layout/onboarding-shell";
import { TrainingProfileGate } from "@/features/training-profile/gate/training-profile-gate";
import { OnboardingProvider } from "@/features/training-profile/onboarding/context/onboarding-context";

export default function OnboardingLayout({ children }: { children: ReactNode }) {
  return (
    <OnboardingShell>
      <TrainingProfileGate access="onboarding">
        <OnboardingProvider>{children}</OnboardingProvider>
      </TrainingProfileGate>
    </OnboardingShell>
  );
}
