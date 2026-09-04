import type { ReactNode } from "react";
import { BrandMark } from "./brand-mark";

type AuthShellProps = {
  children: ReactNode;
};

export function AuthShell({ children }: AuthShellProps) {
  return (
    <main className="relative isolate flex min-h-dvh justify-center overflow-x-hidden overflow-y-auto bg-background px-5 py-8 sm:min-h-svh sm:items-center sm:overflow-hidden sm:px-6 sm:py-10">
      <div
        aria-hidden="true"
        className="pointer-events-none absolute inset-x-0 top-1/2 -z-10 mx-auto h-[32rem] w-[32rem] -translate-y-1/2 rounded-full bg-[radial-gradient(circle_at_center,color-mix(in_srgb,var(--primary)_12%,transparent)_0%,color-mix(in_srgb,var(--secondary)_7%,transparent)_28%,transparent_66%)] blur-2xl"
      />
      <div className="mx-auto my-auto flex w-full max-w-[440px] flex-col gap-8 sm:my-0 sm:gap-9">
        <div className="flex justify-center">
          <BrandMark />
        </div>
        <section className="w-full border-0 bg-transparent p-0 shadow-none">
          {children}
        </section>
      </div>
    </main>
  );
}
