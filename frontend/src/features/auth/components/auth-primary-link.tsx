import Link from "next/link";
import type { ReactNode } from "react";

type AuthPrimaryLinkProps = {
  children: ReactNode;
  href: string;
};

export function AuthPrimaryLink({ children, href }: AuthPrimaryLinkProps) {
  return (
    <Link
      className="inline-flex min-h-14 w-full items-center justify-center rounded-control bg-primary px-5 text-sm font-semibold text-primary-foreground transition-[background-color,transform] duration-200 hover:bg-[color-mix(in_srgb,var(--primary)_88%,var(--primary-pressed))] active:scale-[0.99] active:bg-primary-pressed focus-visible:outline-focus motion-reduce:transition-none motion-reduce:active:scale-100"
      href={href}
    >
      {children}
    </Link>
  );
}
