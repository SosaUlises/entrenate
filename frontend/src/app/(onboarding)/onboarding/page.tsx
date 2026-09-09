import { Suspense } from "react";
import { OnboardingWelcome } from "@/features/training-profile/onboarding/components/onboarding-welcome";
import { PersonalizedOnboardingWelcome } from "@/features/training-profile/onboarding/components/personalized-onboarding-welcome";

export default function OnboardingPage() {
  return (
    <Suspense fallback={<OnboardingWelcome />}>
      <PersonalizedOnboardingWelcome />
    </Suspense>
  );
}
