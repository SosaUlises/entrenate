import type { ReactNode } from "react";

type OnboardingShellProps = {
  children: ReactNode;
};

export function OnboardingShell({ children }: OnboardingShellProps) {
  return (
    <main className="relative isolate min-h-dvh overflow-x-hidden bg-background px-5 py-6 sm:px-6 sm:py-8">
      <div className="mx-auto flex min-h-[calc(100dvh-3rem)] w-full max-w-[960px] flex-col sm:min-h-[calc(100dvh-4rem)]">
        {children}
      </div>
    </main>
  );
}
