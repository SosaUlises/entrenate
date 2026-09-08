"use client";

import { useRouter } from "next/navigation";

type OnboardingStepNavigationOptions = {
  backPath: string;
  nextPath: string;
};

const summaryPath = "/onboarding/summary";

export function useOnboardingStepNavigation({
  backPath,
  nextPath,
}: OnboardingStepNavigationOptions) {
  const router = useRouter();

  const resolvePath = (defaultPath: string) =>
    new URLSearchParams(window.location.search).get("returnTo") === "summary"
      ? summaryPath
      : defaultPath;

  return {
    goBack: () => router.push(resolvePath(backPath)),
    goNext: () => router.push(resolvePath(nextPath)),
  };
}
