import type { ReactNode } from "react";

type OnboardingShellProps = {
  children: ReactNode;
};

export function OnboardingShell({ children }: OnboardingShellProps) {
  return (
    <main className="relative isolate min-h-dvh overflow-x-hidden bg-background px-5 py-6 sm:px-6 sm:py-8">
      <div
        aria-hidden="true"
        className="pointer-events-none absolute inset-x-0 top-[38%] -z-10 mx-auto h-[28rem] w-[28rem] -translate-y-1/2 rounded-full bg-[radial-gradient(circle_at_center,color-mix(in_srgb,var(--primary)_10%,transparent)_0%,color-mix(in_srgb,var(--secondary)_6%,transparent)_30%,transparent_68%)] blur-2xl"
      />
      <div className="mx-auto flex min-h-[calc(100dvh-3rem)] w-full max-w-[900px] flex-col sm:min-h-[calc(100dvh-4rem)]">
        {children}
      </div>
    </main>
  );
}
