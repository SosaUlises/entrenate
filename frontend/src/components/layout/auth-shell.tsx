"use client";

import { usePathname } from "next/navigation";
import type { ReactNode } from "react";
import { cn } from "@/lib/class-names";
import { BrandMark } from "./brand-mark";

type AuthShellProps = {
  children: ReactNode;
};

export function AuthShell({ children }: AuthShellProps) {
  const pathname = usePathname();
  const isForgotPassword = pathname === "/forgot-password";
  const isLogin = pathname === "/login";
  const isRegister = pathname === "/register";
  const isResetPassword = pathname === "/reset-password";
  const usesCompactAuth = isRegister || isResetPassword;
  const usesProminentBrand =
    isForgotPassword || isLogin || usesCompactAuth;

  return (
    <main
      className={cn(
        "relative isolate flex min-h-dvh justify-center overflow-x-hidden overflow-y-auto bg-background px-5 sm:min-h-svh sm:items-center sm:overflow-hidden sm:px-6 sm:py-10",
        usesCompactAuth ? "py-6" : "py-8",
      )}
    >
      <div
        className={cn(
          "mx-auto my-auto flex w-full max-w-[440px] flex-col sm:my-0",
          isLogin || isForgotPassword
            ? "gap-10 sm:gap-11"
            : usesCompactAuth
              ? "gap-7 sm:gap-8"
              : "gap-8 sm:gap-9",
        )}
      >
        <div className={cn("flex justify-center", usesCompactAuth && "-my-4")}>
          <BrandMark prominent={usesProminentBrand} />
        </div>
        <section className="w-full border-0 bg-transparent p-0 shadow-none">
          {children}
        </section>
      </div>
    </main>
  );
}
