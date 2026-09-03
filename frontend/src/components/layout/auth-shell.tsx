import type { ReactNode } from "react";
import { BrandMark } from "./brand-mark";

type AuthShellProps = {
  children: ReactNode;
};

export function AuthShell({ children }: AuthShellProps) {
  return (
    <main className="min-h-svh bg-background px-5 py-8">
      <div className="mx-auto flex min-h-[calc(100svh-4rem)] w-full max-w-md flex-col justify-center gap-8">
        <BrandMark />
        <section className="w-full rounded-card border border-border bg-surface p-5 shadow-elevated sm:p-6">
          {children}
        </section>
      </div>
    </main>
  );
}
